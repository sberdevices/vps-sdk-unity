using System;
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
        // Number of photos in serial
        private int photosInSerial;
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
        public static event System.Action OnSerialIsReady;

        public float TotalWaitingTime = 0;

        System.Diagnostics.Stopwatch stopWatch;
        float neuronTime = 0;

        public LocalizationImagesCollector(int PhotosInSerial, bool usingAngle = false)
        {
            photosInSerial = PhotosInSerial;
            localizationData = new List<RequestLocalizationData>();
            for (int i = 0; i < photosInSerial; i++)
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
            TotalWaitingTime = 0;
            var tracking = provider.GetTracking();

            VPSLogger.Log(LogLevel.DEBUG, "Start collect photo serial");
            for (int i = 0; i < photosInSerial; i++)
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
                        float newTimeout = timeout - neuronTime;
                        VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] SerialPhotoTimeout {0}", newTimeout);
                        yield return new WaitForSeconds(newTimeout);
                        TotalWaitingTime += newTimeout;
                        neuronTime = 0;
                    }
                }

                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();

                while (!CheckTakePhotoConditions(tracking.GetLocalTracking().Rotation.eulerAngles))
                    yield return null;

                stopWatch.Stop();
                TimeSpan waitingTS = stopWatch.Elapsed;

                string waitingTime = String.Format("{0:N10}", waitingTS.TotalSeconds);
                VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] TotalWaitingTime {0}", waitingTime);
                TotalWaitingTime += (float)waitingTS.TotalSeconds;

                stopWatch.Restart();

                yield return AddPhoto(provider, sendOnlyFeatures, localizationData[i]);

                stopWatch.Stop();
                TimeSpan addPhotoTS = stopWatch.Elapsed;

                string addPhotoTime = String.Format("{0:N10}", addPhotoTS.TotalSeconds);
                VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] AddPhotoTime{0} {1}", sendOnlyFeatures ? "MVPS" : "Image", addPhotoTime);

                OnPhotoAdded?.Invoke();
                predAngle = tracking.GetLocalTracking().Rotation.eulerAngles.y;
            }
            OnSerialIsReady?.Invoke();
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
                MobileVPS mobileVPS = provider.GetMobileVPS();
                yield return new WaitUntil(() => ARFoundationCamera.semaphore.CheckState());
                ARFoundationCamera.semaphore.TakeOne();

                Meta = DataCollector.CollectData(provider, true, sendOnlyFeatures);
                pose = provider.GetARFoundationApplyer().GetCurrentPose();

                NativeArray<byte> featureExtractorInput = camera.GetBuffer(mobileVPS.imageFeatureExtractorRequirements);
                if (featureExtractorInput == null || featureExtractorInput.Length == 0)
                {
                    VPSLogger.Log(LogLevel.ERROR, "Cannot take camera image as ByteArray for FeatureExtractor");
                    yield break;
                }

                NativeArray<byte> encoderInput = camera.GetBuffer(mobileVPS.imageEncoderRequirements);
                if (encoderInput == null || encoderInput.Length == 0)
                {
                    VPSLogger.Log(LogLevel.ERROR, "Cannot take camera image as ByteArray for Encoder");
                    yield break;
                }

                yield return new WaitWhile(() => mobileVPS.ImageFeatureExtractorIsWorking || mobileVPS.ImageEncoderIsWorking);

                var preprocessTask = mobileVPS.StartPreprocess(featureExtractorInput, encoderInput);
                while (!preprocessTask.IsCompleted)
                    yield return null;

                if (!preprocessTask.Result)
                {
                    yield break;
                }

                var imageFeatureExtractorTask = mobileVPS.GetFeaturesAsync();
                var imageEncoderTask = mobileVPS.GetGlobalDescriptorAsync();

                stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();

                while (!imageFeatureExtractorTask.IsCompleted || !imageEncoderTask.IsCompleted)
                    yield return null;

                stopWatch.Stop();
                TimeSpan neuronTS = stopWatch.Elapsed;
                neuronTime = (float)neuronTS.TotalSeconds;

                string neuronTimeStr = String.Format("{0:N10}", neuronTS.TotalSeconds);
                VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] TotalInferenceTime {0}", neuronTimeStr);

                ARFoundationCamera.semaphore.Free();
                Embedding = EMBDCollector.ConvertToEMBD(1, 2, imageFeatureExtractorTask.Result.keyPoints, imageFeatureExtractorTask.Result.scores, imageFeatureExtractorTask.Result.descriptors, imageEncoderTask.Result.globalDescriptor);
                ImageBytes = null;
            }
            else
            {
                Meta = DataCollector.CollectData(provider, true, sendOnlyFeatures);
                pose = provider.GetARFoundationApplyer().GetCurrentPose();
                Texture2D Image = camera.GetFrame(provider.GetTextureRequirement());
                if (Image == null)
                {
                    VPSLogger.Log(LogLevel.ERROR, "Image from camera is not available");
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
