using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public interface ITracking
    {
        /// <summary>
        /// Get current tracking data;
        /// updates only on request
        /// </summary>
        /// <returns>The local tracking.</returns>
        TrackingData GetLocalTracking();
        /// <summary>
        /// Set localize flag in true
        /// </summary>
        void Localize();
        /// <summary>
        /// Reset current tracking
        /// </summary>
        void ResetTracking();
    }
}
