using System;

namespace ARVRLab.ARVRLab.VPSService.JSONs
{
    [Serializable]
    public class ResponseStruct
    {
        public ResponseData data;
    }

    [Serializable]
    public class ResponseData
    {
        public string id;
        public ResponseAttributes attributes;
    }

    [Serializable]
    public class ResponseAttributes
    {
        public string status;
        public ResponseLocation location;
        public string clientCoordinateSystem;
        public TrackingPose trackingPose;
        public TrackingPose vpsPose;
    }

    [Serializable]
    public class ResponseLocation
    {
        public RequstGps gps;
        public RequestCompass compass;
    }
}
