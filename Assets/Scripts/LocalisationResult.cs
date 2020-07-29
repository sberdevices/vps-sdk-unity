using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class LocalisationResult
    {
        public Vector3 LocalPosition;
        public float LocalRotationY;
        public double GpsLatitude;
        public double GpsLongitude;
        public string GuidPointcloud;
        public float Heading;
        public float Accuracy;
        public double Timestamp;
    }
}