using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class TrackingData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsLocalisedFloor;
        public string GuidPointcloud;
    }
}