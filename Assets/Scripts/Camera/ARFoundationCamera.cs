using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARVRLab.VPSService
{
    public class ARFoundationCamera : MonoBehaviour, ICamera
    {
        public ARCameraManager cameraManager;

        private Texture2D texture;

        private void Awake()
        {
            cameraManager.frameReceived += UpdateFrame;
        }

        private void UpdateFrame(ARCameraFrameEventArgs args)
        {
            GetFrame();
        }

        public unsafe Texture2D GetFrame()
        {
            // Пытаемся получить последнее изображение с камеры
            XRCameraImage image;
            if (!cameraManager.TryGetLatestImage(out image))
            {
                return null;
            }

            Vector2Int Resolution = new Vector2Int(1920, 1080);

            var format = TextureFormat.RGBA32;

            // Создаем текстуру
            if (texture == null || texture.width != Resolution.x || texture.height != Resolution.y)
            {
                texture = new Texture2D(Resolution.x, Resolution.y, format, false);
            }

            // Настраиваем параметры: задаем формат, отражаем по горизонтали (лево | право)
            var conversionParams = new XRCameraImageConversionParams(image, format, CameraImageTransformation.MirrorY);
            // Задаем downscale до нужного разрешения
            conversionParams.outputDimensions = new Vector2Int(Resolution.x, Resolution.y);

            // Получаем ссылку на массив байтов текущей текстуры
            var rawTextureData = texture.GetRawTextureData<byte>();
            try
            {
                // Копируем байты из изображения с камеры в текстуру
                image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
            }
            finally
            {
                // Высвобождаем память
                image.Dispose();
            }

            texture.Apply();

            return texture;
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
                return intrins.focalLength;
            }

            return Vector2.zero;
        }

        public bool IsCameraReady()
        {
            return texture != null;
        }
    }
}