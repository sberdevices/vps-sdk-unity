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
    /// Внутреннее управление VPS
    /// </summary>
    public class VPSLocalisationAlgorithm
    {
        private VPSLocalisationService localisationService;
        private ServiceProvider provider;

        private LocationState locationState;

        private SettingsVPS settings;

        private bool usingPhotoSerias;
        private bool sendOnlyFeatures;
        private bool alwaysForceVPS;

        IRequestVPS requestVPS = new HttpClientRequestVPS();

        /// <summary>
        /// Событие ошибки локализации
        /// </summary>
        public event System.Action<ErrorCode> OnErrorHappend;

        /// <summary>
        /// Событие успешной локализации
        /// </summary>
        public event System.Action<LocationState> OnLocalisationHappend;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="vps_servise">Родительский GameObject, для запуска корутин</param>
        /// <param name="vps_provider">Провайдер камеры, gps и трекинга</param>
        /// <param name="vps_settings">Настройки</param>
        public VPSLocalisationAlgorithm(VPSLocalisationService vps_servise, ServiceProvider vps_provider, SettingsVPS vps_settings, bool usePhotoSerias, bool onlyFeatures,
                                        bool alwaysForce, bool sendGps)
        {
            localisationService = vps_servise;
            provider = vps_provider;

            usingPhotoSerias = usePhotoSerias;
            sendOnlyFeatures = onlyFeatures;
            alwaysForceVPS = alwaysForce;

            provider.GetGPS().SetEnable(sendGps);

            settings = vps_settings;

            locationState = new LocationState();

            localisationService.StartCoroutine(LocalisationRoutine());
        }

        public void Stop()
        {
            provider.GetMobileVPS().StopTask();
            localisationService.StopAllCoroutines();
            ARFoundationCamera.semaphore.Free();
        }

        /// <summary>
        /// Выдает последний доступный Location state. Location state обновляется в LocalisationRoutine()
        /// </summary>
        /// <returns></returns>
        public LocationState GetLocationRequest()
        {
            return locationState;
        }

        /// <summary>
        /// Главный цикл процесса. Проверяет готовность всех сервисов, отправляет запрос (force / не force), применяет полученную локализацию в случае успеха
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

                // проверим, должен ли VPS сделать запрос в режиме локализации или в режиме докалибровки
                bool isCalibration;
                if (alwaysForceVPS)
                    isCalibration = false;
                else
                    isCalibration = tracking.GetLocalTracking().IsLocalisedFloor;

                if (!isCalibration && usingPhotoSerias)
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

                    yield return new WaitForSeconds(settings.Timeout);
                    continue;
                }

                // запомним текущию позицию
                arRFoundationApplyer?.LocalisationStart();

                Meta = DataCollector.CollectData(provider, !isCalibration, sendOnlyFeatures);

                requestVPS.SetUrl(settings.Url);

                // если отправляем фичи - получаем их
                if (sendOnlyFeatures)
                {
                    MobileVPS mobileVPS = provider.GetMobileVPS();
                    yield return new WaitUntil(() => ARFoundationCamera.semaphore.CheckState());
                    ARFoundationCamera.semaphore.TakeOne();
                    NativeArray<byte> input = camera.GetImageArray();
                    if (input == null || input.Length == 0)
                    {
                        Debug.LogError("Cannot take camera image as ByteArray");
                        yield return null;
                        continue;
                    }

                    var task = mobileVPS.GetFeaturesAsync(input);
                    while (!task.IsCompleted)
                        yield return null;

                    ARFoundationCamera.semaphore.Free();
                    Embedding = EMBDCollector.ConvertToEMBD(0, 0, task.Result.keyPoints, task.Result.scores, task.Result.descriptors, task.Result.globalDescriptor);

                    Debug.Log("Sending VPS Request...");
                    yield return requestVPS.SendVpsRequest(Embedding, Meta);
                }
                // иначе отправляем только фото и мету
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