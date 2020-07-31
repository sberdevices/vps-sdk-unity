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
        public bool StartOnAwake;

        private bool startOnAwake;
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

        public void StartVPS()
        {
            algorithm = new VPSLocalisationAlgorithm(this, provider);
        }

        public void StartVPS(SettingsVPS settings)
        {
            algorithm = new VPSLocalisationAlgorithm(this, provider, settings);
        }

        public void StopVps()
        {
            algorithm?.Stop();
        }

        public LocationState GetLatestPose()
        {
            if (isMock)
                return mockLocation;

            return algorithm.GetLocationRequest();
        }

        public void SetMockLocation(LocalisationResult mock_location)
        {
            mockLocation = new LocationState();

            mockLocation.Localisation = mock_location;
            mockLocation.Status = LocalisationStatus.VPS_READY;
            mockLocation.Error = ErrorCode.NO_ERROR;
        }

        public void SetMockMode(bool is_mock)
        {
            isMock = is_mock;
        }
    }
}