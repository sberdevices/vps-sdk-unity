using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARVRLab.VPSService
{
    public class ARFoundationCamera : MonoBehaviour, ICamera
    {
        private ARCameraManager cameraManager;
        private Texture2D texture;

        private Dictionary<VPSTextureRequirement, NativeArray<byte>> buffers;

        private NativeArray<XRCameraConfiguration> configurations;

        private SimpleJob job;
        private RotateJob rotateJob;

        public static Semaphore semaphore = new Semaphore(1);

        private bool isReady = false;
        private DeviceOrientation currentOrientation;

        private void Awake()
        {
            cameraManager = FindObjectOfType<ARCameraManager>();
            if (!cameraManager)
            {
                VPSLogger.Log(LogLevel.ERROR, "ARCameraManager is not found");
                return;
            }
            cameraManager.frameReceived += UpdateFrame;
        }

        public void Init(VPSTextureRequirement[] requirements)
        {
            FreeBufferMemory();

            var distinctRequir = requirements.Distinct().ToList();
            buffers = distinctRequir.ToDictionary(r => r, r => new NativeArray<byte>(r.Width * r.Height * r.ChannelsCount(), Allocator.Persistent));
        }

        private IEnumerator Start()
        {   
            job = new SimpleJob();
            rotateJob = new RotateJob();

            while (configurations.Length == 0)
            {
                if ((cameraManager == null) || (cameraManager.subsystem == null) || !cameraManager.subsystem.running)
                {
                    yield return null;
                    continue;
                }

                // Try to get available resolutions
                configurations = cameraManager.GetConfigurations(Allocator.Temp);

                if (!configurations.IsCreated || (configurations.Length <= 0))
                {
                    yield return null;
                    continue;
                }

                // Try to get 1920x1080 resolution
                var hdConfig = configurations.FirstOrDefault(a => a.width == 1920 && a.height == 1080);
                if (hdConfig == default)
                {
                    VPSLogger.Log(LogLevel.DEBUG, "Can't take HD resolution. The best available one will be chosen");
                    // Get the best resolution
                    hdConfig = configurations.OrderByDescending(a => a.width * a.height).FirstOrDefault();
                }
                isReady = true;

                cameraManager.currentConfiguration = hdConfig;
            } 
        }

        /// <summary>
        /// Update texture 
        /// </summary>
        private unsafe void UpdateFrame(ARCameraFrameEventArgs args)
        {
            if (buffers == null || buffers.Count == 0)
                return;

            if (!semaphore.CheckState())
                return;
            semaphore.TakeOne();

            // Get latest camera image
            XRCpuImage image;
            if (!cameraManager.TryAcquireLatestCpuImage(out image))
            {
                VPSLogger.Log(LogLevel.ERROR, "Can't take camera image");
                return;
            }

            try
            {
                // Convert XRCpuImage to texture
                foreach (var req in buffers.Keys)
                {
                    if (currentOrientation == DeviceOrientation.Portrait || currentOrientation == DeviceOrientation.PortraitUpsideDown)
                    {
                        image.Convert(req.GetConversionParams(image, req.Height, req.Width), new IntPtr(buffers[req].GetUnsafePtr()), buffers[req].Length);
                    }
                    else
                    {
                        image.Convert(req.GetConversionParams(image, req.Width, req.Height), new IntPtr(buffers[req].GetUnsafePtr()), buffers[req].Length);
                    }
                    RotateImage(req);
                }
            }
            catch (Exception ex)
            {
                VPSLogger.Log(LogLevel.ERROR, ex);
            }
            finally
            {
                // Free memory
                image.Dispose();
            }
            semaphore.Free();
        }

        public Texture2D GetFrame(VPSTextureRequirement requir)
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            if (texture == null || texture.width != requir.Width || texture.height != requir.Height || texture.format != requir.Format)
            {
                texture = new Texture2D(requir.Width, requir.Height, requir.Format, false);
            }
            texture.LoadRawTextureData(GetBuffer(requir));
            texture.Apply();

            // need to copy the red channel to green and blue
            NativeArray<Color> array = new NativeArray<Color>(texture.GetPixels(), Allocator.TempJob);
            job.array = array;

            JobHandle handle = job.Schedule(array.Length, 64);
            handle.Complete();

            if (handle.IsCompleted)
            {
                texture.SetPixels(job.array.ToArray());
            }
            texture.Apply();

            array.Dispose();

            stopWatch.Stop();
            TimeSpan copyChannelTS = stopWatch.Elapsed;

            string copyChannelTime = String.Format("{0:N10}", copyChannelTS.TotalSeconds);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] CopyImageFrameChannelRunTime {0}", copyChannelTime);
            return texture;
        }

        public Vector2 GetFocalPixelLength()
        {
            if (buffers == null || buffers.Count == 0 || !cameraManager.currentConfiguration.HasValue)
                return Vector2.zero;

            XRCameraIntrinsics intrins;
            if (cameraManager.TryGetIntrinsics(out intrins))
            {
                VPSTextureRequirement req = buffers.FirstOrDefault().Key;
                float cameraHeight = (float)cameraManager.currentConfiguration.Value.height;

                if (currentOrientation == DeviceOrientation.Portrait || currentOrientation == DeviceOrientation.PortraitUpsideDown)
                {
                    float resizeCoef = req.Width / cameraHeight;
                    return new Vector2(intrins.focalLength.x * resizeCoef,
                        intrins.focalLength.y * resizeCoef);
                }
                else
                {
                    float resizeCoef = req.Height / cameraHeight;
                    return new Vector2(intrins.focalLength.x * resizeCoef,
                        intrins.focalLength.y * resizeCoef);
                }
            }

            return Vector2.zero;
        }

        public Vector2 GetPrincipalPoint()
        {
            VPSTextureRequirement req = buffers.FirstOrDefault().Key;
            return new Vector2(req.Width / 2f, req.Height / 2f);
        }

        public bool IsCameraReady()
        {
            return isReady;
        }

        public NativeArray<byte> GetBuffer(VPSTextureRequirement requir)
        {
            return buffers[requir];
        }

        private void RotateImage(VPSTextureRequirement requir)
        {
            rotateJob.width = requir.Height;
            rotateJob.height = requir.Width;

            rotateJob.orientation = currentOrientation;
            rotateJob.input = buffers[requir];
            rotateJob.output = new NativeArray<byte>(buffers[requir].Length, Allocator.TempJob);

            JobHandle handle = rotateJob.Schedule(buffers[requir].Length, 64);
            handle.Complete();

            if (handle.IsCompleted)
            {
                buffers[requir].CopyFrom(rotateJob.output);
            }

            rotateJob.output.Dispose();
        }

        /// <summary>
        /// Free all buffers
        /// </summary>
        private void FreeBufferMemory()
        {
            if (buffers == null)
                return;

            foreach (var buffer in buffers.Values)
            {
                if (buffer != null && buffer.IsCreated)
                    buffer.Dispose();
            }
            buffers.Clear();
        }

        private void OnDestroy()
        {
            FreeBufferMemory();
        }

        private struct SimpleJob : IJobParallelFor
        {
            public NativeArray<Color> array;
            private Color color;

            public void Execute(int i)
            {
                color.r = array[i].r;
                color.g = array[i].r;
                color.b = array[i].r;
                array[i] = color;
            }
        }

        private struct RotateJob : IJobParallelFor
        {
            public int width, height;
            public NativeArray<byte> input;
            public NativeArray<byte> output;
            public DeviceOrientation orientation;

            private int x, y;

            public void Execute(int i)
            {
                switch (orientation)
                {
                    case DeviceOrientation.LandscapeRight:
                        // don't rotate
                        output[i] = input[i];
                        break;
                    case DeviceOrientation.LandscapeLeft:
                        // rotate 180
                        output[width * height - i - 1] = input[i];
                        break;
                    case DeviceOrientation.Portrait:
                        // rotete 90 clockwise
                        x = i / width;
                        y = i % width;
                        output[y * height + height - x - 1] = input[i];
                        break;
                    case DeviceOrientation.PortraitUpsideDown:
                        // rotete 90 anticlockwise
                        x = i / width;
                        y = i % width;
                        output[(width - y - 1) * height + x] = input[i];
                        break;
                }
            }
        }

        public VPSOrientation GetOrientation()
        {
            return VPSOrientation.Portrait;
        }

        private void Update()
        {
            if (Input.deviceOrientation != DeviceOrientation.FaceDown
                && Input.deviceOrientation != DeviceOrientation.FaceUp
                && Input.deviceOrientation != DeviceOrientation.Unknown)
                currentOrientation = Input.deviceOrientation;
        }
    }
}