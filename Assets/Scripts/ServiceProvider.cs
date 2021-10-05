using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    // TODO: Зарефактероить в мега-класс, с множеством настроек
    public class ServiceProvider : MonoBehaviour
    {
        [Tooltip("To apply resulting localization")]
        public ARFoundationApplyer arFoundationApplyer;

        [Tooltip("Target photo resolution")]
        public Vector2Int desiredResolution = new Vector2Int(960, 540);
        public TextureFormat format = TextureFormat.RGB24;

        [Tooltip("Number photos in seria")]
        [SerializeField]
        public int PhotosInSeria = 5;

        private new ICamera camera;
        private IServiceGPS gps;
        private ITracking tracking;

        private LocalizationImagesCollector imagesCollector;
        private MobileVPS mobileVPS;

        private VPSTextureRequirement textureRequir;

        public ICamera GetCamera()
        {
            return camera;
        }

        public VPSTextureRequirement GetTextureRequirement()
        {
            return textureRequir;
        }

        private void Awake()
        {
            camera = GetComponent<ICamera>();
            textureRequir = new VPSTextureRequirement(desiredResolution.x, desiredResolution.y, format);

            gps = GetComponent<IServiceGPS>();
            tracking = GetComponent<ITracking>();
        }

        public void Init(bool useMobileVps)
        {
            if (imagesCollector == null)
            {
                imagesCollector = new LocalizationImagesCollector(PhotosInSeria, false);
            }
            if (useMobileVps && mobileVPS == null)
            {
                mobileVPS = new MobileVPS();
            }
        }

        public IServiceGPS GetGPS()
        {
            return gps;
        }

        public ITracking GetTracking()
        {
            return tracking;
        }

        public ARFoundationApplyer GetARFoundationApplyer()
        {
            return arFoundationApplyer;
        }

        public LocalizationImagesCollector GetImageCollector()
        {
            return imagesCollector;
        }

        public MobileVPS GetMobileVPS()
        {
            return mobileVPS; 
        }
    }
}
