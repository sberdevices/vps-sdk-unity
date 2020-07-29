using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class VPSLocalisationService : MonoBehaviour
    {
        private bool StartOnAwake;
        private bool IsMock;

        private ServiceProvider Provider;
        private VPSLocalisationAlgorithm algorithm;

        public void Start()
        {
            Provider = GetComponent<ServiceProvider>();
            StartVPS();
        }

        public void StartVPS()
        {
            algorithm = new VPSLocalisationAlgorithm(this, Provider);
            StartCoroutine(algorithm.LocalisationRoutine());
        }

        public void StartVPS(SettingsVPS settings)
        {
            algorithm = new VPSLocalisationAlgorithm(this, Provider, settings);
            StartCoroutine(algorithm.LocalisationRoutine());
        }

        public void StopVps()
        {
            StopAllCoroutines();
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