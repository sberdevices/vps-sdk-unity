using System;
using System.Collections;
using System.Collections.Generic;
using ARVRLab.VPSService;
using UnityEngine;

namespace ARVRLab.ARVRLab.VPSService.JSONs
{
    /// <summary>
    /// Serialization and deserealisation JSON 
    /// </summary>
    public static class DataCollector
    {
        private static RequestStruct CollectDataAttributes(ServiceProvider Provider, bool forceVPS = false, bool sendOnlyFeatures = false)
        {
            Pose pose = new Pose();
            var tracking = Provider.GetTracking().GetLocalTracking();
            var loc_id = tracking.GuidPointcloud;

            pose.position = tracking.Position;
            pose.rotation = tracking.Rotation;

            string relative_type = "relative";

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

            Vector2 FocalPixelLength = Provider.GetCamera().GetFocalPixelLength();
            Vector2 PrincipalPoint = Provider.GetCamera().GetPrincipalPoint();
            float resizeCoef = Provider.GetCamera().GetResizeCoefficient();

            const string userId = "user_id";
            if (!PlayerPrefs.HasKey(userId))
            {
                PlayerPrefs.SetString(userId, System.Guid.NewGuid().ToString());
            }

            var attrib = new RequestAttributes
            {
                //id = System.Guid.NewGuid().ToString(),

                location = new RequestLocation()
                {
                    type = relative_type,
                    location_id = loc_id,
                    gps = new RequstGps
                    {
                        latitude = lat,
                        longitude = lon,
                        altitude = alt,
                        accuracy = accuracy,
                        timestamp = locationTimeStamp
                    },

                    compass = new ARVRLab.VPSService.JSONs.RequestCompass
                    {
                        heading = heading,
                        accuracy = headingAccuracy,
                        timestamp = compassTimeStamp
                    },


                    clientCoordinateSystem = "unity",

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

                imageTransform = new ImageTransform
                {
                    orientation = sendOnlyFeatures ? 1 : 0,
                    mirrorX = false,
#if UNITY_EDITOR
                    mirrorY = false
#else
                    mirrorY = true // на устройстве обязательно отражать
#endif

                },

                intrinsics = new Intrinsics
                {
                    fx = sendOnlyFeatures ? FocalPixelLength.y * resizeCoef : FocalPixelLength.x * resizeCoef,
                    fy = sendOnlyFeatures ? FocalPixelLength.x * resizeCoef : FocalPixelLength.y * resizeCoef,
                    cx = sendOnlyFeatures ? PrincipalPoint.y * resizeCoef : PrincipalPoint.x * resizeCoef,
                    cy = sendOnlyFeatures ? PrincipalPoint.x * resizeCoef : PrincipalPoint.y * resizeCoef
                },

                forced_localization = forceVPS,

                version = 1,

                user_id = PlayerPrefs.GetString(userId),

                timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds() / 1000d
        };


            var data = new RequestData
            {
                id = System.Guid.NewGuid().ToString(),

                type = "job",

                attributes = attrib
            };

            var communicationStruct = new RequestStruct
            {
                data = data
            };

            return communicationStruct;
        }

        /// <summary>
        /// Serialize all data in json
        /// </summary>
        /// <returns>The data.</returns>
        /// <param name="Provider">Provider.</param>
        /// <param name="forceVPS">If set to <c>true</c> force vps.</param>
        public static string CollectData(ServiceProvider Provider, bool forceVPS = false, bool sendOnlyFeatures = false)
        {
            var communicationStruct = CollectDataAttributes(Provider, forceVPS, sendOnlyFeatures);
            var json = JsonUtility.ToJson(communicationStruct);

            VPSLogger.LogFormat(LogLevel.DEBUG, "Json to send: {0}", json);
            return json;
        }

        /// <summary>
        /// Deserialize server responce
        /// </summary>
        /// <returns>The deserialize.</returns>
        /// <param name="json">Json.</param>
        public static LocationState Deserialize(string json)
        {
            ResponseStruct communicationStruct = JsonUtility.FromJson<ResponseStruct>(json);

            int id;
            bool checkImgId = int.TryParse(communicationStruct.data.id, out id);

            LocalisationResult localisation = new LocalisationResult
            {
                LocalPosition = new Vector3(communicationStruct.data.attributes.location.relative.x, communicationStruct.data.attributes.location.relative.y,
                communicationStruct.data.attributes.location.relative.z),
                LocalRotation = new Vector3(communicationStruct.data.attributes.location.relative.roll,
                                            communicationStruct.data.attributes.location.relative.pitch,
                                            communicationStruct.data.attributes.location.relative.yaw),
                Img_id = checkImgId ? id : -1,
                //GpsLatitude = communicationStruct.data.attributes.location.gps.latitude,
                //GpsLongitude = communicationStruct.data.attributes.location.gps.longitude,
                GuidPointcloud = communicationStruct.data.attributes.location.location_id,
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