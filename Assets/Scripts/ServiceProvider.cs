using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class ServiceProvider : MonoBehaviour
    {
        [SerializeField]
        private Camera cameraObject;

        [SerializeField]
        private ARFoundationApplyer arFoundationApplyer;

        private ICamera camera;
        private IServiceGPS gps;

        //[SerializeField]
        //private IGPS gps;

        //[SerializeField]
        //private ITracking tracking;

        public ICamera GetCamera()
        {
            return camera;
        }

        private void Awake()
        {
            camera = cameraObject.GetComponent<ICamera>();
            gps = GetComponent<IServiceGPS>();
        }

        public IServiceGPS GetGPS()
        {
            return gps;
        }

        //public ITracking GetTracking()
        //{
        //    return Tracking;
        //}

        public ARFoundationApplyer GetARFoundationApplyer()
        {
            return arFoundationApplyer;
        }
    }
}
