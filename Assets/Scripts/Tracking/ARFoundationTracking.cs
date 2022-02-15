using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARVRLab.VPSService
{
    public class ARFoundationTracking : MonoBehaviour, ITracking
    {
        private GameObject ARCamera;
        private TrackingData trackingData;

        private void Start()
        {
            ARCamera = FindObjectOfType<ARSessionOrigin>().camera.gameObject;
            if (ARCamera == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "Camera is not available for tracking");
            }
            trackingData = new TrackingData();
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

        public void Localize()
        {
            if (trackingData != null)
            {
                trackingData.IsLocalisedFloor = true;
            }
        }

        public void ResetTracking()
        {
            if (trackingData != null)
            {
                trackingData.IsLocalisedFloor = false;
            }
        }
    }
}
