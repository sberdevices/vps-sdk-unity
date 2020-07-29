using System.Collections;
using System.Collections.Generic;
using ARVRLab.ARVRLab.VPSService.JSONs;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class VPSLocalisationAlgorithm
    {
        private VPSLocalisationService localisationService;
        private ServiceProvider provider;

        private LocationState locationState;

        private bool isLocalized = false; // сообщает трекинг, перенести туда

        private SettingsVPS settings;

        event System.Action<ErrorCode> OnErrorHappend;
        event System.Action<LocationState> OnLocalisationHappend;

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

        // исключить nullreference, ошибки парсинга, try catch
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

                if (!isLocalized)
                {
                    Meta = DataCollector.CollectData(Pose.identity, true);
                }
                else
                {
                    //arRFoundationApplyer?.LocalisationStart();

                    //Meta = DataCollector.CollectData(Provider.GetTracking(), false);
                    Meta = DataCollector.CollectData(Pose.identity, true);
                }

                yield return requestVPS.SendVpsRequest(Image, Meta);

                if (requestVPS.GetStatus() == LocalisationStatus.VPS_READY)
                {
                    arRFoundationApplyer?.ApplyVPSTransform(requestVPS.GetResponce());
                    isLocalized = true;
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