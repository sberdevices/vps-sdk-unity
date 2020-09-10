using System.Collections;
using System.Collections.Generic;
using System.IO;
using ARVRLab.ARVRLab.VPSService.JSONs;
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
        private List<RequestLocalizationData> localizationData = new List<RequestLocalizationData>();
        // Использовать дистанцию или по таймауту?
        private bool useDistance = true;
        // Задержка между фотографиями
        private float timeout = 1;
        // Расстояние между фотографиями
        private float distance = 0.5f;

        private Vector3 predPos = Vector3.zero;

        public static event System.Action OnPhotoAdded;
        public static event System.Action OnSeriaIsReady;

        public LocalizationImagesCollector(int PhotosInSeria)
        {
            photosInSeria = PhotosInSeria;
        }

        /// <summary>
        /// Делаем фото и собираем в список с интервалом timeout
        /// </summary>
        /// <returns>The image.</returns>
        /// <param name="provider">Provider.</param>
        public IEnumerator StartCollectPhoto(ServiceProvider provider)
        {
            localizationData.Clear();
            Texture2D Image;
            string Meta;

            // Повторная проверка доступности камеры
            var camera = provider.GetCamera();
            if (camera == null)
            {
                Debug.LogError("Camera is not available");
                yield break;
            }

            // Повторная проверка доступности трекинга
            var tracking = provider.GetTracking();
            if (tracking == null)
            {
                Debug.LogError("Tracking is not available");
                yield break;
            }

            var arFoundationApplyer = provider.GetARFoundationApplyer();
            useDistance = arFoundationApplyer != null;
            if (!useDistance)
            {
                Debug.Log("ArFoundationApplyer is not available. Using timeout");
            }

            Debug.Log("Start collect photo");
            while (localizationData.Count < photosInSeria)
            {
                yield return new WaitUntil(() => camera.IsCameraReady());

                Image = camera.GetFrame();
                if (Image == null)
                {
                    Debug.LogError("Image from camera is not available");
                    yield break;
                }

                Meta = DataCollector.CollectData(provider, true);

                localizationData.Add(new RequestLocalizationData(Image.EncodeToJPG(), Meta, provider.GetARFoundationApplyer().GetCurrentPose()));

                predPos = arFoundationApplyer.GetCurrentPose().position;

                if (useDistance)
                {
                    yield return new WaitUntil(() => Vector3.Distance(predPos, arFoundationApplyer.GetCurrentPose().position) > distance);
                }
                else
                {
                    yield return new WaitForSeconds(timeout);
                }
                OnPhotoAdded?.Invoke();
            }
            OnSeriaIsReady?.Invoke();
        }

        public List<RequestLocalizationData> GetLocalizationData()
        {
            return localizationData;
        }
    }
}
