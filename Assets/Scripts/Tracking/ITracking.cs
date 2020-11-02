using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public interface ITracking
    {
        TrackingData GetLocalTracking();
        void SetGuidPointcloud(string guid);
    }

    public enum VPSBuilding { Bootcamp, Polytech };
}
