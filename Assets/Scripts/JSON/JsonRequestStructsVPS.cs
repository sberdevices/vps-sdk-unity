using System;
using UnityEngine;

namespace ARVRLab.ARVRLab.VPSService.JSONs
{
	[Serializable]
	public class RequestStruct
	{
		public RequestData data;
	}

	[Serializable]
	public class RequestData
	{
		public string id;
		public string type;
		public RequestAttributes attributes;
	}

	[Serializable]
	public class RequestAttributes
	{
        public RequestLocation location;

        public ImageTransform imageTransform;

        public Intrinsics intrinsics;

        public bool forced_localization;
	}

    [Serializable]
	public class RequestLocation
	{
		public string type;
		public string location_id;
		public RequstGps gps;
		public RequestCompass compass;
        public string clientCoordinateSystem;
        public LocalPos localPos;
	}

	[Serializable]
	public class RequstGps
	{
		public double latitude;

		public double longitude;

		public double altitude;

		public float accuracy;

		public double timestamp;
	}

    [Serializable]
    public class LocalPos
    {
        public float x;
        public float y;
        public float z;

        public float roll;
        public float pitch;
        public float yaw;
    }

    [Serializable]
	public class RequestCompass
	{
		public float heading;

		public float accuracy;

		public double timestamp;
	}

    [Serializable]
    public class ImageTransform
    {
        public int orientation;

        public bool mirrorX;

        public bool mirrorY;
    }

    [Serializable]
    public class Intrinsics
    {
        public float fx;

        public float fy;

        public float cx;

        public float cy;
    }

    public class RequestLocalizationData
    {
        public Texture2D image;
        public string meta;
        public Pose pose;

        public RequestLocalizationData(Texture2D Img, string Meta, Pose pose)
        {
            image = Img;
            meta = Meta;
            this.pose = pose;
        }
    }
}
