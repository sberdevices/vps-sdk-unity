using System.Collections;
using System.Collections.Generic;
using ARVRLab.ARVRLab.VPSService.JSONs;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public interface IRequestVPS
    {
        /// <summary>
        /// Set vps server url for sending requests
        /// </summary>
        void SetUrl(string url);
        /// <summary>
        /// Send requst: image and meta
        /// </summary>
        IEnumerator SendVpsRequest(Texture2D image, string meta, System.Action callback);
        /// <summary>
        /// Send requst: meta and mobileVPS result
        /// </summary>
        IEnumerator SendVpsRequest(byte[] embedding, string meta, System.Action callback);
        /// <summary>
        /// Get latest request status
        /// </summary>
        LocalisationStatus GetStatus();
        /// <summary>
        /// Get latest request error
        /// </summary>
        ErrorCode GetErrorCode();
        /// <summary>
        /// Get latest request responce
        /// </summary>
        LocalisationResult GetResponce();
    }
}