using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class UnityGPS : MonoBehaviour, IServiceGPS
    {
        private GPSData gpsData;
        private CompassData compassData;

        private void Start()
        {
#if UNITY_ANDROID
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);
#endif

            gpsData = new GPSData();
            compassData = new CompassData();
            StartCoroutine(StartGPS());
        }

        private IEnumerator StartGPS()
        {
            // First, check if user has location service enabled
            if (!Input.location.isEnabledByUser)
            {
                gpsData.status = GPSStatus.Failed;
                Debug.LogError("GPS is not available");
                yield break;
            }

            // Start service before querying location
            Input.location.Start();
            Input.compass.enabled = true;

            // Wait until service initializes
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // Service didn't initialize in 20 seconds
            if (maxWait < 1)
            {
                gpsData.status = GPSStatus.Failed;
                Debug.LogError("GPS is timed out");
                yield break;
            }

            // Connection has failed
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

                    Debug.Log("Gps location: " + gpsData.Latitude + " " + gpsData.Longitude + " " + gpsData.Altitude + " " + gpsData.Accuracy + " " + gpsData.Timestamp);
                    Debug.Log("Compass data: " + compassData.Heading + " " + compassData.Accuracy + " " + compassData.Timestamp);

                    yield return new WaitForSeconds(3);
                }
            }
        }

        void StopGPS()
        {
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
    }
}
