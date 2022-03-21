using System.Collections;
using System.Collections.Generic;
using ARVRLab.ARVRLab.VPSService.JSONs;
using UnityEngine;
using System.IO;
using TensorFlowLite;
using Unity.Collections;
using System;

namespace ARVRLab.VPSService
{
    /// <summary>
    /// Internal management VPS
    /// </summary>
    public class VPSLocalisationAlgorithm
    {
        private const float MaxAngleX = 30;
        private const float MaxAngleZ = 30;

        private VPSLocalisationService localisationService;
        private ServiceProvider provider;

        private LocationState locationState;

        private SettingsVPS settings;

        private bool sendOnlyFeatures;

        IRequestVPS requestVPS = new HttpClientRequestVPS();

        /// <summary>
        /// Event localisation error
        /// </summary>
        public event System.Action<ErrorCode> OnErrorHappend;

        /// <summary>
        /// Event localisation success
        /// </summary>
        public event System.Action<LocationState> OnLocalisationHappend;

        float neuronTime = 0;

        const int failsToReset = 5;
        int currentFailsCount = 0;
        bool isLocalization = true;

        #region Metrics

        int attemptCount;
        private const string FullLocalizationStopWatch = "FullLocalizationStopWatch";
        private const string TotalWaitingTime = "TotalWaitingTime";
        private const string TotalInferenceTime = "TotalInferenceTime";

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="vps_servise">Parent GameObject, for start coroutine</param>
        /// <param name="vps_provider">Provider to get camera, gps and tracking</param>
        /// <param name="vps_settings">Settings</param>
        public VPSLocalisationAlgorithm(VPSLocalisationService vps_servise, ServiceProvider vps_provider, SettingsVPS vps_settings, bool onlyFeatures,
                                        bool sendGps)
        {
            localisationService = vps_servise;
            provider = vps_provider;

            sendOnlyFeatures = onlyFeatures;

            var gps = provider.GetGPS();
            if (gps != null)
                gps.SetEnable(sendGps);

            settings = vps_settings;

            locationState = new LocationState();

            localisationService.StartCoroutine(LocalisationRoutine());

            neuronTime = 0;

            OnErrorHappend += (error) => ResetIfFails(failsToReset);

            currentFailsCount = 0;
            isLocalization = true;
        }

        public void Stop()
        {
            provider.GetMobileVPS()?.StopTask();
            localisationService.StopAllCoroutines();
            ARFoundationCamera.semaphore.Free();
        }

        /// <summary>
        /// Get latest available Location state (updated in LocalisationRoutine())
        /// </summary>
        /// <returns></returns>
        public LocationState GetLocationRequest()
        {
            return locationState;
        }

