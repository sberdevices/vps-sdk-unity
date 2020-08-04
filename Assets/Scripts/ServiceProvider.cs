using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    // TODO: Зарефактероить в мега-класс, с множеством настроек
    public class ServiceProvider : MonoBehaviour
    {
        [Tooltip("Для применения полученной локализации")]
        [SerializeField]
        private ARFoundationApplyer arFoundationApplyer;

        private new ICamera camera;
        private IServiceGPS gps;
        private ITracking tracking;

        public ICamera GetCamera()
        {
            return camera;
        }

        private void Awake()
        {
            camera = GetComponent<ICamera>();
            gps = GetComponent<IServiceGPS>();
            tracking = GetComponent<ITracking>();
        }

        public IServiceGPS GetGPS()
        {
            return gps;
        }

        public ITracking GetTracking()
        {
            return tracking;
        }

        public ARFoundationApplyer GetARFoundationApplyer()
        {
            return arFoundationApplyer;
        }
    }
}
