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
        private float cropCoefficient = 9f / 16f;
        private float resizeCoefficient = 1.0f;

        public Resolution TagretResolution;

        private ARCameraManager cameraManager;
        private Texture2D texture;
        private Texture2D returnedTexture;

        private NativeArray<XRCameraConfiguration> configurations;

        private NativeArray<byte> buffer;

        private SimpleJob job;

        public static Semaphore semaphore = new Semaphore(1);

        private void Awake()
        {
            cameraManager = FindObjectOfType<ARCameraManager>();
            if (!cameraManager)
            {
                Debug.LogError("Can't find ARCameraManager on scene!");
                return;
            }

            cameraManager.frameReceived += UpdateFrame;

            TagretResolution.width = desiredResolution.x;
            TagretResolution.height = desiredResolution.y;
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
                    resizeCoefficient = (float)TagretResolution.width / (float)bestConfiguration.width;

                    RectInt resolution = Crop(bestConfiguration.width, bestConfiguration.height);
                    TagretResolution.width = resolution.width;
                    TagretResolution.height = resolution.height;
                }
                else
                {
                    cameraManager.currentConfiguration = hdConfig;
                    resizeCoefficient = (float)TagretResolution.width / (float)hdConfig.width;

                    RectInt resolution = Crop(hdConfig.width, hdConfig.height);
                    TagretResolution.width = resolution.width;
                    TagretResolution.height = resolution.height;
                }
                buffer = new NativeArray<byte>(TagretResolution.width * TagretResolution.height, Allocator.Persistent);
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

            var format = TextureFormat.R8;

            RectInt croppedRect = Crop(cameraManager.currentConfiguration.Value.width, cameraManager.currentConfiguration.Value.height);
            // Create texture
            if (texture == null || texture.width != desiredResolution.x || texture.height != desiredResolution.y)
            {
                texture = new Texture2D(desiredResolution.x, desiredResolution.y, format, false);
            }

            // Set parametrs: format, horizontal mirror (left | right)
            var conversionParams = new XRCpuImage.ConversionParams(image, format, XRCpuImage.Transformation.None);
            conversionParams.inputRect = croppedRect;
            // Set downscale resolution
            conversionParams.outputDimensions = new Vector2Int(desiredResolution.x, desiredResolution.y);

            var raw = texture.GetRawTextureData<byte>();

            try
            {
                // Convert XRCpuImage to texture
                image.Convert(conversionParams, new IntPtr(raw.GetUnsafePtr()), raw.Length);
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                // Free memory
                image.Dispose();
            }

            buffer.CopyFrom(raw);
            texture.Apply();
            semaphore.Free();
        }

        public Texture2D GetFrame()
        {
            // Need to create new texture in RGB format
            if (returnedTexture == null)
            {
                returnedTexture = new Texture2D(TagretResolution.width, TagretResolution.height, TextureFormat.RGBA32, false);
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

        private RectInt Crop(int width, int height)
        {
            int requiredWidth = width;
            int requiredHeight = (int)(width * cropCoefficient);
            int xpos = 0;
            int ypos = 0;

            if (requiredHeight > height)
            {
                requiredHeight = height;
                requiredWidth = (int)(width * (1 / cropCoefficient));
                xpos = (width - requiredWidth) / 2;
            }
            else
            {
                ypos = (height - requiredHeight) / 2;
            }

            return new RectInt(xpos, ypos, requiredWidth, requiredHeight);
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

        public NativeArray<byte> GetImageArray()
        {
            return buffer;
        }

        private void FreeBufferMemory()
        {
            if (buffer != null && buffer.IsCreated)
            {
                buffer.Dispose();
            }
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