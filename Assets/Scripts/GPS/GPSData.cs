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

        public GPSData()
        {
            status = GPSStatus.Running;
            Latitude = 0.0;
            Longitude = 0.0;
            Altitude = 0.0;
            Accuracy = 0.0f;
            Timestamp = 0.0;
        }
    }

    public class CompassData
    {
        public GPSStatus status = GPSStatus.Initializing;

        public float Accuracy;
        public float Heading;
        public double Timestamp;

        public CompassData()
        {
            status = GPSStatus.Running;
            Heading = 0.0f;
            Accuracy = 0.0f;
            Timestamp = 0.0;
        }
    }
}