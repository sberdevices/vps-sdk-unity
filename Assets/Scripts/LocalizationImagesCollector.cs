using System.Collections;
using System.Collections.Generic;
using System.IO;
using ARVRLab.ARVRLab.VPSService.JSONs;
using Unity.Collections;
using UnityEngine;

namespace ARVRLab.VPSService
{
    /// <summary>
    /// Собирает серию фотографий для отправки на сервер локализации
    /// </summary>
    public class LocalizationImagesCollector
    {
        // Кол-во фотографий в серии
        private int photosInSeria;
        // Список фото, меты и pose, откуда были сделаны
        private List<RequestLocalizationData> localizationData;
        // Использовать дистанцию или по таймауту?
        //private bool useDistance = true;
        // Задержка между фотографиями
        private float timeout = 1;
        // Расстояние между фотографиями
        //private float distance = 0.5f;

        private MobileVPS mobileVPS;

        //private Vector3 predPos = Vector3.zero;

        public static event System.Action OnPhotoAdded;
        public static event System.Action OnSeriaIsReady;

        public LocalizationImagesCollector(int PhotosInSeria)
        {
            photosInSeria = PhotosInSeria;
            localizationData = new List<RequestLocalizationData>();
            for (int i = 0; i < photosInSeria; i++)
            {
                localizationData.Add(new RequestLocalizationData());
            }
            mobileVPS = new MobileVPS();
        }

        /// <summary>
        /// Делаем фото и собираем в список с интервалом timeout
        /// </summary>
        /// <returns>The image.</returns>
        /// <param name="provider">Provider.</param>
        public IEnumerator StartCollectPhoto(ServiceProvider provider, bool sendOnlyFeatures)
        {
            var arFoundationApplyer = provider.GetARFoundationApplyer();

            Debug.Log("Start collect photo");
            for (int i = 0; i < photosInSeria; i++)
            {
                yield return AddPhoto(provider, sendOnlyFeatures, localizationData[i]);
                yield return new WaitForSeconds(timeout);
                OnPhotoAdded?.Invoke();
            }
            OnSeriaIsReady?.Invoke();
        }

        public List<RequestLocalizationData> GetLocalizationData()
        {
            return localizationData;
        }

        private IEnumerator AddPhoto(ServiceProvider provider, bool sendOnlyFeatures, RequestLocalizationData currentData)
        {
            ICamera camera = provider.GetCamera();

            string Meta = DataCollector.CollectData(provider, true);

            // если отправляем фичи - получаем их
            byte[] Embedding;
            byte[] ImageBytes;
            if (sendOnlyFeatures)
            {
                NativeArray<byte> input = camera.GetImageArray();
                if (input == null || input.Length == 0)
                {
                    Debug.LogError("Cannot take camera image as ByteArray");
                    yield break;
                }

                var task = mobileVPS.GetFeaturesAsync(input);
                while (!task.IsCompleted)
                    yield return null;

                Embedding = EMBDCollector.ConvertToEMBD(0, 0, task.Result.keyPoints, task.Result.scores, task.Result.descriptors, task.Result.globalDescriptor);
                ImageBytes = null;
            }
            else
            {
                Texture2D Image = camera.GetFrame();
                if (Image == null)
                {
                    Debug.LogError("Image from camera is not available");
                    yield break;
                }

                ImageBytes = Image.EncodeToJPG();
                Embedding = null;
            }

            currentData.image = ImageBytes;
            currentData.meta = Meta;
            currentData.pose = provider.GetARFoundationApplyer().GetCurrentPose();
            currentData.Embedding = Embedding;
        }
    }
}
