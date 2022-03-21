using System;
using Newtonsoft.Json;

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
        [JsonProperty("client_coordinate_system")]
        public string clientCoordinateSystem;
        [JsonProperty("tracking_pose")]
        public TrackingPose trackingPose;
        [JsonProperty("vps_pose")]
        public TrackingPose vpsPose;
    }

    [Serializable]
    public class ResponseLocation
    {
        public RequstGps gps;
        public RequestCompass compass;
    }
}
