using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public enum GPSStatus { Initializing, Running, Failed };

    public class GPSData
    {
        public GPSStatus status = GPSStatus.Initializing;

        public double Latitude;
        public double Longitude;
        public double Altitude;
        public float Accuracy;
        public double Timestamp;
    }

    public class CompassData
    {
        public GPSStatus status = GPSStatus.Initializing;

        public float Accuracy;
        public float Heading;
        public double Timestamp;
    }
}