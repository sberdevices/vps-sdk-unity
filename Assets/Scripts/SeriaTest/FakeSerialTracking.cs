using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class FakeSerialTracking : MonoBehaviour, ITracking
    {
        private string DefaultGuidPointcloud = "";

        private TrackingData trackingData;

        private int Counter = 0;

        public string ImagesPath = "/Users/admin/arnavigation/Modules/VPSServise/Assets/Images";

        public TextAsset[] CustomPoses;

        private void Start()
        {
            trackingData = new TrackingData
            {
                GuidPointcloud = DefaultGuidPointcloud
            };
        }

        /// <summary>
        /// Write current position and rotation from current file in the structure
        /// </summary>
        private void UpdateTrackingData()
        {
            Pose currentPose = MetaParser.Parse(CustomPoses[Counter].text);
            trackingData.Position = currentPose.position;
            trackingData.Rotation = currentPose.rotation;
            Counter++;
            if (Counter >= CustomPoses.Length)
                Counter = 0;
        }

        public TrackingData GetLocalTracking()
        {
            UpdateTrackingData();
            return trackingData;
        }

        public void SetGuidPointcloud(string guid)
        {
            trackingData.GuidPointcloud = guid;
            trackingData.IsLocalisedFloor = true;
        }

        public void SetDefaultBuilding(string defaultBuilding)
        {
            if (trackingData == null)
            {
                trackingData = new TrackingData();
            }
            trackingData.GuidPointcloud = defaultBuilding;
            DefaultGuidPointcloud = defaultBuilding;
        }

        public void ResetTracking()
        {
            if (trackingData != null)
            {
                trackingData.GuidPointcloud = DefaultGuidPointcloud;
                trackingData.IsLocalisedFloor = false;
            }
        }
    }
}
