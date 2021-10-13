using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARVRLab.VPSService
{
    public class ARFoundationTracking : MonoBehaviour, ITracking
    {
        private string DefaultGuidPointcloud = "";

        private GameObject ARCamera;
        private TrackingData trackingData;

        private void Start()
        {
            ARCamera = FindObjectOfType<ARSessionOrigin>().camera.gameObject;
            if (ARCamera == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "Camera is not available for tracking");
            }
            if (trackingData == null)
            {
                trackingData = new TrackingData();
            }
        }

        /// <summary>
        /// Write current position and rotation from camera in the structure
        /// </summary>
        private void UpdateTrackingData()
        {
            if (ARCamera != null)
            {
                trackingData.Position = ARCamera.transform.position;
                trackingData.Rotation = ARCamera.transform.rotation;
            }
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
