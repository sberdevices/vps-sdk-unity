using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARVRLab.VPSService
{
    public class SettingsToggles : MonoBehaviour
    {
        public VPSLocalisationService VPS;

        public Toggle UsePhotoSerias;
        public Toggle SendOnlyFeatures;
        public Toggle AlwaysForce;
        public Toggle SendGPS;

        public Button RestartVPSButton;

        private void Awake()
        {
            UsePhotoSerias.onValueChanged.AddListener((value) => VPS.UsePhotoSeries = value);
            SendOnlyFeatures.onValueChanged.AddListener((value) => VPS.SendOnlyFeatures = value);
            AlwaysForce.onValueChanged.AddListener((value) => VPS.AlwaysForce = value);
            SendGPS.onValueChanged.AddListener((value) => VPS.SendGPS = value);

            RestartVPSButton.onClick.AddListener(() =>
            {
                VPS.ResetTracking();
                SettingsVPS settings = new SettingsVPS(VPS.defaultUrl, VPS.defaultBuildingGuid);
                VPS.StartVPS(settings);
            });
        }

        private void Start()
        {
            UsePhotoSerias.isOn = VPS.UsePhotoSeries;
            SendOnlyFeatures.isOn = VPS.SendOnlyFeatures;
            AlwaysForce.isOn = VPS.AlwaysForce;
            SendGPS.isOn = VPS.SendGPS;
        }
    }
}
