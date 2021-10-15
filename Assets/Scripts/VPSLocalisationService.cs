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
        [Tooltip("Start VPS in OnAwake")]
        public bool StartOnAwake;

        [Tooltip("Use runtime provider if true or mock provider if false")]
        public bool UseRuntimeProvider;
        [Tooltip("Which camera, GPS and tracking use for runtime")]
        public ServiceProvider RuntimeProvider;
        [Tooltip("Which camera, GPS and tracking use for mock data")]
        public ServiceProvider MockProvider;
        private ServiceProvider provider;

        [Tooltip("Use photo serial pipeline")]
        public bool UsePhotoSeries;
        [Tooltip("Send features or photo")]
        public bool SendOnlyFeatures;
        [Tooltip("Always send force vps")]
        public bool AlwaysForce;
        [Tooltip("Send GPS")]
        public bool SendGPS;

        [Header("Default VPS Settings")]
        public string defaultUrl = "https://vps.arvr.sberlabs.com/polytech-pub/";
        public string defaultBuildingGuid = "Polytech";

        [Header("Custom URL")]
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

        /// <summary>
        /// Event mobile vps is downloaded
        /// </summary>
        public event System.Action OnVPSReady;

        private VPSLocalisationAlgorithm algorithm;

        private IEnumerator Start()
        {
            if (!provider)
                yield break;

            if (UseCustomUrl)
            {
                defaultSettings = new SettingsVPS(CustomUrl, defaultBuildingGuid);
            }
            else
            {
                defaultSettings = new SettingsVPS(defaultUrl, defaultBuildingGuid);
            }

            if (SendOnlyFeatures)
            {
                yield return DownloadMobileVps();
            }

            if (StartOnAwake)
                StartVPS(defaultSettings);
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
                return;

            ResetTracking();
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
            StopVps();

            if (SendOnlyFeatures)
            {
                StartCoroutine(DownloadMobileVps());
                if (!IsReady())
                {
                    VPSLogger.Log(LogLevel.DEBUG, "MobileVPS is not ready. Start downloading...");
                    return;
                }
            }

            algorithm = new VPSLocalisationAlgorithm(this, provider, settings, UsePhotoSeries, SendOnlyFeatures, AlwaysForce, SendGPS);

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
                VPSLogger.Log(LogLevel.ERROR, "VPS service is not running. Use StartVPS before");
                return null;
            }
            return algorithm.GetLocationRequest();
        }

        /// <summary>
        /// Was there at least one successful localisation?
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
            return vpsPreparing?.GetProgress() == 1;
        }

        /// <summary>
        /// Reset current tracking
        /// </summary>
        public void ResetTracking()
        {
            if (!provider)
                return;

            provider.GetARFoundationApplyer()?.ResetTracking();
            provider.GetTracking().ResetTracking();
            VPSLogger.Log(LogLevel.NONE, "Tracking reseted");
        }

        private void Awake()
        {
            if (UseRuntimeProvider)
            {
                provider = RuntimeProvider;
                if (Application.isEditor)
                    VPSLogger.Log(LogLevel.NONE, "For work with VPS in Editor it is recommended to choose MockData provider");
            }
            else
            {
                provider = MockProvider;
                if (!Application.isEditor)
                    VPSLogger.Log(LogLevel.NONE, "For work with VPS on device it is recommended to choose ARFoundation provider");
            }

            if (!provider)
            {
                VPSLogger.LogFormat(LogLevel.ERROR, "Please, select {0} provider for VPS service!", UseRuntimeProvider ? "ARFoundation" : "MockData");
                return;
            }

            for (var i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(false);
            }
            provider.gameObject.SetActive(true);
            vpsPreparing = new VPSPrepareStatus();
        }

        private IEnumerator DownloadMobileVps()
        {
            vpsPreparing.OnVPSReady += () => OnVPSReady?.Invoke();
            if (!IsReady())
            {
                yield return vpsPreparing.DownloadNeurals();
            }
            provider.InitMobileVPS();
        }
    }
}