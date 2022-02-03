using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class ServiceProvider : MonoBehaviour
    {
        [Tooltip("To apply resulting localization")]
        public ARFoundationApplyer arFoundationApplyer;

        [Tooltip("Target photo resolution")]
        public Vector2Int desiredResolution = new Vector2Int(540, 960);
        public TextureFormat format = TextureFormat.R8;

        [Tooltip("Number photos in serial")]
        [SerializeField]
        public int PhotosInSerial = 5;

        private new ICamera camera;
        private IServiceGPS gps;
        private ITracking tracking;

        private LocalizationImagesCollector imagesCollector;
        private MobileVPS mobileVPS;

        private VPSTextureRequirement textureRequir;

        private string sessionId;

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

            tracking = GetComponent<ITracking>();
            imagesCollector = new LocalizationImagesCollector(PhotosInSerial, false);
        }

        public void InitMobileVPS()
        {
            if (mobileVPS == null)
            {
                mobileVPS = new MobileVPS();
            }
        }

        public void InitGPS(bool useGPS)
        {
            gps = useGPS ? GetComponent<IServiceGPS>() : null;
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

        public string GetSessionId()
        {
            return sessionId;
        }

        public void ResetSessionId()
        {
            sessionId = System.Guid.NewGuid().ToString();
            VPSLogger.Log(LogLevel.VERBOSE, $"New session: {sessionId}");
        }
    }
}
