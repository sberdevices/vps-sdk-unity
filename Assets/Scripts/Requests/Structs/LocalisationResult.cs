using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class LocalisationResult
    {
        public Vector3 TrackingPosition;
        public Vector3 TrackingRotation;

        public Vector3 VpsPosition;
        public Vector3 VpsRotation;

        public double GpsLatitude;
        public double GpsLongitude;
        public float Heading;
        public float Accuracy;
        public double Timestamp;

        public LocalisationResult()
        {
            TrackingPosition = Vector3.zero;
            TrackingRotation = Vector3.zero;
            VpsPosition = Vector3.zero;
            VpsRotation = Vector3.zero;
            GpsLatitude = 0;
            GpsLongitude = 0;
            Heading = 0;
            Accuracy = 0;
            Timestamp = 0;
        }
    }
}