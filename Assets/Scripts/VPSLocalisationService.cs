using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    /// <summary>
    /// API VPS Service
    /// </summary>
    public class VPSLocalisationService : MonoBehaviour
    {
        [Tooltip("Start VPS in OnAwake?")]
        public bool StartOnAwake;

        [Tooltip("Which camera, GPS and tracking use")]
        public ServiceProvider provider;

        [Tooltip("Use photo seria pipeline?")]
        public bool UsePhotoSerias;
        [Tooltip("Send features or photo?")]
        public bool SendOnlyFeatures;
        [Tooltip("Always send force vps?")]
        public bool AlwaysForce;
        [Tooltip("Send GPS?")]
        public bool SendGPS;

        [Header("Default VPS Settings")]
        public VPSBuilding defaultBuilding;
        public ServerType defaultServerType;

        [Header("Custom URL")]
        [Tooltip("Use custom url?")]
        public bool UseCustomUrl;
        public string CustomUrl = "";

        private SettingsVPS defaultSettings;
        private VPSPrepareStatus vpsPreparing;

        /// <summary>
        /// Event localisation error
        /// </summary>
        public event System.Action<ErrorCode> OnErrorHappend;

        /// <summary>
        /// Event localisation success
        /// </summary>
        public event System.Action<LocationState> OnPositionUpdated;

        private VPSLocalisationAlgorithm algorithm;

        private void Start()
        {
            if (!provider)
            {
                Debug.LogError("Please, select provider for VPS service!");
                return;
            }

            vpsPreparing = new VPSPrepareStatus();

            if (!IsReady())
            {
                vpsPreparing.OnVPSReady += () => provider.Init();
                StartCoroutine(vpsPreparing.DownloadNeural());
            }
            else
            {
                provider.Init();
            }

            if (UseCustomUrl)
            {
                defaultSettings = new SettingsVPS(CustomUrl);
            }
            else
            {
                defaultSettings = new SettingsVPS(defaultBuilding, defaultServerType);
            }

            if (StartOnAwake)
                StartVPS(defaultSettings);
        }

        /// <summary>
        /// Start VPS service with default settings
        /// </summary>
        public void StartVPS()
        {
            StartVPS(defaultSettings);
        }

        /// <summary>
        /// Start VPS service with settings
        /// </summary>
        public void StartVPS(SettingsVPS settings)
        {
            if (!IsReady())
            {
                Debug.LogError("MobileVPS is not ready");
                return;
            }

            StopVps();

            algorithm = new VPSLocalisationAlgorithm(this, provider, settings, UsePhotoSerias, SendOnlyFeatures, AlwaysForce, SendGPS);

            algorithm.OnErrorHappend += (e) => OnErrorHappend?.Invoke(e);
            algorithm.OnLocalisationHappend += (ls) => OnPositionUpdated?.Invoke(ls);
        }

        /// <summary>
        /// Stop VPS service
        /// </summary>
        public void StopVps()
        {
            algorithm?.Stop();
        }

        /// <summary>
        /// Get latest localisation result
        /// </summary>
        public LocationState GetLatestPose()
        {
            if (algorithm == null)
            {
                Debug.LogError("VPS service is not running. Use StartVPS before");
                return null;
            }
            return algorithm.GetLocationRequest();
        }

        /// <summary>
        /// Цas there at least one successful localisation?
        /// </summary>
        public bool IsLocalized()
        {
            return provider.GetTracking().GetLocalTracking().IsLocalisedFloor;
        }

        /// <summary>
        /// Get download mobileVPS progress (between 0 and 1)
        /// </summary>
        public float GetPreparingProgress()
        {
            return vpsPreparing.GetProgress();
        }

        /// <summary>
        /// Is mobileVPS ready?
        /// </summary>
        public bool IsReady()
        {
            return vpsPreparing != null && vpsPreparing.IsReady();
        }

        /// <summary>
        /// Reset current tracking
        /// </summary>
        public void ResetTracking()
        {
            provider.GetARFoundationApplyer()?.ResetTracking();
            provider.GetTracking().GetLocalTracking().IsLocalisedFloor = false;
        }

        private void Awake()
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
            provider.gameObject.SetActive(true);
        }
    }
}