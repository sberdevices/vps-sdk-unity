using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public interface ITracking
    {
        void SetDefaultBuilding(string defaultBuilding);
        TrackingData GetLocalTracking();
        void SetGuidPointcloud(string guid);
        void ResetTracking();
    }
}
