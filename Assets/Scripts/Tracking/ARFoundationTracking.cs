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
            trackingData = new TrackingData();

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
