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

        private new ICamera camera;
        private IServiceGPS gps;
        private ITracking tracking;

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
