using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ARVRLab.ARVRLab.VPSService.JSONs;
using UnityEngine;

namespace ARVRLab.VPSService
{
    /// <summary>
    /// Requst to VPS server
    /// </summary>
    public class HttpClientRequestVPS : IRequestVPS
    {
        private string serverUrl;
        // api for localisation
        private string api_path_session = "vps/api/v2";

        private int timeout = 4;

        private LocationState locationState = new LocationState();

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

            MultipartFormDataContent form = new MultipartFormDataContent();

            var binaryImage = GetByteArrayFromImage(image);
            if (binaryImage == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "Can't read camera image! Please, check image format!");
                yield break;
            }
            HttpContent img = new ByteArrayContent(binaryImage);
            form.Add(img, "image", CreateFileName());

            HttpContent metaContent = new StringContent(meta);
            form.Add(metaContent, "json");

            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            yield return Task.Run(() => SendRequest(uri, form, timeout)).AsCoroutine();

            stopWatch.Stop();
            TimeSpan requestTS = stopWatch.Elapsed;

            string requestTime = String.Format("{0:N10}", requestTS.TotalSeconds);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric" + SettingsToggles.GetLocType() + "] ImageVPSRequest {0}", requestTime);

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

            MultipartFormDataContent form = new MultipartFormDataContent();

            HttpContent embd = new ByteArrayContent(embedding);
            form.Add(embd, "embedding", "data.embd");

            HttpContent metaContent = new StringContent(meta);
            form.Add(metaContent, "json");

            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            yield return Task.Run(() => SendRequest(uri, form, timeout)).AsCoroutine();

            stopWatch.Stop();
            TimeSpan requestTS = stopWatch.Elapsed;

            string requestTime = String.Format("{0:N10}", requestTS.TotalSeconds);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric" + SettingsToggles.GetLocType() + "] MVPSRequest {0}", requestTime);

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

        private void SendRequest(string uri, MultipartFormDataContent form, int timeout)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(timeout);
                string resultContent = "";
                try
                {
                    var result = client.PostAsync(uri, form);

                    resultContent = result.Result.Content.ReadAsStringAsync().Result;
                    VPSLogger.LogFormat(LogLevel.DEBUG, "Server answer: {0}", resultContent);
                }
                catch
                {
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, ErrorCode.NO_INTERNET, null);
                    return;
                }

                LocationState deserialized = null;
                try
                {
                    deserialized = DataCollector.Deserialize(resultContent);
                }
                catch (Exception e)
                {
                    VPSLogger.Log(LogLevel.ERROR, e);
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, ErrorCode.SERVER_INTERNAL_ERROR, null);
                    return;
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
                    return;
                }
            }
        }
    }
}
