using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARVRLab.VPSService
{
    public class ARFoundationCamera : MonoBehaviour, ICamera
    {
        [Tooltip("Разрешение, в котором будут делаться фотографии")]
        private Vector2Int desiredResolution = new Vector2Int(960, 540);

        public Resolution TagretResolution;

        private ARCameraManager cameraManager;
        private Texture2D texture;
        private Texture2D scaledTexture;

        private RenderTexture currentRender;

        private NativeArray<XRCameraConfiguration> configurations;

        private NativeArray<byte> buffer;

        private void Awake()
        {
            cameraManager = FindObjectOfType<ARCameraManager>();
            if (!cameraManager)
            {
                Debug.LogError("Can't find ARCameraManager on scene!");
                return;
            }

            cameraManager.frameReceived += UpdateFrame;
            //cameraManager.frameReceived += UpdateFrame1;

            TagretResolution.width = desiredResolution.x;
            TagretResolution.height = desiredResolution.y;
        }

        private IEnumerator Start()
        {
            // Если список доступных разрешений пустой
            while (configurations.Length == 0)
            {
                if ((cameraManager == null) || (cameraManager.subsystem == null) || !cameraManager.subsystem.running)
                {
                    yield return null;
                    continue;
                }

                // Пытаемся получить доступные разрешения
                configurations = cameraManager.GetConfigurations(Allocator.Temp);

                if (!configurations.IsCreated || (configurations.Length <= 0))
                {
                    yield return null;
                    continue;
                }

                // Пытаемся получить разрешение 1920x1080
                cameraManager.currentConfiguration = configurations.FirstOrDefault(a => a.width == 1920 && a.height == 1080);
                if (cameraManager.currentConfiguration == null)
                {
                    Debug.LogError("Не удалось получить HD разрешение!");
                    // Берем наилучшее возможное
                    cameraManager.currentConfiguration = configurations.OrderByDescending(a => a.width * a.height).FirstOrDefault();
                }
            }

            //buffer = new NativeArray<byte>(2073600, Allocator.Persistent);
        }

        /// <summary>
        /// Обновляет значение texture
        /// </summary>
        /// <returns>The frame.</returns>
        private unsafe void UpdateFrame(ARCameraFrameEventArgs args)
        {
            // Пытаемся получить последнее изображение с камеры
            XRCpuImage image;
            if (!cameraManager.TryAcquireLatestCpuImage(out image))
            {
                return;
            }

            var format = TextureFormat.R8;

            // Создаем текстуру
            if (texture == null || texture.width != desiredResolution.x || texture.height != desiredResolution.y)
            {
                texture = new Texture2D(desiredResolution.x, desiredResolution.y, format, false);
            }

            // Настраиваем параметры: задаем формат, отражаем по горизонтали (лево | право)
            var conversionParams = new XRCpuImage.ConversionParams(image, format, XRCpuImage.Transformation.None);
            // Задаем downscale до нужного разрешения
            conversionParams.outputDimensions = new Vector2Int(desiredResolution.x, desiredResolution.y);

            // Получаем ссылку на массив байтов текущей текстуры
            var raw = texture.GetRawTextureData<byte>();
            try
            {
                // Копируем байты из изображения с камеры в текстуру
                image.Convert(conversionParams, new IntPtr(raw.GetUnsafePtr()), raw.Length);
            }
            finally
            {
                // Высвобождаем память
                image.Dispose();
            }

            texture.Apply();

            if (currentRender == null)
                currentRender = new RenderTexture(TagretResolution.width, TagretResolution.height, 0);

            Graphics.Blit(texture, currentRender);

            RenderTexture.active = currentRender;

            if (scaledTexture == null)
                scaledTexture = new Texture2D(TagretResolution.width, TagretResolution.height);

            scaledTexture.ReadPixels(new Rect(0, 0, currentRender.width, currentRender.height), 0, 0);
            scaledTexture.Apply();
            raw.Dispose();
        }

        //private unsafe void UpdateFrame1(ARCameraFrameEventArgs args)
        //{
        //    // Пытаемся получить последнее изображение с камеры
        //    XRCpuImage image;
        //    if (!cameraManager.TryAcquireLatestCpuImage(out image))
        //    {
        //        return;
        //    }

        //    var format = TextureFormat.R8;

        //    // Настраиваем параметры: задаем формат, отражаем по горизонтали (лево | право)
        //    var conversionParams = new XRCpuImage.ConversionParams(image, format, XRCpuImage.Transformation.None);
        //    // Задаем downscale до нужного разрешения
        //    conversionParams.outputDimensions = new Vector2Int(desiredResolution.x, desiredResolution.y);

        //    try
        //    {
        //        // Копируем байты из изображения с камеры в текстуру
        //        image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);
        //    }
        //    finally
        //    {
        //        // Высвобождаем память
        //        image.Dispose();
        //    }
        //}

        public Texture2D GetFrame()
        {
            return scaledTexture;
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
            return scaledTexture != null;
        }

        public NativeArray<byte> GetImageArray()
        {
            return buffer;
        }
    }
}