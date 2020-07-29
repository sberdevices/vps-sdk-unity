﻿using System.Collections;
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
        }

        //public IGPS GetGPS()
        //{
        //    return GPS;
        //}

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
