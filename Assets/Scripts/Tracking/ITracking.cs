using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public interface ITracking
    {
        /// <summary>
        /// Set default building id from user
        /// </summary>
        void SetDefaultBuilding(string defaultBuilding);
        /// <summary>
        /// Get current tracking data;
        /// updates only on request
        /// </summary>
        /// <returns>The local tracking.</returns>
        TrackingData GetLocalTracking();
        /// <summary>
        /// Set building id from server
        /// </summary>
        void SetGuidPointcloud(string guid);
        /// <summary>
        /// Reset current tracking
        /// </summary>
        void ResetTracking();
    }
}
