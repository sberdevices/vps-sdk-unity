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
        private VPSLocalisationService localisationService;
        private ServiceProvider provider;

        private LocationState locationState;

        private SettingsVPS settings;

        private bool usingPhotoSeries;
        private bool sendOnlyFeatures;
        private bool alwaysForceVPS;

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

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="vps_servise">Parent GameObject, for start coroutine</param>
        /// <param name="vps_provider">Provider to get camera, gps and tracking</param>
        /// <param name="vps_settings">Settings</param>
        public VPSLocalisationAlgorithm(VPSLocalisationService vps_servise, ServiceProvider vps_provider, SettingsVPS vps_settings, bool usePhotoSeries, bool onlyFeatures,
                                        bool alwaysForce, bool sendGps)
        {
            localisationService = vps_servise;
            provider = vps_provider;

            usingPhotoSeries = usePhotoSeries;
            sendOnlyFeatures = onlyFeatures;
            alwaysForceVPS = alwaysForce;

            var gps = provider.GetGPS();
            if (gps != null)
                gps.SetEnable(sendGps);

            settings = vps_settings;
            provider.GetTracking().SetDefaultBuilding(vps_settings.defaultLocationId);

            locationState = new LocationState();

            localisationService.StartCoroutine(LocalisationRoutine());
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
        /// Main cycle. Check readiness every service, send request (force / not force), apply the resulting localization if success
        /// </summary>
        /// <returns>The routine.</returns>
        public IEnumerator LocalisationRoutine()
        {
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

            if (sendOnlyFeatures)
            {
                MobileVPS mobileVPS = provider.GetMobileVPS();
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
                yield return new WaitUntil(() => camera.IsCameraReady());

                // send localisation (force) or calibration (not force) request?
                bool isCalibration;
                if (alwaysForceVPS)
                    isCalibration = false;
                else
                    isCalibration = tracking.GetLocalTracking().IsLocalisedFloor;

                if (!isCalibration && usingPhotoSeries)
                {
                    LocalizationImagesCollector imagesCollector = provider.GetImageCollector();

                    System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                    stopWatch.Start();

                    yield return imagesCollector.StartCollectPhoto(provider, sendOnlyFeatures);

                    stopWatch.Stop();
                    TimeSpan collectImagesTS = stopWatch.Elapsed;

                    string collectImagesTime = String.Format("{0:N10}", collectImagesTS.TotalSeconds - imagesCollector.TotalWaitingTime);
                    VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] CollectSerial{0}Time {1}", sendOnlyFeatures ? "MVPS" : "Image", collectImagesTime);

                    VPSLogger.Log(LogLevel.DEBUG, "Sending VPS Localization Request...");
                    stopWatch.Start();
                    requestVPS.SetUrl(settings.Url);

                    yield return requestVPS.SendVpsLocalizationRequest(imagesCollector.GetLocalizationData());
                    VPSLogger.Log(LogLevel.DEBUG, "VPS Localization answer recieved!");

                    VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] LocalizationSerialResult {0}", requestVPS.GetStatus() == LocalisationStatus.VPS_READY ? 1 : 0);

                    if (requestVPS.GetStatus() == LocalisationStatus.VPS_READY)
                    {
                        var response = requestVPS.GetResponce();
                        tracking.SetGuidPointcloud(response.GuidPointcloud);

                        locationState.Status = LocalisationStatus.VPS_READY;
                        locationState.Error = ErrorCode.NO_ERROR;
                        locationState.Localisation = arRFoundationApplyer?.ApplyVPSTransform(response, imagesCollector.GetLocalizationData()[response.Img_id].pose);
                        

                        OnLocalisationHappend?.Invoke(locationState);
                    }
                    else
                    {
                        locationState.Status = LocalisationStatus.GPS_ONLY;
                        locationState.Error = requestVPS.GetErrorCode();
                        locationState.Localisation = null;

                        OnErrorHappend?.Invoke(requestVPS.GetErrorCode());
                        VPSLogger.LogFormat(LogLevel.DEBUG, "VPS Request Error: {0}", requestVPS.GetErrorCode());
                    }

                    stopWatch.Stop();
                    TimeSpan fullSeriaRequestTS = stopWatch.Elapsed;

                    string fullSeriaRequestTime = String.Format("{0:N10}", fullSeriaRequestTS.TotalSeconds - imagesCollector.TotalWaitingTime);
                    VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] FullSerial{0}RequestTime {1}", sendOnlyFeatures ? "MVPS" : "Image", fullSeriaRequestTime);

                    string fullSeriaRequestTimeWithWaiting = String.Format("{0:N10}", fullSeriaRequestTS.TotalSeconds);
                    VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] FullSerial{0}RequestTimeWithWaiting {1}", sendOnlyFeatures ? "MVPS" : "Image", fullSeriaRequestTimeWithWaiting);

                    yield return new WaitForSeconds(settings.Timeout - neuronTime);
                    neuronTime = 0;
                    continue;
                }

                System.Diagnostics.Stopwatch fullStopWatch = new System.Diagnostics.Stopwatch();
                fullStopWatch.Start();

                // remember current pose
                arRFoundationApplyer?.LocalisationStart();

                Meta = DataCollector.CollectData(provider, !isCalibration, sendOnlyFeatures);

                requestVPS.SetUrl(settings.Url);

                // if send features - send them
                if (sendOnlyFeatures)
                {
                    MobileVPS mobileVPS = provider.GetMobileVPS();
                    yield return new WaitUntil(() => ARFoundationCamera.semaphore.CheckState());
                    ARFoundationCamera.semaphore.TakeOne();

                    NativeArray<byte> featureExtractorInput = camera.GetBuffer(mobileVPS.imageFeatureExtractorRequirements);
                    if (featureExtractorInput == null || featureExtractorInput.Length == 0)
                    {
                        VPSLogger.Log(LogLevel.ERROR, "Cannot take camera image as ByteArray for FeatureExtractor");
                        yield return null;
                        continue;
                    }

                    NativeArray<byte> encoderInput = camera.GetBuffer(mobileVPS.imageEncoderRequirements);
                    if (encoderInput == null || encoderInput.Length == 0)
                    {
                        VPSLogger.Log(LogLevel.ERROR, "Cannot take camera image as ByteArray for Encoder");
                        yield return null;
                        continue;
                    }

                    yield return new WaitWhile(() => mobileVPS.ImageFeatureExtractorIsWorking || mobileVPS.ImageEncoderIsWorking);

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

                    System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                    stopWatch.Start();

                    while (!imageFeatureExtractorTask.IsCompleted || !imageEncoderTask.IsCompleted)
                        yield return null;

                    stopWatch.Stop();
                    TimeSpan neuronTS = stopWatch.Elapsed;
                    neuronTime = neuronTS.Seconds + neuronTS.Milliseconds / 1000f;

                    string neuronTimeStr = String.Format("{0:N10}", neuronTS.TotalSeconds);
                    VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] TotalInferenceTime {0}", neuronTimeStr);

                    ARFoundationCamera.semaphore.Free();
                    Embedding = EMBDCollector.ConvertToEMBD(1, 2, imageFeatureExtractorTask.Result.keyPoints, imageFeatureExtractorTask.Result.scores, imageFeatureExtractorTask.Result.descriptors, imageEncoderTask.Result.globalDescriptor);
                    VPSLogger.Log(LogLevel.DEBUG, "Sending VPS Request...");

                    yield return requestVPS.SendVpsRequest(Embedding, Meta);
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

                    VPSLogger.Log(LogLevel.DEBUG, "Sending VPS Request...");
                    yield return requestVPS.SendVpsRequest(Image, Meta);
                }

                fullStopWatch.Stop();
                TimeSpan fullRequestTS = fullStopWatch.Elapsed;

                string fullRequestTime = String.Format("{0:N10}", fullRequestTS.TotalSeconds);
                VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] FullSingle{0}RequestTime {1}", sendOnlyFeatures ? "MVPS" : "Image", fullRequestTime);

                VPSLogger.Log(LogLevel.DEBUG, "VPS answer recieved!");
                VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] LocalizationSingleResult {0}", requestVPS.GetStatus() == LocalisationStatus.VPS_READY ? 1 : 0);

                if (requestVPS.GetStatus() == LocalisationStatus.VPS_READY)
                {
                    var response = requestVPS.GetResponce();
                    tracking.SetGuidPointcloud(response.GuidPointcloud);

                    locationState.Status = LocalisationStatus.VPS_READY;
                    locationState.Error = ErrorCode.NO_ERROR;
                    locationState.Localisation = arRFoundationApplyer?.ApplyVPSTransform(response);

                    OnLocalisationHappend?.Invoke(locationState);
                }
                else
                {
                    locationState.Status = LocalisationStatus.GPS_ONLY;
                    locationState.Error = requestVPS.GetErrorCode();
                    locationState.Localisation = null;

                    OnErrorHappend?.Invoke(requestVPS.GetErrorCode());
                    VPSLogger.LogFormat(LogLevel.NONE, "VPS Request Error: {0}", requestVPS.GetErrorCode());
                }

                yield return new WaitForSeconds(settings.Timeout);
            }
        }
    }
}