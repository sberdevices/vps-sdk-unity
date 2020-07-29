using System.Collections;
using System.Collections.Generic;
using ARVRLab.VPSService;
using UnityEngine;

namespace ARVRLab.ARVRLab.VPSService.JSONs
{
    public static class DataCollector
    {
        // передать ServiceProvider в параметрах
        public static string CollectData(Pose CurrentCameraPose, bool forceVPS = false)
        {
            //CurrentCameraPose = new Pose(ARSessionOrigin.camera.transform.position, ARSessionOrigin.camera.transform.rotation)
            Pose pose = forceVPS ? Pose.identity : CurrentCameraPose;


            // Это все перекочует в ITracking
            string stat = "-";
            float prog = 0f;
            string loc_id = "eeb38592-4a3c-4d4b-b4c6-38fd68331521";

            double lat = 0.0d;
            double lon = 0.0d;
            double alt = 0.0d;
            float accuracy = 0.0f;
            double locationTimeStamp = 0.0d;

            float heading = 0.0f;
            float headingAccuracy = 0.0f;
            double compassTimeStamp = 0.0f;

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
                GpsLatitude = communicationStruct.data.attributes.location.gps.latitude,
                GpsLongitude = communicationStruct.data.attributes.location.gps.longitude,
                GuidPointcloud = communicationStruct.data.attributes.location.location_id,
                Heading = communicationStruct.data.attributes.location.compass.heading,
                Accuracy = communicationStruct.data.attributes.location.gps.accuracy,
                Timestamp = communicationStruct.data.attributes.location.gps.timestamp
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