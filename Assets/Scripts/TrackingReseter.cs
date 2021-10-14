using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    /// <summary>
    /// Reset tracking by pressing a button
    /// </summary>
    public class TrackingReseter : MonoBehaviour
    {
        public VPSLocalisationService VPS;
        public KeyCode ResetKeyCode;

        private void Update()
        {
            if (Input.GetKeyDown(ResetKeyCode))
                VPS.ResetTracking();
        }
    }
}
