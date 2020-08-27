using System.Collections;
using System.Collections.Generic;
using ARVRLab.ARVRLab.VPSService.JSONs;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class LocalizationImagesCollector
    {
        private List<RequestLocalizationData> localizationData = new List<RequestLocalizationData>();

        public IEnumerator AddImage(ServiceProvider provider)
        {
            Texture2D Image;
            string Meta;

            var camera = provider.GetCamera();
            if (camera == null)
            {
                //OnErrorHappend?.Invoke(ErrorCode.NO_CAMERA);
                Debug.LogError("Camera is not available");
                yield break;
            }

            var tracking = provider.GetTracking();
            if (tracking == null)
            {
                //OnErrorHappend?.Invoke(ErrorCode.TRACKING_NOT_AVALIABLE);
                Debug.LogError("Tracking is not available");
                yield break;
            }

            var arRFoundationApplyer = provider.GetARFoundationApplyer();

            yield return new WaitUntil(() => camera.IsCameraReady());

            Image = camera.GetFrame();

            if (Image == null)
            {
                Debug.LogError("Image from camera is not available");
                yield break;
            }

            Meta = DataCollector.CollectData(provider, true);

            localizationData.Add(new RequestLocalizationData(Image, Meta, provider.GetARFoundationApplyer().GetCurrentPose()));
        }

        public List<RequestLocalizationData> GetLocalizationData()
        {
            return localizationData;
        }
    }
}
