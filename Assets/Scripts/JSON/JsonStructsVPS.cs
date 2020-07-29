using System;

namespace ARVRLab.ARVRLab.VPSService.JSONs
{
	[Serializable]
	public class CommunicationStruct
	{
		public Data data;
	}

	[Serializable]
	public class Data
	{
		public string id;
		public string type;
		public Attributes attributes;
	}

	[Serializable]
	public class Attributes
	{
		public string status;

		public float progress;

		public Location location;

        public bool forced_localization;
	}

	[Serializable]
	public class Location
	{

		public string type;
		public string location_id;
		public Gps gps;
		public Compass compass;
		public Relative relative;
        public LocalPos localPos;
	}
	[Serializable]
	public class Gps
	{

		public double latitude;

		public double longitude;

		public double altitude;

		public float accuracy;

		public double timestamp;

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
	public class Compass
	{
		public float heading;

		public float accuracy;

		public double timestamp;
	}
}