        /// <summary>
        /// Main cycle. Check readiness every service, send request, apply the resulting localization if success
        /// </summary>
        /// <returns>The routine.</returns>
        public IEnumerator LocalisationRoutine()
        {
            attemptCount = 0;
            MetricsCollector.Instance.StartStopwatch(FullLocalizationStopWatch);

            Texture2D Image;
            string Meta;
            byte[] Embedding;

            var camera = provider.GetCamera();
            if (camera == null)
            {
                OnErrorHappend?.Invoke(ErrorCode.NO_CAMERA);
                VPSLogger.Log(LogLevel.ERROR, "Camera is not available");
                yield break;
            }

            MobileVPS mobileVPS = provider.GetMobileVPS();
            if (sendOnlyFeatures)
            {
                camera.Init(new VPSTextureRequirement[] { mobileVPS.imageFeatureExtractorRequirements, mobileVPS.imageEncoderRequirements });
            }
            else
            {
                camera.Init(new VPSTextureRequirement[] { provider.GetTextureRequirement() });
            }

            var tracking = provider.GetTracking();
            if (tracking == null)
            {
                OnErrorHappend?.Invoke(ErrorCode.TRACKING_NOT_AVALIABLE);
                VPSLogger.Log(LogLevel.ERROR, "Tracking is not available");
                yield break;
            }

            var arRFoundationApplyer = provider.GetARFoundationApplyer();

            while (true)
            {
                while (!camera.IsCameraReady())
                    yield return null;

                MetricsCollector.Instance.StartStopwatch(TotalWaitingTime);

                while (!CheckTakePhotoConditions(tracking.GetLocalTracking().Rotation.eulerAngles))
                    yield return null;

                MetricsCollector.Instance.StopStopwatch(TotalWaitingTime);
                VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0} {1}", TotalWaitingTime, MetricsCollector.Instance.GetStopwatchSecondsAsString(TotalWaitingTime));

                var metaMsg = DataCollector.CollectData(provider);
                Meta = DataCollector.Serialize(metaMsg);

                requestVPS.SetUrl(settings.Url);

                // if send features - send them
                if (sendOnlyFeatures)
                {
                    while (!ARFoundationCamera.semaphore.CheckState())
                        yield return null;
                    ARFoundationCamera.semaphore.TakeOne();

                    NativeArray<byte> featureExtractorInput = camera.GetBuffer(mobileVPS.imageFeatureExtractorRequirements);
                    if (featureExtractorInput == null || featureExtractorInput.Length == 0)
                    {
                        VPSLogger.Log(LogLevel.ERROR, "Cannot take camera image as ByteArray for FeatureExtractor");
                        yield return null;
                        continue;
                    }

                    if (DebugUtils.SaveImagesLocaly)
                    {
                        VPSLogger.Log(LogLevel.VERBOSE, "Saving FeatureExtractor image before sending...");
                        DebugUtils.SaveDebugImage(featureExtractorInput, mobileVPS.imageFeatureExtractorRequirements, metaMsg.data.id, "features");
                    }

                    NativeArray<byte> encoderInput = camera.GetBuffer(mobileVPS.imageEncoderRequirements);
                    if (encoderInput == null || encoderInput.Length == 0)
                    {
                        VPSLogger.Log(LogLevel.ERROR, "Cannot take camera image as ByteArray for Encoder");
                        yield return null;
                        continue;
                    }

                    if (DebugUtils.SaveImagesLocaly)
                    {
                        VPSLogger.Log(LogLevel.VERBOSE, "Saving Encoder image before sending...");
                        DebugUtils.SaveDebugImage(featureExtractorInput, mobileVPS.imageFeatureExtractorRequirements, metaMsg.data.id, "encoder");

                        DebugUtils.SaveJson(metaMsg);
                    }

                    while (mobileVPS.ImageFeatureExtractorIsWorking || mobileVPS.ImageEncoderIsWorking)
                        yield return null;

                    var preprocessTask = mobileVPS.StartPreprocess(featureExtractorInput, encoderInput);
                    while (!preprocessTask.IsCompleted)
                        yield return null;

                    if (!preprocessTask.Result)
                    { 
                        yield return null;
                        continue;
                    }

                    var imageFeatureExtractorTask = mobileVPS.GetFeaturesAsync();
                    var imageEncoderTask = mobileVPS.GetGlobalDescriptorAsync();

                    MetricsCollector.Instance.StartStopwatch(TotalInferenceTime);

                    while (!imageFeatureExtractorTask.IsCompleted || !imageEncoderTask.IsCompleted)
                        yield return null;

                    MetricsCollector.Instance.StopStopwatch(TotalInferenceTime);
                    TimeSpan neuronTS = MetricsCollector.Instance.GetStopwatchTimespan(TotalInferenceTime);
                    neuronTime = neuronTS.Seconds + neuronTS.Milliseconds / 1000f;

                    VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0} {1}", TotalInferenceTime, MetricsCollector.Instance.GetStopwatchSecondsAsString(TotalInferenceTime));

                    ARFoundationCamera.semaphore.Free();
                    Embedding = EMBDCollector.ConvertToEMBD(1, 2, imageFeatureExtractorTask.Result.keyPoints, imageFeatureExtractorTask.Result.scores,
                        imageFeatureExtractorTask.Result.descriptors, imageEncoderTask.Result.globalDescriptor);

                    if (DebugUtils.SaveImagesLocaly)
                    {
                        VPSLogger.Log(LogLevel.VERBOSE, "Saving embeding before sending...");
                        DebugUtils.SaveDebugEmbd(Embedding, metaMsg.data.id);
                    }

                    VPSLogger.Log(LogLevel.DEBUG, "Sending VPS Request...");

                    localisationService.StartCoroutine(requestVPS.SendVpsRequest(Embedding, Meta, () => Callback(tracking, arRFoundationApplyer)));
                }
                // if not - send only photo and meta
                else
                {
                    Image = camera.GetFrame(provider.GetTextureRequirement());

                    if (Image == null)
                    {
                        VPSLogger.Log(LogLevel.ERROR, "Image from camera is not available");
                        yield return null;
                        continue;
                    }

                    if (DebugUtils.SaveImagesLocaly)
                    {
                        VPSLogger.Log(LogLevel.VERBOSE, "Saving image before sending...");
                        DebugUtils.SaveDebugImage(Image, metaMsg.data.id);
                        DebugUtils.SaveJson(metaMsg);
                    }

                    VPSLogger.Log(LogLevel.DEBUG, "Sending VPS Request...");
                    localisationService.StartCoroutine(requestVPS.SendVpsRequest(Image, Meta, () => Callback(tracking, arRFoundationApplyer)));
                }

