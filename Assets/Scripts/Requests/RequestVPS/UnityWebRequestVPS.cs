﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ARVRLab.ARVRLab.VPSService.JSONs;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Net;
using System.Net.Http;

namespace ARVRLab.VPSService
{
    /// <summary>
    /// Requst to VPS server
    /// </summary>
    public class UnityWebRequestVPS : IRequestVPS
    {
        private string serverUrl;
        // api for one photo localisation
        private string api_path_session = "vps/api/v2";

        private int timeout = 4;

        private LocationState locationState = new LocationState();

        #region Metrics

        private const string ImageVPSRequest = "ImageVPSRequest";
        private const string MVPSRequest = "MVPSRequest";

        #endregion

        public void SetUrl(string url)
        {
            serverUrl = url;
        }

        public IEnumerator SendVpsRequest(Texture2D image, string meta, System.Action callback)
        {
            string uri = Path.Combine(serverUrl, api_path_session).Replace("\\", "/");

            if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            {
                VPSLogger.LogFormat(LogLevel.ERROR, "URL is incorrect: {0}", uri);
                yield break;
            }

            WWWForm form = new WWWForm();

            var binaryImage = GetByteArrayFromImage(image);
            if (binaryImage == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "Can't read camera image! Please, check image format!");
                yield break;
            }
            form.AddBinaryData("image", binaryImage, CreateFileName());

            form.AddField("json", meta);

            MetricsCollector.Instance.StartStopwatch(ImageVPSRequest);

            yield return SendRequest(uri, form);

            MetricsCollector.Instance.StopStopwatch(ImageVPSRequest);

            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0} {1}", ImageVPSRequest, MetricsCollector.Instance.GetStopwatchSecondsAsString(ImageVPSRequest));

            callback();
        }

        public IEnumerator SendVpsRequest(byte[] embedding, string meta, System.Action callback)
        {
            string uri = Path.Combine(serverUrl, api_path_session).Replace("\\", "/");

            if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            {
                VPSLogger.LogFormat(LogLevel.ERROR, "URL is incorrect: {0}", uri);
                yield break;
            }

            WWWForm form = new WWWForm();

            form.AddBinaryData("embedding", embedding, "data.embd");

            form.AddField("json", meta);

            MetricsCollector.Instance.StartStopwatch(MVPSRequest);

            yield return SendRequest(uri, form);

            MetricsCollector.Instance.StopStopwatch(MVPSRequest);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] {0} {1}", MVPSRequest, MetricsCollector.Instance.GetStopwatchSecondsAsString(MVPSRequest));

            callback();
        }

        public LocalisationStatus GetStatus()
        {
            return locationState.Status;
        }

        public ErrorCode GetErrorCode()
        {
            return locationState.Error;
        }

        public LocalisationResult GetResponce()
        {
            return locationState.Localisation;
        }

        /// <summary>
        /// Create name for image from current date and time
        /// </summary>
        private string CreateFileName()
        {
            DateTime dateTime = DateTime.Now;
            string file = dateTime.ToString("yyyy-MM-dd-HH-mm-ss");
            file += ".jpg";
            return file;
        }

        /// <summary>
        /// Convert Texture2D to byte array
        /// </summary>
        private byte[] GetByteArrayFromImage(Texture2D image)
        {
            byte[] bytesOfImage = image.EncodeToJPG(100);
            return bytesOfImage;
        }

        /// <summary>
        /// Update latest response data
        /// </summary>
        /// <param name="Status">Status.</param>
        /// <param name="Error">Error.</param>
        /// <param name="Localisation">Localisation.</param>
        private void UpdateLocalisationState(LocalisationStatus Status, ErrorCode Error, LocalisationResult Localisation)
        {
            locationState.Status = Status;
            locationState.Error = Error;
            locationState.Localisation = Localisation;
        }

        private IEnumerator SendRequest(string uri, WWWForm form)
        {
            using (UnityWebRequest www = UnityWebRequest.Post(uri, form))
            {
                www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

                www.timeout = timeout;

                www.SendWebRequest();
                while (!www.isDone)
                {
                    yield return null;
                }

                if (www.isNetworkError || www.isHttpError)
                {
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, ErrorCode.NO_INTERNET, null);
                    VPSLogger.LogFormat(LogLevel.ERROR, "Network error: {0}", www.error);
                    yield break;
                }

                VPSLogger.LogFormat(LogLevel.DEBUG, "Request finished with code: {0}", www.responseCode);

                if (www.responseCode != 200)
                {
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, ErrorCode.SERVER_INTERNAL_ERROR, null);
                    yield break;
                }
                string response = www.downloadHandler.text;

                VPSLogger.LogFormat(LogLevel.DEBUG, "Request Finished Successfully!\n{0}", response);
                LocationState deserialized = null;
                try
                {
                    deserialized = DataCollector.Deserialize(response);
                }
                catch (Exception e)
                {
                    VPSLogger.Log(LogLevel.ERROR, e);
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, ErrorCode.SERVER_INTERNAL_ERROR, null);
                    yield break;
                }

                if (deserialized != null)
                {
                    VPSLogger.LogFormat(LogLevel.DEBUG, "Server status {0}", deserialized.Status);
                    locationState = deserialized;
                }
                else
                {
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, ErrorCode.SERVER_INTERNAL_ERROR, null);
                    VPSLogger.Log(LogLevel.ERROR, "There is no data come from server");
                    yield break;
                }
            }
        }
    }
}