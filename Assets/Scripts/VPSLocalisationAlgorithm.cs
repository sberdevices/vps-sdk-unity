using System.Collections;
using System.Collections.Generic;
using ARVRLab.ARVRLab.VPSService.JSONs;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class VPSLocalisationAlgorithm
    {
        VPSLocalisationService LocalisationService;
        ServiceProvider Provider;

        private LocationState locationState;

        private bool IsLocalized = false;

        private SettingsVPS Settings;

        public VPSLocalisationAlgorithm(VPSLocalisationService servise, ServiceProvider provider, SettingsVPS settings = null)
        {
            LocalisationService = servise;
            Provider = provider;

            if (settings != null)
                Settings = settings;
            else
                Settings = new SettingsVPS();
        }

        public LocationState GetLocationRequest()
        {
            return locationState;
        }

        public IEnumerator LocalisationRoutine()
        {
            Texture2D Image;
            string Meta;

            while (true)
            {
                yield return new WaitUntil(() => Provider.GetCamera().IsCameraReady());

                Debug.Log("Send");

                RequestVPS requestVPS = new RequestVPS(Settings.Url);

                if (!IsLocalized)
                {
                    Image = Provider.GetCamera().GetFrame();
                    Meta = DataCollector.CollectData(Pose.identity, true);
                }
                else
                {
                    //ARFoundationApplyer.Instance.LocalisationStart();
                    Image = Provider.GetCamera().GetFrame();

                    //Meta = DataCollector.CollectData(Provider.GetTracking(), false);
                    Meta = DataCollector.CollectData(Pose.identity, true);
                }

                yield return LocalisationService.StartCoroutine(requestVPS.SendVpsRequest(Image, Meta));

                if (requestVPS.GetStatus() == LocalisationStatus.VPS_READY)
                {
                    ARFoundationApplyer.Instance.ApplyVPSTransform(requestVPS.GetResponce());
                    IsLocalized = true;
                }
                else
                {
                    Debug.LogErrorFormat("Ошибка: {0}", requestVPS.GetErrorCode());
                }

                yield return new WaitForSeconds(Settings.Timeout);
            }
        }
    }
}