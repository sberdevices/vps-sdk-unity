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
        [Tooltip("Используй tooltip, чтобы указать комментарий для полей в инспекторе")]
        public bool StartOnAwake;

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

        private ServiceProvider provider;
        private VPSLocalisationAlgorithm algorithm;

        private void Start()
        {
            provider = GetComponent<ServiceProvider>();
            if (StartOnAwake)
                StartVPS();
        }

        /// <summary>
        /// Запускает сервис VPS c настройками по умолчанию
        /// </summary>
        public void StartVPS()
        {
            algorithm = new VPSLocalisationAlgorithm(this, provider);

            algorithm.OnErrorHappend += (e) => OnErrorHappend?.Invoke(e);
            algorithm.OnLocalisationHappend += (ls) => OnPositionUpdated?.Invoke(ls);
        }

        /// <summary>
        /// Запускает сервис VPS c заданными настройками
        /// </summary>
        public void StartVPS(SettingsVPS settings)
        {
            algorithm = new VPSLocalisationAlgorithm(this, provider, settings);

            algorithm.OnErrorHappend += (e) => OnErrorHappend?.Invoke(e);
            algorithm.OnLocalisationHappend += (ls) => OnPositionUpdated(ls);
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
    }
}