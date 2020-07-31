using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    /// <summary>
    /// Получение GPS координат и значение компаса
    /// </summary>
    public class UnityGPS : MonoBehaviour, IServiceGPS
    {
        private GPSData gpsData;
        private CompassData compassData;

        // максимальное время ожидания инициализации
        private int maxWait = 20;
        // период обновления данных gps
        private const float timeToUpdate = 3;

        private void Start()
        {
            // для андроида запрашиваем разрешение отдельно
#if UNITY_ANDROID
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);
#endif

            gpsData = new GPSData();
            compassData = new CompassData();
            StartCoroutine(StartGPS());
        }

        private IEnumerator StartGPS()
        {
            // Проверяем, что gps доступен
            if (!Input.location.isEnabledByUser)
            {
                gpsData.status = GPSStatus.Failed;
                Debug.LogError("GPS is not available");
                yield break;
            }

            // Запускаем 
            Input.location.Start();
            Input.compass.enabled = true;

            // Ждем инициализации
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // Если не получилось инициализировать в течении maxWait секунд - выход по таймауту
            if (maxWait < 1)
            {
                gpsData.status = GPSStatus.Failed;
                Debug.LogError("GPS is timed out");
                yield break;
            }

            // Проверка на ошибку соединения
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
