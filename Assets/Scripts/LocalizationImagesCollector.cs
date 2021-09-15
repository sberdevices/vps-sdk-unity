using System.Collections;
using System.Collections.Generic;
using System.IO;
using ARVRLab.ARVRLab.VPSService.JSONs;
using Unity.Collections;
using UnityEngine;

namespace ARVRLab.VPSService
{
    /// <summary>
    /// Collects a series of photos to send to the localization server
    /// </summary>
    public class LocalizationImagesCollector
    {
        // Number of photos in seria
        private int photosInSeria;
        // List of photo, meta and poses
        private List<RequestLocalizationData> localizationData;
        // Using angle or timeout between photos?
        private bool useAngle = true;
        // Delay between photos
        private float timeout = 1;
        // Angle between photos
        private const float angle = 25f;

        private float predAngle = 0f;

        private const float MaxAngleX = 30;
        private const float MaxAngleZ = 30;

        public static event System.Action OnPhotoAdded;
        public static event System.Action OnSeriaIsReady;

        System.Diagnostics.Stopwatch stopWatch;
        float neuronTime = 0;

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
        /// Take photo and add it to list
        /// </summary>
        /// <returns>The image.</returns>
        /// <param name="provider">Provider.</param>
        public IEnumerator StartCollectPhoto(ServiceProvider provider, bool sendOnlyFeatures)
        {
            var tracking = provider.GetTracking();

            VPSLogger.Log(LogLevel.DEBUG, "Start collect photo");
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
                        yield return new WaitForSeconds(timeout - neuronTime);
                        neuronTime = 0;
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
            // if send features - get them
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

                MobileVPS mobileVPS = provider.GetMobileVPS();
                yield return new WaitWhile(() => mobileVPS.Working);
                var task = mobileVPS.GetFeaturesAsync(input);
                stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                while (!task.IsCompleted)
                    yield return null;
                stopWatch.Stop();
                neuronTime = stopWatch.Elapsed.Seconds + stopWatch.Elapsed.Milliseconds / 1000f;
                VPSLogger.LogFormat(LogLevel.VERBOSE, "Neuron time = {0:f3}", neuronTime);

                ARFoundationCamera.semaphore.Free();
                Embedding = EMBDCollector.ConvertToEMBD(1, 0, task.Result.keyPoints, task.Result.scores, task.Result.descriptors, task.Result.globalDescriptor);
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

                ImageBytes = Image.EncodeToJPG(100);
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
