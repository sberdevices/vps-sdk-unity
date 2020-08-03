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
        /// Тут тоже комментарий
        /// </summary>
        public event System.Action<ErrorCode> OnErrorHappend;

        /// <summary>
        /// И тут комментарий
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
        /// Расписать комментарии к каждой функции
        /// </summary>
        public void StartVPS()
        {
            algorithm = new VPSLocalisationAlgorithm(this, provider);
        }

        /// <summary>
        /// Расписать комментарии к каждой функции
        /// </summary>
        public void StartVPS(SettingsVPS settings)
        {
            algorithm = new VPSLocalisationAlgorithm(this, provider, settings);
        }

        /// <summary>
        /// Расписать комментарии к каждой функции
        /// </summary>
        public void StopVps()
        {
            algorithm?.Stop();
        }

        /// <summary>
        /// Расписать комментарии к каждой функции
        /// </summary>
        public LocationState GetLatestPose()
        {
            if (isMock)
                return mockLocation;

            // null ref если алгоритм не запущен - нужно вывести ошибку
            return algorithm.GetLocationRequest();
        }

        /// <summary>
        /// Расписать комментарии к каждой функции
        /// </summary>
        public void SetMockLocation(LocalisationResult mock_location)
        {
            mockLocation = new LocationState();

            mockLocation.Localisation = mock_location;
            mockLocation.Status = LocalisationStatus.VPS_READY;
            mockLocation.Error = ErrorCode.NO_ERROR;
        }

        /// <summary>
        /// Расписать комментарии к каждой функции
        /// </summary>
        public void SetMockMode(bool is_mock)
        {
            isMock = is_mock;
        }
    }
}