﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class ServiceProvider : MonoBehaviour
    {
        [Tooltip("Камера, с которой будет браться изображение")]
        [SerializeField]
        private Camera cameraObject;

        [Tooltip("Для применения полученной локализации")]
        [SerializeField]
        private ARFoundationApplyer arFoundationApplyer;

        private ICamera camera;
        private IServiceGPS gps;
        private ITracking tracking;

        public ICamera GetCamera()
        {
            return camera;
        }

        private void Awake()
        {
            camera = cameraObject.GetComponent<ICamera>();
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
