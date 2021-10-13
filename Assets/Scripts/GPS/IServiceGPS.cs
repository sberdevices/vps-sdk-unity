using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public interface IServiceGPS
    {
        /// <summary>
        /// Get current gps data
        /// </summary>
        GPSData GetGPSData();
        /// <summary>
        /// Get current compass data
        /// </summary>
        CompassData GetCompassData();
        /// <summary>
        /// Start / stop gps
        /// </summary>
        void SetEnable(bool enable);
    }
}