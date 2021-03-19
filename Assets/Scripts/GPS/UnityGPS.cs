using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    /// <summary>
    /// Getting GPS coordinates and compass
    /// </summary>
    public class UnityGPS : MonoBehaviour, IServiceGPS
    {
        private GPSData gpsData;
        private CompassData compassData;

        // max initialization time
        private int maxWait = 20;
        // update gps timeout
        private const float timeToUpdate = 3;

        private new bool enabled = true;

        private void Start()
        {
            if (Application.isEditor)
                return;

            gpsData = new GPSData();
            compassData = new CompassData();
        }

        private IEnumerator StartGPS()
        {
            // check gps available
            if (!Input.location.isEnabledByUser)
            {
                gpsData.status = GPSStatus.Failed;
                Debug.LogError("GPS is not available");
                yield break;
            }

            // start gps 
            Input.location.Start(0f, 0f);
            Input.compass.enabled = true;

            // initialization
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // timeout exit
            if (maxWait < 1)
            {
                gpsData.status = GPSStatus.Failed;
                Debug.LogError("GPS is timed out");
                yield break;
            }

            // check connection
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.LogError("GPS: Unable to determine device location");
                yield break;
            }
            else
            {
                while (Input.location.status == LocationServiceStatus.Running)
                {
                    gpsData.status = GPSStatus.Running;

                    gpsData.Latitude = Input.location.lastData.latitude;
                    gpsData.Longitude = Input.location.lastData.longitude;
                    gpsData.Altitude = Input.location.lastData.altitude;
                    gpsData.Accuracy = Input.location.lastData.horizontalAccuracy;
                    gpsData.Timestamp = Input.location.lastData.timestamp;

                    compassData.status = GPSStatus.Running;

                    compassData.Heading = Input.compass.trueHeading;
                    compassData.Accuracy = Input.compass.headingAccuracy;
                    compassData.Timestamp = Input.compass.timestamp;

                    yield return new WaitForSeconds(timeToUpdate);
                }
            }
        }

        void StopGPS()
        {
            StopAllCoroutines();
            Input.location.Stop();
            Input.compass.enabled = false;
        }

        public CompassData GetCompassData()
        {
            return compassData;
        }

        public GPSData GetGPSData()
        {
            return gpsData;
        }

        public void SetEnable(bool enable)
        {
            enabled = enable;
            if (enabled)
            {
                // for android ask permission here
#if UNITY_ANDROID
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);
#endif
                StartCoroutine(StartGPS());
            }
            else
            {
                StopGPS();
            }
        }
    }
}
