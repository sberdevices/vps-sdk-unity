using System;
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
        // api for serial photo localization
        private string api_path_firstloc = "vps/api/v1/first_loc/job";
        // api for one photo localisation
        private string api_path = "vps/api/v1/job";

        private int timeout = 10;

        private LocationState locationState = new LocationState();

        public void SetUrl(string url)
        {
            serverUrl = url;
        }

        public IEnumerator SendVpsRequest(Texture2D image, string meta)
        {
            string uri = Path.Combine(serverUrl, api_path).Replace("\\", "/");

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

            yield return SendRequest(uri, form);
        }

        public IEnumerator SendVpsRequest(byte[] embedding, string meta)
        {
            string uri = Path.Combine(serverUrl, api_path).Replace("\\", "/");

            if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            {
                VPSLogger.LogFormat(LogLevel.ERROR, "URL is incorrect: {0}", uri);
                yield break;
            }

            WWWForm form = new WWWForm();

            form.AddBinaryData("embedding", embedding, "data.embd");

            form.AddField("json", meta);

            yield return SendRequest(uri, form);
        }

        public IEnumerator SendVpsLocalizationRequest(List<RequestLocalizationData> data)
        {
            string uri = Path.Combine(serverUrl, api_path_firstloc).Replace("\\", "/");

            if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            {
                VPSLogger.LogFormat(LogLevel.ERROR, "URL is incorrect: {0}", uri);
                yield break;
            }

            WWWForm form = new WWWForm();

            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].Embedding != null)
                {
                    form.AddBinaryData("embd" + i, data[i].Embedding, "data.embd");
                }
                else
                {
                    form.AddBinaryData("mes" + i, data[i].image, CreateFileName());
                }
                form.AddField("mes" + i, data[i].meta);
            }

            yield return SendRequest(uri, form);
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
            string file = "";
            DateTime dateTime = DateTime.Now;
            file = dateTime.ToString("yyyy-MM-dd-HH-mm-ss");
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