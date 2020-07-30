using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public interface IServiceGPS
    {
        GPSData GetGPSData();
        CompassData GetCompassData();
    }
}