using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    /// <summary>
    /// API сервиса VPS
    /// </summary>
    public class VPSLocalisationService : MonoBehaviour
    {
        [Tooltip("Запускать VPS сразу?")]
        public bool StartOnAwake;

        [Tooltip("Какие брать данные с камеры, GPS...")]
        public ServiceProvider provider;

        [Tooltip("Использовать пайплайн с локализацией по серии снимков?")]
        public bool UsePhotoSerias;

        [Header("Default VPS Settings")]
        public VPSBuilding defaultBuilding;
        public ServerType defaultServerType;

        [Header("Custom URL")]
        [Tooltip("Использовать кастомную ссылку?")]
        public bool UseCustomUrl;
        public string CustomUrl = "";

        private SettingsVPS defaultSettings;
        private VPSPrepareStatus vpsPreparing;

        /// <summary>
        /// Событие ошибки локализации
        /// </summary>
        public event System.Action<ErrorCode> OnErrorHappend;

        /// <summary>
        /// Событие успешной локализации
        /// </summary>
        public event System.Action<LocationState> OnPositionUpdated;

        private bool isMock;
        private LocationState mockLocation;

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
                StartCoroutine(vpsPreparing.DownloadNeural());
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
        /// Запускает сервис VPS c настройками по умолчанию
        /// </summary>
        public void StartVPS()
        {
            StartVPS(defaultSettings);
        }

        /// <summary>
        /// Запускает сервис VPS c заданными настройками
        /// </summary>
        public void StartVPS(SettingsVPS settings)
        {
            if (!IsReady())
            {
                Debug.LogError("MobileVPS is not ready");
                return;
            }

            StopVps();

            algorithm = new VPSLocalisationAlgorithm(this, provider, settings, UsePhotoSerias);

            algorithm.OnErrorHappend += (e) => OnErrorHappend?.Invoke(e);
            algorithm.OnLocalisationHappend += (ls) => OnPositionUpdated?.Invoke(ls);
        }

        /// <summary>
        /// Останавливает работу сервиса VPS
        /// </summary>
        public void StopVps()
        {
            algorithm?.Stop();
        }

        /// <summary>
        /// Выдает результат последней локализации
        /// </summary>
        public LocationState GetLatestPose()
        {
            if (isMock)
                return mockLocation;

            // null ref если алгоритм не запущен - выводим ошибку и возвращаем null
            if (algorithm == null)
            {
                Debug.LogError("VPS service is not running. Use StartVPS before");
                return null;
            }
            return algorithm.GetLocationRequest();
        }

        /// <summary>
        /// Задает тестовый результат запроса локализации
        /// </summary>
        public void SetMockLocation(LocalisationResult mock_location)
        {
            mockLocation = new LocationState();

            mockLocation.Localisation = mock_location;
            mockLocation.Status = LocalisationStatus.VPS_READY;
            mockLocation.Error = ErrorCode.NO_ERROR;
        }

        /// <summary>
        /// Включает/выключает тестовый режим 
        /// </summary>
        public void SetMockMode(bool is_mock)
        {
            isMock = is_mock;
        }

        /// <summary>
        /// Была ли локализация?
        /// </summary>
        /// <returns><c>true</c>, if localized was ised, <c>false</c> otherwise.</returns>
        public bool IsLocalized()
        {
            return provider.GetTracking().GetLocalTracking().IsLocalisedFloor;
        }

        /// <summary>
        /// Возвращает прогресс скачивания от 0 до 1
        /// </summary>
        public float GetPreparingProgress()
        {
            return vpsPreparing.GetProgress();
        }

        /// <summary>
        /// Проверяет, скачана ли нейронка
        /// </summary>
        public bool IsReady()
        {
            return vpsPreparing.IsReady();
        }
    }
}