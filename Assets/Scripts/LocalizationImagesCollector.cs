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
        // Использовать угол или по таймауту?
        private bool useAngle = true;
        // Задержка между фотографиями
        private float timeout = 1;
        // Расстояние между фотографиями
        private const float angle = 25f;

        private float predAngle = 0f;

        private const float MaxAngleX = 30;
        private const float MaxAngleZ = 30;

        public static event System.Action OnPhotoAdded;
        public static event System.Action OnSeriaIsReady;

        public LocalizationImagesCollector(int PhotosInSeria, bool usingAngle = false)
        {
            photosInSeria = PhotosInSeria;
            localizationData = new List<RequestLocalizationData>();
            for (int i = 0; i < photosInSeria; i++)
            {
                localizationData.Add(new RequestLocalizationData());
            }
            useAngle = usingAngle;
        }

        /// <summary>
        /// Делаем фото и собираем в список с интервалом timeout
        /// </summary>
        /// <returns>The image.</returns>
        /// <param name="provider">Provider.</param>
        public IEnumerator StartCollectPhoto(ServiceProvider provider, bool sendOnlyFeatures)
        {
            var tracking = provider.GetTracking();

            Debug.Log("Start collect photo");
            for (int i = 0; i < photosInSeria; i++)
            {
                if (i != 0)
                {
                    if (useAngle)
                    {
                        Vector3 curAngle;
                        do
                        {
                            yield return null;
                            curAngle = tracking.GetLocalTracking().Rotation.eulerAngles;
                        }
                        while (!CheckTakePhotoConditions(curAngle));
                        yield return new WaitUntil(() => Mathf.Abs(Mathf.DeltaAngle(predAngle, tracking.GetLocalTracking().Rotation.eulerAngles.y)) > angle);
                    }
                    else
                    {
                        yield return new WaitForSeconds(timeout);
                    }
                }

                yield return new WaitUntil(() => CheckTakePhotoConditions(tracking.GetLocalTracking().Rotation.eulerAngles));
                yield return AddPhoto(provider, sendOnlyFeatures, localizationData[i]);
                OnPhotoAdded?.Invoke();
                predAngle = tracking.GetLocalTracking().Rotation.eulerAngles.y;
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

            string Meta;
            Pose pose;
            // если отправляем фичи - получаем их
            byte[] Embedding;
            byte[] ImageBytes;
            if (sendOnlyFeatures)
            {
                yield return new WaitUntil(() => ARFoundationCamera.semaphore.CheckState());
                ARFoundationCamera.semaphore.TakeOne();

                Meta = DataCollector.CollectData(provider, true, sendOnlyFeatures);
                pose = provider.GetARFoundationApplyer().GetCurrentPose();

                NativeArray<byte> input = camera.GetImageArray();
                if (input == null || input.Length == 0)
                {
                    Debug.LogError("Cannot take camera image as ByteArray");
                    yield break;
                }

                var task = provider.GetMobileVPS().GetFeaturesAsync(input);
                while (!task.IsCompleted)
                    yield return null;

                ARFoundationCamera.semaphore.Free();
                Embedding = EMBDCollector.ConvertToEMBD(0, 0, task.Result.keyPoints, task.Result.scores, task.Result.descriptors, task.Result.globalDescriptor);
                ImageBytes = null;
            }
            else
            {
                Meta = DataCollector.CollectData(provider, true, sendOnlyFeatures);
                pose = provider.GetARFoundationApplyer().GetCurrentPose();
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
            currentData.pose = pose;
            currentData.Embedding = Embedding;
        }

        private bool CheckTakePhotoConditions(Vector3 curAngle)
        {
            return (curAngle.x < MaxAngleX || curAngle.x > 360 - MaxAngleX) &&
            (curAngle.z < MaxAngleZ || curAngle.z > 360 - MaxAngleZ);
        }
    }
}
