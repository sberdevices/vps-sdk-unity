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
        public string type;
        public int Img_id;
        public ResponseAttributes attributes;
    }

    [Serializable]
    public class ResponseAttributes
    {
        public string status;

        public ResponseLocation location;
    }

    [Serializable]
    public class ResponseLocation
    {
        public string type;
        public string location_id;
        public string clientCoordinateSystem;
        public ResponseGps gps;
        public ResponseCompass compass;
        public Relative relative;
    }

    [Serializable]
    public class ResponseGps
    {
        public double latitude;

        public double longitude;

        public double altitude;
    }

    [Serializable]
    public class ResponseCompass
    {
        public float heading;
    }

    [Serializable]
    public class Relative
    {
        public float x;
        public float y;
        public float z;

        public float roll;
        public float pitch;
        public float yaw;
    }
}
