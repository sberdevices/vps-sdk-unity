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

        System.Diagnostics.Stopwatch stopWatch;
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

            provider.GetGPS().SetEnable(sendGps);

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
        /// Main cycle. Check readiness every service, send request (force / не force), apply the resulting localization if success
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
                Debug.LogError("Camera is not available");
                yield break;
            }

            if (sendOnlyFeatures)
            {
                MobileVPS mobileVPS = provider.GetMobileVPS();
                camera.Init(mobileVPS.imageFeatureExtractorRequirements, mobileVPS.imageEncoderRequirements);
            }

            var tracking = provider.GetTracking();
            if (tracking == null)
            {
                OnErrorHappend?.Invoke(ErrorCode.TRACKING_NOT_AVALIABLE);
                Debug.LogError("Tracking is not available");
                yield break;
            }


            // TODO: убрать ссылку в этом скрипте на ARFoundationApplyer и из Provider
            // Все операции по вычислению скоректированной новой позиции можно
            // сделать внутри этого класса. ARFoundationApplyer должен подписаться
            // на событие начала локализации (новое) и конца локализации
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
                    yield return imagesCollector.StartCollectPhoto(provider, sendOnlyFeatures);

                    Debug.Log("Sending VPS Localization Request...");
                    requestVPS.SetUrl(settings.Url);

                    yield return requestVPS.SendVpsLocalizationRequest(imagesCollector.GetLocalizationData());
                    Debug.Log("VPS Localization answer recieved!");

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
                        Debug.LogErrorFormat("VPS Request Error: {0}", requestVPS.GetErrorCode());
                    }

                    yield return new WaitForSeconds(settings.Timeout - neuronTime);
                    neuronTime = 0;
                    continue;
                }

                // remember current pose
                arRFoundationApplyer?.LocalisationStart();

                Meta = DataCollector.CollectData(provider, !isCalibration, sendOnlyFeatures);

                requestVPS.SetUrl(settings.Url);

                // if send features - send them
                if (sendOnlyFeatures)
                {
                    yield return new WaitUntil(() => ARFoundationCamera.semaphore.CheckState());
                    ARFoundationCamera.semaphore.TakeOne();

                    NativeArray<byte> featureExtractorInput = camera.GetImageEncoderBuffer();
                    if (featureExtractorInput == null || featureExtractorInput.Length == 0)
                    {
                        Debug.LogError("Cannot take camera image as ByteArray for FeatureExtractor");
                        yield return null;
                        continue;
                    }

                    NativeArray<byte> encoderInput = camera.GetImageEncoderBuffer();
                    if (encoderInput == null || encoderInput.Length == 0)
                    {
                        Debug.LogError("Cannot take camera image as ByteArray for Encoder");
                        yield return null;
                        continue;
                    }
                    MobileVPS mobileVPS = provider.GetMobileVPS();
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
                    while (!imageFeatureExtractorTask.IsCompleted || !imageEncoderTask.IsCompleted)
                        yield return null;

                    ARFoundationCamera.semaphore.Free();
                    Embedding = EMBDCollector.ConvertToEMBD(1, 2, imageFeatureExtractorTask.Result.keyPoints, imageFeatureExtractorTask.Result.scores, imageFeatureExtractorTask.Result.descriptors, imageEncoderTask.Result.globalDescriptor);
                    Debug.Log("Sending VPS Request...");
                    yield return requestVPS.SendVpsRequest(Embedding, Meta);
                }
                // if not - send only photo and meta
                else
                {
                    Image = camera.GetFrame();

                    if (Image == null)
                    {
                        Debug.LogError("Image from camera is not available");
                        yield return null;
                        continue;
                    }

                    Debug.Log("Sending VPS Request...");
                    yield return requestVPS.SendVpsRequest(Image, Meta);
                }

                Debug.Log("VPS answer recieved!");

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
                    Debug.LogErrorFormat("VPS Request Error: {0}", requestVPS.GetErrorCode());
                }

                yield return new WaitForSeconds(settings.Timeout);
            }
        }
    }
}