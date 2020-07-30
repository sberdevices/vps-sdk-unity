using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class FakeGPS : MonoBehaviour, IServiceGPS
    {
        private GPSData gpsData;
        private CompassData compassData;

        private void Start()
        {
            gpsData = new GPSData();
            gpsData.status = GPSStatus.Running;
            gpsData.Latitude = 54.875;
            gpsData.Longitude = 48.6543;
            gpsData.Altitude = 72.4563;
            gpsData.Accuracy = 0.5f;
            gpsData.Timestamp = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

            compassData = new CompassData();
            compassData.status = GPSStatus.Running;
            compassData.Heading = 55.33f;
            compassData.Accuracy = 0.4f;
            compassData.Timestamp = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public CompassData GetCompassData()
        {
            return compassData;
        }

        public GPSData GetGPSData()
        {
            return gpsData;
        }
    }
}