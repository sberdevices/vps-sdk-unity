using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    /// <summary>
    /// Фейковый GPS - выдает заданные данные
    /// </summary>
    public class FakeGPS : MonoBehaviour, IServiceGPS
    {
        private GPSData gpsData;
        private CompassData compassData;

        public float Latitude = 54.875f;
        public float Longitude = 48.6543f;

        private GPSData GenerateGPSData()
        {
            var gpsData = new GPSData();
            gpsData.status = GPSStatus.Running;
            gpsData.Latitude = Latitude;
            gpsData.Longitude = Longitude;
            gpsData.Altitude = 72.4563;
            gpsData.Accuracy = 0.5f;
            gpsData.Timestamp = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

            return gpsData;
        }

        private CompassData GenerateCompassData()
        {
            var compassData = new CompassData();
            compassData.status = GPSStatus.Running;
            compassData.Heading = 55.33f;
            compassData.Accuracy = 0.4f;
            compassData.Timestamp = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

            return compassData;
        }

        public CompassData GetCompassData()
        {
            return GenerateCompassData();
        }

        public GPSData GetGPSData()
        {
            return GenerateGPSData();
        }
    }
}