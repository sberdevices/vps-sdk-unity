using System.Collections;
using System.Collections.Generic;
using ARVRLab.ARVRLab.VPSService.JSONs;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public interface IRequestVPS
    {
        void SetUrl(string url);
        IEnumerator SendVpsRequest(Texture2D image, string meta, byte[] embedding = null);
        IEnumerator SendVpsLocalizationRequest(List<RequestLocalizationData> data);
        LocalisationStatus GetStatus();
        ErrorCode GetErrorCode();
        LocalisationResult GetResponce();
    }
}