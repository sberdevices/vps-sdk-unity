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
        public float Latitude = 54.875f;
        public float Longitude = 48.6543f;

        private new bool enabled = true;

        private GPSData GenerateGPSData()
        {
            var gpsData = new GPSData();
            if (!enabled)
            {
                return gpsData;
            }

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
            if (!enabled)
            {
                return compassData;
            }

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

        public void SetEnable(bool enable)
        {
            enabled = enable;
        }
    }
}