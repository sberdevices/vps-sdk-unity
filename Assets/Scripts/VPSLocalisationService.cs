using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class VPSLocalisationService : MonoBehaviour
    {
        private bool startOnAwake;
        private bool isMock;

        private ServiceProvider provider;
        private VPSLocalisationAlgorithm algorithm;

        private void Start()
        {
            provider = GetComponent<ServiceProvider>();
            StartVPS();
        }

        public void StartVPS()
        {
            algorithm = new VPSLocalisationAlgorithm(this, provider);
        }

        public void StartVPS(SettingsVPS settings)
        {
            algorithm = new VPSLocalisationAlgorithm(this, provider, settings);
            StartCoroutine(algorithm.LocalisationRoutine());
        }

        public void StopVps()
        {
            algorithm?.Stop();
        }

        public LocationState GetLatestPose()
        {
            return algorithm.GetLocationRequest();
        }

        public void SetMockLocation(LocalisationResult mockLocation)
        {

        }

        public void SetMockMode(bool isMock)
        {

        }
    }
}