using System;
using System.Collections;
using System.Collections.Generic;
using ARVRLab.VPSService;
using UnityEngine;
using Newtonsoft.Json;

namespace ARVRLab.ARVRLab.VPSService.JSONs
{
    /// <summary>
    /// Serialization and deserealisation JSON 
    /// </summary>
    public static class DataCollector
    {
        /// <summary>
        /// Create request structure from providers data
        /// </summary>
        /// <returns>The data.</returns>
        /// <param name="Provider">Provider.</param>
        /// <param name="forceVPS">If set to <c>true</c> force vps.</param>
        public static RequestStruct CollectData(ServiceProvider Provider, bool forceVPS = false, bool sendOnlyFeatures = false)
        {
            Pose pose = new Pose();
            var tracking = Provider.GetTracking().GetLocalTracking();
            var loc_id = tracking.GuidPointcloud;

            pose.position = tracking.Position;
            pose.rotation = tracking.Rotation;

            string relative_type = "relative";

            RequstGps requstGps = null;
            RequestCompass requestCompass = null;
            IServiceGPS gps = Provider.GetGPS();
            if (gps != null)
            {
                GPSData gpsData = gps.GetGPSData();
                requstGps = new RequstGps
                {
                    latitude = gpsData.Latitude,
                    longitude = gpsData.Longitude,
                    altitude = gpsData.Altitude,
                    accuracy = gpsData.Accuracy,
                    timestamp = gpsData.Timestamp
                };
                CompassData gpsCompass = gps.GetCompassData();
                requestCompass = new RequestCompass
                {
                    heading = gpsCompass.Heading,
                    accuracy = gpsCompass.Accuracy,
                    timestamp = gpsCompass.Timestamp
                };
            }

            Vector2 FocalPixelLength = Provider.GetCamera().GetFocalPixelLength();
            Vector2 PrincipalPoint = Provider.GetCamera().GetPrincipalPoint();

            const string userId = "user_id";
            if (!PlayerPrefs.HasKey(userId))
            {
                PlayerPrefs.SetString(userId, System.Guid.NewGuid().ToString());
            }

            int orient = sendOnlyFeatures ? 0 : (int)Provider.GetCamera().GetOrientation();

            var attrib = new RequestAttributes
            {
                location = new RequestLocation()
                {
                    type = relative_type,
                    location_id = loc_id,

                    gps = requstGps,
                    compass = requestCompass,

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
                    orientation = orient,
                    mirrorX = false,
                    mirrorY = false
                },

                intrinsics = new Intrinsics
                {
                    fx = FocalPixelLength.x,
                    fy = FocalPixelLength.y,
                    cx = PrincipalPoint.x,
                    cy = PrincipalPoint.y
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

                attributes = attrib,
                session_id = Provider.GetSessionId()
            };

            var communicationStruct = new RequestStruct
            {
                data = data
            };

            return communicationStruct;
        }

        /// <summary>
        /// Serialize request to json
        /// </summary>
        public static string Serialize(RequestStruct meta)
        {
            var json = JsonConvert.SerializeObject(meta);

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
            ResponseStruct communicationStruct = JsonConvert.DeserializeObject<ResponseStruct>(json);

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
                GuidPointcloud = communicationStruct.data.attributes.location.location_id,
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