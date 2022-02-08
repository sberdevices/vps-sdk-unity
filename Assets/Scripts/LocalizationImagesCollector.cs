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
                        yield return new WaitForSeconds(timeout - neuronTime);
                        neuronTime = 0;
                    }
                }

                yield return new WaitUntil(() => CheckTakePhotoConditions(tracking.GetLocalTracking().Rotation.eulerAngles));
                yield return AddPhoto(provider, sendOnlyFeatures, localizationData[i], i);
                OnPhotoAdded?.Invoke();
                predAngle = tracking.GetLocalTracking().Rotation.eulerAngles.y;
            }
            OnSerialIsReady?.Invoke();
        }

        public List<RequestLocalizationData> GetLocalizationData()
        {
            return localizationData;
        }

        private IEnumerator AddPhoto(ServiceProvider provider, bool sendOnlyFeatures, RequestLocalizationData currentData, int index)
        {
            ICamera camera = provider.GetCamera();

            string Meta;
            Pose pose;
            // if send features - get them
            byte[] Embedding;
            byte[] ImageBytes;

            // todo: merge this logic with VPSLocalisationAlgorithm
            if (sendOnlyFeatures)
            {
                MobileVPS mobileVPS = provider.GetMobileVPS();
                yield return new WaitUntil(() => ARFoundationCamera.semaphore.CheckState());
                ARFoundationCamera.semaphore.TakeOne();

                var metaMsg = DataCollector.CollectData(provider, true, sendOnlyFeatures);
                Meta = DataCollector.Serialize(metaMsg);
                pose = provider.GetARFoundationApplyer().GetCurrentPose();

                NativeArray<byte> featureExtractorInput = camera.GetBuffer(mobileVPS.imageFeatureExtractorRequirements);
                if (featureExtractorInput == null || featureExtractorInput.Length == 0)
                {
                    VPSLogger.Log(LogLevel.ERROR, "Cannot take camera image as ByteArray for FeatureExtractor");
                    yield break;
                }

                if (DebugUtils.SaveImagesLocaly)
                {
                    VPSLogger.Log(LogLevel.VERBOSE, "Saving FeatureExtractor image before sending...");
                    DebugUtils.SaveDebugImage(featureExtractorInput, mobileVPS.imageFeatureExtractorRequirements, metaMsg.data.id, $"features_{index}");
                }

                NativeArray<byte> encoderInput = camera.GetBuffer(mobileVPS.imageEncoderRequirements);
                if (encoderInput == null || encoderInput.Length == 0)
                {
                    VPSLogger.Log(LogLevel.ERROR, "Cannot take camera image as ByteArray for Encoder");
                    yield break;
                }

                if (DebugUtils.SaveImagesLocaly)
                {
                    VPSLogger.Log(LogLevel.VERBOSE, "Saving Encoder image before sending...");
                    DebugUtils.SaveDebugImage(featureExtractorInput, mobileVPS.imageFeatureExtractorRequirements, metaMsg.data.id, $"encoder_{index}");

                    // also save json, we are done here
                    DebugUtils.SaveJson(metaMsg, index.ToString());
                }

                yield return new WaitWhile(() => mobileVPS.ImageFeatureExtractorIsWorking || mobileVPS.ImageEncoderIsWorking);

                stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();

                var preprocessTask = mobileVPS.StartPreprocess(featureExtractorInput, encoderInput);
                while (!preprocessTask.IsCompleted)
                    yield return null;

                if (!preprocessTask.Result)
                {
                    yield break;
                }

                var imageFeatureExtractorTask = mobileVPS.GetFeaturesAsync();
                var imageEncoderTask = mobileVPS.GetGlobalDescriptorAsync();
                while (!imageFeatureExtractorTask.IsCompleted || !imageEncoderTask.IsCompleted)
                    yield return null;

                stopWatch.Stop();
                neuronTime = stopWatch.Elapsed.Seconds + stopWatch.Elapsed.Milliseconds / 1000f;
                VPSLogger.LogFormat(LogLevel.VERBOSE, "Neuron time = {0:f3}", neuronTime);

                ARFoundationCamera.semaphore.Free();
                Embedding = EMBDCollector.ConvertToEMBD(1, 2, imageFeatureExtractorTask.Result.keyPoints, imageFeatureExtractorTask.Result.scores,
                    imageFeatureExtractorTask.Result.descriptors, imageEncoderTask.Result.globalDescriptor);


                if (DebugUtils.SaveImagesLocaly)
                {
                    VPSLogger.Log(LogLevel.VERBOSE, "Saving embeding before sending...");
                    DebugUtils.SaveDebugEmbd(Embedding, metaMsg.data.id);
                }

                ImageBytes = null;
            }
            else
            {
                var metaMsg = DataCollector.CollectData(provider, true, sendOnlyFeatures);
                Meta = DataCollector.Serialize(metaMsg);
                pose = provider.GetARFoundationApplyer().GetCurrentPose();
                Texture2D Image = camera.GetFrame(provider.GetTextureRequirement());
                if (Image == null)
                {
                    VPSLogger.Log(LogLevel.ERROR, "Image from camera is not available");
                    yield break;
                }

                if (DebugUtils.SaveImagesLocaly)
                {
                    VPSLogger.Log(LogLevel.VERBOSE, "Saving image before sending...");
                    DebugUtils.SaveDebugImage(Image, metaMsg.data.id);
                    DebugUtils.SaveJson(metaMsg);
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
