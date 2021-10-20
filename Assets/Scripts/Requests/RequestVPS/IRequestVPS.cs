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
        IEnumerator SendVpsRequest(Texture2D image, string meta);
        /// <summary>
        /// Send requst: meta and mobileVPS result
        /// </summary>
        IEnumerator SendVpsRequest(byte[] embedding, string meta);
        /// <summary>
        /// Send requst: photo serial and meta 
        /// </summary>
        IEnumerator SendVpsLocalizationRequest(List<RequestLocalizationData> data);
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