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
        private TextureFormat format = TextureFormat.R8;
        private float resizeCoefficient = 1.0f;

        private ARCameraManager cameraManager;
        private Texture2D texture;
        private Texture2D returnedTexture;
        private NativeArray<byte> imageFeatureExtractorBuffer;
        private NativeArray<byte> imageEncoderBuffer;

        private NativeArray<XRCameraConfiguration> configurations;

        private SimpleJob job;

        public static Semaphore semaphore = new Semaphore(1);

        private VPSTextureRequirement textureRequirement;
        private VPSTextureRequirement feautureExtractorRequirement;
        private VPSTextureRequirement encoderRequirement;

        private void Awake()
        {
            cameraManager = FindObjectOfType<ARCameraManager>();
            if (!cameraManager)
            {
                Debug.LogError("Can't find ARCameraManager on scene!");
                return;
            }

            textureRequirement = new VPSTextureRequirement(VPSTextureType.LOCALISATION_TEXTURE, desiredResolution.x, desiredResolution.y, format);
            cameraManager.frameReceived += UpdateFrame;
        }

        public void Init(VPSTextureRequirement FeautureExtractorRequirement, VPSTextureRequirement EncoderRequirement)
        {
            FreeBufferMemory();
            feautureExtractorRequirement = FeautureExtractorRequirement;
            imageFeatureExtractorBuffer = new NativeArray<byte>(feautureExtractorRequirement.Width * feautureExtractorRequirement.Height, Allocator.Persistent);
            encoderRequirement = EncoderRequirement;
            imageEncoderBuffer = new NativeArray<byte>(encoderRequirement.Width * encoderRequirement.Height, Allocator.Persistent);
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
            if (imageFeatureExtractorBuffer == null || imageEncoderBuffer == null)
            {
                Debug.LogError("You need call Init before update frame!");
                return;
            }

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
            if (texture == null || texture.width != textureRequirement.Width || texture.height != textureRequirement.Height)
            {
                texture = new Texture2D(textureRequirement.Width, textureRequirement.Height, textureRequirement.Format, false);
            }

            try
            {
                // Convert XRCpuImage to texture
                image.Convert(textureRequirement.GetConversionParams(image), new IntPtr(texture.GetRawTextureData<byte>().GetUnsafePtr()), texture.GetRawTextureData<byte>().Length);
                texture.Apply();
                if (feautureExtractorRequirement.Equals(textureRequirement))
                {
                    imageFeatureExtractorBuffer.CopyFrom(texture.GetRawTextureData<byte>());
                }
                else
                {
                    image.Convert(feautureExtractorRequirement.GetConversionParams(image), new IntPtr(imageFeatureExtractorBuffer.GetUnsafePtr()), imageFeatureExtractorBuffer.Length);
                }

                if (encoderRequirement.Equals(textureRequirement))
                {
                    imageEncoderBuffer.CopyFrom(texture.GetRawTextureData<byte>());
                }
                else if (encoderRequirement.Equals(feautureExtractorRequirement))
                {
                    imageEncoderBuffer.CopyFrom(imageFeatureExtractorBuffer);
                }
                else
                {
                    image.Convert(encoderRequirement.GetConversionParams(image), new IntPtr(imageEncoderBuffer.GetUnsafePtr()), imageEncoderBuffer.Length);
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
                returnedTexture = new Texture2D(desiredResolution.x, desiredResolution.y, TextureFormat.RGBA32, false);
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

        public NativeArray<byte> GetImageEncoderBuffer()
        {
            return imageEncoderBuffer;
        }

        public NativeArray<byte> GetImageFeatureExtractorBuffer()
        {
            return imageFeatureExtractorBuffer;
        }

        private void FreeBufferMemory()
        {
            if (imageFeatureExtractorBuffer != null && imageFeatureExtractorBuffer.IsCreated)
            {
                imageFeatureExtractorBuffer.Dispose();
            }
            if (imageEncoderBuffer != null && imageEncoderBuffer.IsCreated)
            {
                imageEncoderBuffer.Dispose();
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