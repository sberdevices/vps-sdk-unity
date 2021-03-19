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

        public VPSBuilding Buiding;

        private void Start()
        {
            if (Buiding == VPSBuilding.Bootcamp)
            {
                DefaultGuidPointcloud = "eeb38592-4a3c-4d4b-b4c6-38fd68331521";
            }
            else if (Buiding == VPSBuilding.Polytech)
            {
                DefaultGuidPointcloud = "Polytech";
            }
            trackingData = new TrackingData
            {
                GuidPointcloud = DefaultGuidPointcloud
            };

            ARCamera = FindObjectOfType<ARSessionOrigin>().camera.gameObject;
            if (ARCamera == null)
            {
                Debug.LogError("Camera is not available for tracking");
            }
        }

        private void UpdateTrackingData()
        {
            if (ARCamera != null)
            {
                trackingData.Position = ARCamera.transform.position;
                trackingData.Rotation = ARCamera.transform.rotation;
            }
        }

        /// <summary>
        /// Tracking updates only on request
        /// </summary>
        /// <returns>The local tracking.</returns>
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
    }
}
