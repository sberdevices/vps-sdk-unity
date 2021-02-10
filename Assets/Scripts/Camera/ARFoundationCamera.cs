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
        [Tooltip("Разрешение, в котором будут отправляться фотографии")]
        private Vector2Int desiredResolution = new Vector2Int(960, 540);
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
            buffer = new NativeArray<byte>(desiredResolution.x * desiredResolution.y, Allocator.Persistent);

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
                var _1920x1080 = configurations.FirstOrDefault(a => a.width == 1920 && a.height == 1080);
                cameraManager.currentConfiguration = _1920x1080;
                if (cameraManager.currentConfiguration == null)
                {
                    Debug.LogError("Не удалось получить HD разрешение!");
                    // Берем наилучшее возможное
                    var bestConfiguration = configurations.OrderByDescending(a => a.width * a.height).FirstOrDefault();
                    cameraManager.currentConfiguration = bestConfiguration;
                    resizeCoefficient = (float)TagretResolution.width / (float)bestConfiguration.width;
                }
                else
                {
                    resizeCoefficient = (float)TagretResolution.width / (float)_1920x1080.width;
                }
            }
        }

        /// <summary>
        /// Обновляет значение texture
        /// </summary>
        private unsafe void UpdateFrame(ARCameraFrameEventArgs args)
        {
            if (!semaphore.CheckState())
                return;

            semaphore.TakeOne();

            // Пытаемся получить последнее изображение с камеры
            XRCpuImage image;
            if (!cameraManager.TryAcquireLatestCpuImage(out image))
            {
                Debug.Log("Не удалось получить изображение с камеры!");
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
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                // Высвобождаем память
                image.Dispose();
            }

            buffer.CopyFrom(raw);
            texture.Apply();
            semaphore.Free();
        }

        public Texture2D GetFrame()
        {
            // Необходимо создать новую текстуру, так как старая в формате R8 и не принимает каналы g и b
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