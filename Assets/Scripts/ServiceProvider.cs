using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class ServiceProvider : MonoBehaviour
    {
        [SerializeField]
        private ARFoundationCamera Camera; //////////////////

        //[SerializeField]
        //private IGPS GPS;

        //[SerializeField]
        //private ITracking Tracking;

        public ICamera GetCamera()
        {
            return Camera;
        }

        //public IGPS GetGPS()
        //{
        //    return GPS;
        //}

        //public ITracking GetTracking()
        //{
        //    return Tracking;
        //}
    }
}
