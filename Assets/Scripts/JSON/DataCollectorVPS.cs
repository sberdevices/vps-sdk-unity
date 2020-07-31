using System.Collections;
using System.Collections.Generic;
using ARVRLab.VPSService;
using UnityEngine;

namespace ARVRLab.ARVRLab.VPSService.JSONs
{
    /// <summary>
    /// Упавковка и распаковка JSON 
    /// </summary>
    public static class DataCollector
    {
        public static string CollectData(ServiceProvider Provider, bool forceVPS = false)
        {
            Pose pose = new Pose();
            string loc_id = "";

            if (forceVPS)
            {
                pose = Pose.identity;
            }
            else
            {
                TrackingData tracking = Provider.GetTracking().GetLocalTracking();
                pose.position = tracking.Position;
                pose.rotation = tracking.Rotation;
            }

            // сейчас сервер обязательно должен получить loc_id от клиента
            loc_id = "eeb38592-4a3c-4d4b-b4c6-38fd68331521";
            //loc_id = tracking.GuidPointcloud; //клиент не знает loc_id, так как сервер не выдает

            string stat = "-";
            float prog = 0f;

            IServiceGPS gps = Provider.GetGPS();

            GPSData gpsData = gps.GetGPSData();

            double lat = gpsData.Latitude;
            double lon = gpsData.Longitude;
            double alt = gpsData.Altitude;
            float accuracy = gpsData.Accuracy;
            double locationTimeStamp = gpsData.Timestamp;

            CompassData gpsCompass = gps.GetCompassData();

            float heading = gpsCompass.Heading;
            float headingAccuracy = gpsCompass.Accuracy;
            double compassTimeStamp = gpsCompass.Timestamp;

            var attrib = new Attributes
            {
                //id = System.Guid.NewGuid().ToString(),

                status = stat,
                progress = prog,

                location = new Location()
                {
                    location_id = loc_id,
                    gps = new Gps
                    {
                        latitude = lat,
                        longitude = lon,
                        altitude = alt,
                        accuracy = accuracy,
                        timestamp = locationTimeStamp
                    },

                    compass = new ARVRLab.VPSService.JSONs.Compass
                    {
                        heading = heading,
                        accuracy = headingAccuracy,
                        timestamp = compassTimeStamp
                    },

                    localPos = new LocalPos
                    {
                        x = pose.position.x,
                        y = pose.position.y,
                        z = pose.position.z,
                        roll = pose.rotation.eulerAngles.x,
                        pitch = pose.rotation.eulerAngles.y,
                        yaw = pose.rotation.eulerAngles.z

                    }
                },

                forced_localization = forceVPS


            };


            var data = new Data
            {
                id = System.Guid.NewGuid().ToString(),

                type = "job",

                attributes = attrib
            };

            var communicationStruct = new CommunicationStruct
            {
                data = data
            };

            var json = JsonUtility.ToJson(communicationStruct);

            return json;
            //return job;
        }

        public static LocationState Deserialize(string json)
        {
            CommunicationStruct communicationStruct = JsonUtility.FromJson<CommunicationStruct>(json);

            LocalisationResult localisation = new LocalisationResult
            {
                LocalPosition = new Vector3(communicationStruct.data.attributes.location.relative.x, communicationStruct.data.attributes.location.relative.y,
                communicationStruct.data.attributes.location.relative.z),
                LocalRotationY = communicationStruct.data.attributes.location.relative.pitch,
                //GpsLatitude = communicationStruct.data.attributes.location.gps.latitude,
                //GpsLongitude = communicationStruct.data.attributes.location.gps.longitude,
                //GuidPointcloud = communicationStruct.data.attributes.location.location_id,
                //Heading = communicationStruct.data.attributes.location.compass.heading,
                //Accuracy = communicationStruct.data.attributes.location.gps.accuracy,
                //Timestamp = communicationStruct.data.attributes.location.gps.timestamp
            };

            LocationState request = new LocationState
            {
                Status = GetStatusFromString(communicationStruct.data.attributes.status),
                Localisation = localisation
            };

            if (request.Status == LocalisationStatus.VPS_READY)
                request.Error = ErrorCode.NO_ERROR;
            else if (request.Status == LocalisationStatus.GPS_ONLY)
                request.Error = ErrorCode.LOCALISATION_FAIL;

            return request;
        }

        static LocalisationStatus GetStatusFromString(string status)
        {
            switch(status)
            {
                case "progress": return LocalisationStatus.NO_LOCALISATION;
                case "done": return LocalisationStatus.VPS_READY;
                case "fail": return LocalisationStatus.GPS_ONLY;
                default: return LocalisationStatus.GPS_ONLY;
            }
        }
    }
}