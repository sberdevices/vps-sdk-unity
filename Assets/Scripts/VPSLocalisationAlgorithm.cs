using System.Collections;
using System.Collections.Generic;
using ARVRLab.ARVRLab.VPSService.JSONs;
using UnityEngine;

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

        event System.Action<ErrorCode> OnErrorHappend;
        event System.Action<LocationState> OnLocalisationHappend;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="vps_servise">Родительский GameObject, для запуска корутин</param>
        /// <param name="vps_provider">Провайдер камеры, gps и трекинга</param>
        /// <param name="vps_settings">Настройки</param>
        public VPSLocalisationAlgorithm(VPSLocalisationService vps_servise, ServiceProvider vps_provider, SettingsVPS vps_settings = null)
        {
            localisationService = vps_servise;
            provider = vps_provider;

            if (vps_settings != null)
                settings = vps_settings;
            else
                settings = new SettingsVPS();

            localisationService.StartCoroutine(LocalisationRoutine());
        }

        public void Stop()
        {
            localisationService.StopAllCoroutines();
        }

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

            ARFoundationApplyer arRFoundationApplyer = provider.GetARFoundationApplyer();
            if (arRFoundationApplyer == null)
            {
                Debug.LogError("ARFoundationApplyer is not available");
            }

            while (true)
            {
                yield return new WaitUntil(() => camera.IsCameraReady());

                Image = camera.GetFrame();

                if (Image == null)
                {
                    Debug.LogError("Image from camera is not available");
                    yield return null;
                    continue;
                }

                Debug.Log("Sending VPS Request");

                RequestVPS requestVPS = new RequestVPS(settings.Url);

                if (!tracking.GetLocalTracking().IsLocalisedFloor)
                {
                    arRFoundationApplyer?.LocalisationStart();

                    Meta = DataCollector.CollectData(provider, true);
                }
                else
                {
                    arRFoundationApplyer?.LocalisationStart();

                    Meta = DataCollector.CollectData(provider, false);
                }

                yield return requestVPS.SendVpsRequest(Image, Meta);

                if (requestVPS.GetStatus() == LocalisationStatus.VPS_READY)
                {
                    arRFoundationApplyer?.ApplyVPSTransform(requestVPS.GetResponce());
                    // сервер не выдает GuidPointcloud
                    tracking.SetGuidPointcloud(requestVPS.GetResponce().GuidPointcloud);
                }
                else
                {
                    OnErrorHappend?.Invoke(requestVPS.GetErrorCode());
                    Debug.LogErrorFormat("VPS Request Error: {0}", requestVPS.GetErrorCode());
                }

                yield return new WaitForSeconds(settings.Timeout);
            }
        }
    }
}