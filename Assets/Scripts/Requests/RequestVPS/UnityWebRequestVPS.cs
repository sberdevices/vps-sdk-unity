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
        // api for seria photo localization
        private string api_path_firstloc = "vps/api/v1/first_loc/job";
        // api for one photo localisation
        private string api_path = "vps/api/v1/job";

        private int timeout = 10;

        private LocationState locationState = new LocationState();

        public void SetUrl(string url)
        {
            serverUrl = url;
        }

        /// <summary>
        /// Send requst: image and meta and выходы нейронки для извлечения фичей
        /// </summary>
        public IEnumerator SendVpsRequest(Texture2D image, string meta)
        {
            string uri = Path.Combine(serverUrl, api_path);

            if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            {
                Debug.LogError("URL is incorrect: " + uri);
                yield break;
            }

            WWWForm form = new WWWForm();

            var binaryImage = GetByteArrayFromImage(image);
            if (binaryImage == null)
            {
                Debug.LogError("Can't read camera image! Please, check image format!");
                yield break;
            }
            form.AddBinaryData("image", binaryImage, CreateFileName());

            form.AddField("json", meta);

            yield return SendRequest(uri, form);
        }

        /// <summary>
        /// Send requst: image and meta and mobileVPS result
        /// </summary>
        public IEnumerator SendVpsRequest(byte[] embedding, string meta)
        {
            string uri = Path.Combine(serverUrl, api_path);

            if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            {
                Debug.LogError("URL is incorrect: " + uri);
                yield break;
            }

            WWWForm form = new WWWForm();

            form.AddBinaryData("embedding", embedding, "data.embd");

            form.AddField("json", meta);

            yield return SendRequest(uri, form);
        }

        /// <summary>
        /// Send requst: photo seria and meta 
        /// </summary>
        /// <returns>The vps localization request.</returns>
        /// <param name="data">Data.</param>
        public IEnumerator SendVpsLocalizationRequest(List<RequestLocalizationData> data)
        {
            string uri = Path.Combine(serverUrl, api_path_firstloc);

            if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            {
                Debug.LogError("URL is incorrect: " + uri);
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

        /// <summary>
        /// Get latest request status
        /// </summary>
        /// <returns>The status.</returns>
        public LocalisationStatus GetStatus()
        {
            return locationState.Status;
        }

        /// <summary>
        /// Get latest request error
        /// </summary>
        /// <returns>The error code.</returns>
        public ErrorCode GetErrorCode()
        {
            return locationState.Error;
        }
        /// <summary>
        /// Get latest request responce
        /// </summary>
        /// <returns>The responce.</returns>
        public LocalisationResult GetResponce()
        {
            return locationState.Localisation;
        }

        private string CreateFileName()
        {
            string file = "";
            DateTime dateTime = DateTime.Now;
            file = dateTime.ToString("yyyy-MM-dd-HH-mm-ss");
            file += ".jpg";
            return file;
        }

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
                    Debug.LogError("Network error: " + www.error);
                    yield break;
                }

                Debug.Log("Request finished with code: " + www.responseCode);

                if (www.responseCode != 200)
                {
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, ErrorCode.SERVER_INTERNAL_ERROR, null);
                    yield break;
                }
                string response = www.downloadHandler.text;

                Debug.Log("Request Finished Successfully!\n" + response);
                LocationState deserialized = null;
                try
                {
                    deserialized = DataCollector.Deserialize(response);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, ErrorCode.SERVER_INTERNAL_ERROR, null);
                    yield break;
                }

                if (deserialized != null)
                {
                    Debug.Log("Server status " + deserialized.Status);
                    locationState = deserialized;
                }
                else
                {
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, ErrorCode.SERVER_INTERNAL_ERROR, null);
                    Debug.LogError("There is no data come from server");
                    yield break;
                }
            }
        }
    }
}