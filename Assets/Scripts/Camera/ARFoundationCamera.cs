﻿using System;
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
        private ARCameraManager cameraManager;
        private Texture2D texture;

        private NativeArray<XRCameraConfiguration> configurations;

        private void Awake()
        {
            cameraManager = FindObjectOfType<ARCameraManager>();
            if (!cameraManager)
            {
                Debug.LogError("Can't find ARCameraManager on scene!");
                return;
            }

            cameraManager.frameReceived += UpdateFrame;
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
        }

        /// <summary>
        /// Обновляет значение texture
        /// </summary>
        /// <returns>The frame.</returns>
        private unsafe void UpdateFrame(ARCameraFrameEventArgs args)
        {
            // Пытаемся получить последнее изображение с камеры
            XRCameraImage image;
            if (!cameraManager.TryGetLatestImage(out image))
            {
                return;
            }

            Vector2Int Resolution = new Vector2Int(1920, 1080);

            var format = TextureFormat.RGBA32;

            // Создаем текстуру
            if (texture == null || texture.width != Resolution.x || texture.height != Resolution.y)
            {
                texture = new Texture2D(Resolution.x, Resolution.y, format, false);
            }

            // Настраиваем параметры: задаем формат, отражаем по горизонтали (лево | право)
            var conversionParams = new XRCameraImageConversionParams(image, format, CameraImageTransformation.None);
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
        }

        public Texture2D GetFrame()
        {
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
                return intrins.principalPoint;
            }

            return Vector2.zero;
        }

        public bool IsCameraReady()
        {
            return texture != null;
        }
    }
}