                if (isLocalization)
                    yield return new WaitForSeconds(settings.localizationTimeout - neuronTime); 
                else
                    yield return new WaitForSeconds(settings.calibrationTimeout - neuronTime);
            }
        }

        private void Callback(ITracking tracking, ARFoundationApplyer arRFoundationApplyer)
        {
            attemptCount++;

            if (requestVPS.GetStatus() == LocalisationStatus.VPS_READY)
            {
                #region Metrics
                if (!tracking.GetLocalTracking().IsLocalisedFloor)
                {
                    MetricsCollector.Instance.StopStopwatch(FullLocalizationStopWatch);

                    VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0} {1}", FullLocalizationStopWatch, MetricsCollector.Instance.GetStopwatchSecondsAsString(FullLocalizationStopWatch));
                    VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] SerialAttemptCount {0}", attemptCount);
                }

                #endregion

                isLocalization = false;
                currentFailsCount = 0;

                var response = requestVPS.GetResponce();
                tracking.Localize();

                locationState.Status = LocalisationStatus.VPS_READY;
                locationState.Error = ErrorCode.NO_ERROR;
                locationState.Localisation = arRFoundationApplyer?.ApplyVPSTransform(response);

                OnLocalisationHappend?.Invoke(locationState);
                VPSLogger.Log(LogLevel.NONE, "VPS localization successful");
            }
            else
            {
                locationState.Status = LocalisationStatus.GPS_ONLY;
                locationState.Error = requestVPS.GetErrorCode();
                locationState.Localisation = null;

                OnErrorHappend?.Invoke(requestVPS.GetErrorCode());
                VPSLogger.LogFormat(LogLevel.NONE, "VPS Request Error: {0}", requestVPS.GetErrorCode());
            }
        }

        private bool CheckTakePhotoConditions(Vector3 curAngle)
        {
            return (curAngle.x < MaxAngleX || curAngle.x > 360 - MaxAngleX) &&
            (curAngle.z < MaxAngleZ || curAngle.z > 360 - MaxAngleZ);
        }

        private void ResetIfFails(int count)
        {
            if (isLocalization)
                return;

            currentFailsCount++;
            if (currentFailsCount >= count)
            {
                currentFailsCount = 0;
                provider.ResetSessionId();
                isLocalization = true;
            }
        }
    }
}