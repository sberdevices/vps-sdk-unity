﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    // TODO: Зарефактероить в мега-класс, с множеством настроек
    public class ServiceProvider : MonoBehaviour
    {
        [Tooltip("To apply resulting localization")]
        [SerializeField]
        private ARFoundationApplyer arFoundationApplyer;

        [Tooltip("Number photos in seria")]
        [SerializeField]
        public int PhotosInSeria = 5;

        private new ICamera camera;
        private IServiceGPS gps;
        private ITracking tracking;

        private LocalizationImagesCollector imagesCollector;
        private MobileVPS mobileVPS;

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

        public void Init()
        {
            imagesCollector = new LocalizationImagesCollector(PhotosInSeria, false);
            mobileVPS = new MobileVPS();
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

        public LocalizationImagesCollector GetImageCollector()
        {
            return imagesCollector;
        }

        public MobileVPS GetMobileVPS()
        {
            return mobileVPS; 
        }
    }
}
