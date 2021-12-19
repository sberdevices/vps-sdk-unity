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

        [Header("Providers")]
        [Tooltip("Which camera, GPS and tracking use for runtime")]
        public ServiceProvider RuntimeProvider;
        [Tooltip("Which camera, GPS and tracking use for mock data")]
        public ServiceProvider MockProvider;
        private ServiceProvider provider;

        [Tooltip("Use mock provider when VPS service has started")]
        public bool UseMock = false;
        [Tooltip("Always use mock provider in Editor, even if UseMock is false")]
        public bool ForceMockInEditor = true;

        [Header("Default VPS Settings")]
        [Tooltip("Use photo serial pipeline")]
        public bool UsePhotoSeries;
        [Tooltip("Send features or photo")]
        public bool SendOnlyFeatures;
        [Tooltip("Always send force vps")]
        public bool AlwaysForce;
        [Tooltip("Send GPS")]
        public bool SendGPS;

        [Header("Location Settings")]
        public string defaultUrl = "https://vps.arvr.sberlabs.com/polytech-pub/";
        public string defaultBuildingGuid = "Polytech";

        [Header("Custom URL")]
        public bool UseCustomUrl;
        public string CustomUrl = "";

        [Header("Debug")]
        [Tooltip("Save images in request localy before sending them to server")]
        [SerializeField]
        private bool saveImagesLocaly;

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
            provider.InitGPS(SendGPS);

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
            DebugUtils.SaveImagesLocaly = saveImagesLocaly;

            // check what provider should VPS use
            var isMockMode = UseMock || Application.isEditor && ForceMockInEditor;
            provider = isMockMode ? MockProvider : RuntimeProvider;

            if (!provider)
            {
                VPSLogger.Log(LogLevel.ERROR, "Can't load proveder! Select {0} provider for VPS service!");
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