using System;
using System.Collections;
using System.Collections.Generic;
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
        [Tooltip("Target photo resolution")]
        private Vector2Int desiredResolution = new Vector2Int(960, 540);
        private TextureFormat format = TextureFormat.RGB24;
        private float resizeCoefficient = 1.0f;

        private ARCameraManager cameraManager;
        private Texture2D texture;
        private Texture2D returnedTexture;

        private Dictionary<VPSTextureRequirement, NativeArray<byte>> buffers;

        private NativeArray<XRCameraConfiguration> configurations;

        private SimpleJob job;

        public static Semaphore semaphore = new Semaphore(1);

        // названия покороче
        private VPSTextureRequirement textureRequir;

        private void Awake()
        {
            cameraManager = FindObjectOfType<ARCameraManager>();
            if (!cameraManager)
            {
                Debug.LogError("Can't find ARCameraManager on scene!");
                return;
            }

            textureRequir = new VPSTextureRequirement(desiredResolution.x, desiredResolution.y, format);
            cameraManager.frameReceived += UpdateFrame;
        }

        // принимает массив
        public void Init(VPSTextureRequirement[] requirements)
        {
            FreeBufferMemory();

            var distinctRequir = requirements.Distinct().ToList();
            buffers = distinctRequir.ToDictionary(r => r, r => new NativeArray<byte>(r.Width * r.Height, Allocator.Persistent));
        }

        private IEnumerator Start()
        {   
            job = new SimpleJob();

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
                    Debug.LogError("Can't take HD resolution!");
                    // Get the best resolution
                    var bestConfiguration = configurations.OrderByDescending(a => a.width * a.height).FirstOrDefault();
                    cameraManager.currentConfiguration = bestConfiguration;
                    resizeCoefficient = (float)desiredResolution.x / (float)bestConfiguration.width;
                }
                else
                {
                    cameraManager.currentConfiguration = hdConfig;
                    resizeCoefficient = (float)desiredResolution.x / (float)hdConfig.width;
                }
            }
        }

        /// <summary>
        /// Update texture 
        /// </summary>
        private unsafe void UpdateFrame(ARCameraFrameEventArgs args)
        {
            if (!semaphore.CheckState())
                return;
            semaphore.TakeOne();

            // Get latest camera image
            XRCpuImage image;
            if (!cameraManager.TryAcquireLatestCpuImage(out image))
            {
                Debug.Log("Не удалось получить изображение с камеры!");
                return;
            }

            // Create texture
            if (texture == null || texture.width != textureRequir.Width || texture.height != textureRequir.Height)
            {
                texture = new Texture2D(textureRequir.Width, textureRequir.Height, textureRequir.Format, false);
            }

            try
            {
                // подумать над оптимизацией через выделения одинаковых
                // Convert XRCpuImage to texture
                image.Convert(textureRequir.GetConversionParams(image),
                    new IntPtr(texture.GetRawTextureData<byte>().GetUnsafePtr()),
                    texture.GetRawTextureData<byte>().Length);
                texture.Apply();

                var toCopy = buffers.Keys.Where(key => key.Equals(textureRequir));
                var toCreate = buffers.Keys.Except(toCopy);

                foreach(var req in toCopy)
                {
                    buffers[req].CopyFrom(texture.GetRawTextureData());
                }

                foreach (var req in toCreate)
                {
                    image.Convert(req.GetConversionParams(image), new IntPtr(buffers[req].GetUnsafePtr()), buffers[req].Length);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                // Free memory
                image.Dispose();
            }
            semaphore.Free();
        }

        public Texture2D GetFrame()
        {
            // Need to create new texture in RGB format
            if (returnedTexture == null)
            {
                returnedTexture = new Texture2D(desiredResolution.x, desiredResolution.y, format, false);
            }

            NativeArray<Color> array = new NativeArray<Color>(texture.GetPixels(), Allocator.TempJob);
            job.array = array;

            JobHandle handle = job.Schedule(array.Length, 64);
            handle.Complete();

            if (handle.IsCompleted)
            {
                returnedTexture.SetPixels(job.array.ToArray());
            }
            returnedTexture.Apply();

            array.Dispose();
            return returnedTexture;
        }

        public Vector2 GetFocalPixelLength()
        {
            XRCameraIntrinsics intrins;
            if (cameraManager.TryGetIntrinsics(out intrins))
            {
                return intrins.focalLength;
            }

            return Vector2.zero;
        }

        public Vector2 GetPrincipalPoint()
        {
            XRCameraIntrinsics intrins;
            if (cameraManager.TryGetIntrinsics(out intrins))
            {
                return intrins.principalPoint;
            }

            return Vector2.zero;
        }

        public bool IsCameraReady()
        {
            return texture != null;
        }

        public NativeArray<byte> GetBuffer(VPSTextureRequirement requir)
        {
            return buffers[requir];
        }

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

        public float GetResizeCoefficient()
        {
            return resizeCoefficient;
        }
    }
}