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
        public static RequestStruct CollectData(ServiceProvider Provider)
        {
            Pose pose = new Pose();
            var tracking = Provider.GetTracking().GetLocalTracking();

            pose.position = tracking.Position;
            pose.rotation = tracking.Rotation;

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

            const string userIdKey = "user_id";
            if (!PlayerPrefs.HasKey(userIdKey))
            {
                PlayerPrefs.SetString(userIdKey, Guid.NewGuid().ToString());
            }

            var attrib = new RequestAttributes
            {
                sessionId = Provider.GetSessionId(),
                userId = PlayerPrefs.GetString(userIdKey),
                timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds() / 1000d,

                location = new RequestLocation()
                {
                    gps = requstGps,
                    compass = requestCompass,

                    clientCoordinateSystem = "unity",

                    trackingPose = new TrackingPose
                    {
                        x = pose.position.x,
                        y = pose.position.y,
                        z = pose.position.z,
                        rx = pose.rotation.eulerAngles.x,
                        ry = pose.rotation.eulerAngles.y,
                        rz = pose.rotation.eulerAngles.z
                    },

                    intrinsics = new Intrinsics
                    {
                        fx = FocalPixelLength.x,
                        fy = FocalPixelLength.y,
                        cx = PrincipalPoint.x,
                        cy = PrincipalPoint.y
                    }
                }
        };


            var data = new RequestData
            {
                id = Guid.NewGuid().ToString(),
                attributes = attrib
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

            LocalisationResult localisation = new LocalisationResult
            {
                LocalPosition = new Vector3(communicationStruct.data.attributes.vpsPose.x,
                                            communicationStruct.data.attributes.vpsPose.y,
                                            communicationStruct.data.attributes.vpsPose.z),
                LocalRotation = new Vector3(communicationStruct.data.attributes.vpsPose.rx,
                                            communicationStruct.data.attributes.vpsPose.ry,
                                            communicationStruct.data.attributes.vpsPose.rz)
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
                case "done": return LocalisationStatus.VPS_READY;
                case "fail": return LocalisationStatus.GPS_ONLY;
                default: return LocalisationStatus.GPS_ONLY;
            }
        }
    }
}