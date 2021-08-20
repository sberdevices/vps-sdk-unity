using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace ARVRLab.VPSService
{
    public class SettingsToggles : MonoBehaviour
    {
        public VPSLocalisationService VPS;
        public ARCameraManager cameraManager;

        public Toggle UsePhotoSerias;
        public Toggle Autofocus;
        public Toggle SendOnlyFeatures;
        public Toggle AlwaysForce;
        public Toggle SendGPS;
        public Toggle Occluder;

        public Button RestartVPSButton;
        public float PressTime = 2f;
        private float mouseDeltaTime = 0;
        public string ContentTag;

        public Material occluderMaterial;
        public Material standartMaterial;

        private void Awake()
        {
            UsePhotoSerias?.onValueChanged.AddListener((value) => VPS.UsePhotoSeries = value);
            Autofocus?.onValueChanged.AddListener((value) => cameraManager.autoFocusRequested = value);
            SendOnlyFeatures?.onValueChanged.AddListener((value) => VPS.SendOnlyFeatures = value);
            AlwaysForce?.onValueChanged.AddListener((value) => VPS.AlwaysForce = value);
            SendGPS?.onValueChanged.AddListener((value) => VPS.SendGPS = value);
            Occluder?.onValueChanged.AddListener((value) => ApplyOccluder(value));

            RestartVPSButton.onClick.AddListener(() =>
            {
                VPS.ResetTracking();
                SettingsVPS settings = new SettingsVPS(VPS.defaultUrl, VPS.defaultBuildingGuid);
                VPS.StartVPS(settings);
                HideToggles();
            });

            HideToggles();
        }

        private void Start()
        {
            if (UsePhotoSerias != null)
                UsePhotoSerias.isOn = VPS.UsePhotoSeries;
            if (Autofocus != null)
                Autofocus.isOn = cameraManager.autoFocusRequested;
            if (SendOnlyFeatures != null)
                SendOnlyFeatures.isOn = VPS.SendOnlyFeatures;
            if (AlwaysForce != null)
                AlwaysForce.isOn = VPS.AlwaysForce;
            if (SendGPS != null)
                SendGPS.isOn = VPS.SendGPS;
            if (Occluder != null)
                Occluder.isOn = false;
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetMouseButton(0))
            {
                mouseDeltaTime += Time.deltaTime;
                if (mouseDeltaTime >= PressTime)
                {
                    ShowToggles();
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                mouseDeltaTime = 0f;
            }
#else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    mouseDeltaTime += Time.deltaTime;
                    if (mouseDeltaTime >= PressTime)
                    {
                        ShowToggles();
                    }
                }
                else
                {
                    mouseDeltaTime = 0f;
                }
            }
#endif
        }

        private void ShowToggles()
        {
            UsePhotoSerias?.gameObject.SetActive(true);
            Autofocus?.gameObject.SetActive(true);
            SendOnlyFeatures?.gameObject.SetActive(true);
            AlwaysForce?.gameObject.SetActive(true);
            SendGPS?.gameObject.SetActive(true);
            RestartVPSButton?.gameObject.SetActive(true);
            Occluder?.gameObject.SetActive(true);
        }

        private void HideToggles()
        {
            UsePhotoSerias?.gameObject.SetActive(false);
            Autofocus?.gameObject.SetActive(false);
            SendOnlyFeatures?.gameObject.SetActive(false);
            AlwaysForce?.gameObject.SetActive(false);
            SendGPS?.gameObject.SetActive(false);
            RestartVPSButton?.gameObject.SetActive(false);
            Occluder?.gameObject.SetActive(false);
        }

        private void ApplyOccluder(bool enable)
        {
            Material material = enable ? occluderMaterial : standartMaterial;

            GameObject[] contents = GameObject.FindGameObjectsWithTag(ContentTag);
            foreach (var content in contents)
            {
                content.GetComponent<Renderer>().material = material;
            }
        }
    }
}
