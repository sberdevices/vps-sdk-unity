using System.Collections;
using System.Collections.Generic;
using ARVRLab.ARVRLab.VPSService.JSONs;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public interface IRequestVPS
    {
        void SetUrl(string url);
        IEnumerator SendVpsRequest(Texture2D image, string meta);
        IEnumerator SendVpsRequest(byte[] embedding, string meta);
        IEnumerator SendVpsLocalizationRequest(List<RequestLocalizationData> data);
        LocalisationStatus GetStatus();
        ErrorCode GetErrorCode();
        LocalisationResult GetResponce();
    }
}