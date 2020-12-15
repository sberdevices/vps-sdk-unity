using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ARVRLab.ARVRLab.VPSService.JSONs;
using Asyncoroutine;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class HttpClientRequestVPS : IRequestVPS
    {
        private string serverUrl;
        // api для локализации через серию фотографий
        private string api_path_firstloc = "vps/api/v1/first_loc/job";
        // api для стандартной работы
        private string api_path = "vps/api/v1/job";

        private int timeout = 5;

        private LocationState locationState = new LocationState();

        public void SetUrl(string url)
        {
            serverUrl = url;
        }

        /// <summary>
        /// Отправка запроса: изображение, meta-данные и выходы нейронки для извлечения фичей
        /// </summary>
        public IEnumerator SendVpsRequest(Texture2D image, string meta)
        {
            string uri = Path.Combine(serverUrl, api_path);

            if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            {
                Debug.LogError("URL is incorrect: " + uri);
                yield break;
            }

            MultipartFormDataContent form = new MultipartFormDataContent();

            var binaryImage = GetByteArrayFromImage(image);
            if (binaryImage == null)
            {
                Debug.LogError("Can't read camera image! Please, check image format!");
                yield break;
            }
            HttpContent img = new ByteArrayContent(binaryImage);
            form.Add(img, "image", CreateFileName());

            HttpContent metaContent = new StringContent(meta);
            form.Add(metaContent, "json");

            yield return Task.Run(() => SendRequest(uri, form)).AsCoroutine();
        }

        /// <summary>
        /// Отправка запроса: изображение, meta-данные и выходы нейронки для извлечения фичей
        /// </summary>
        public IEnumerator SendVpsRequest(byte[] embedding, string meta)
        {
            string uri = Path.Combine(serverUrl, api_path);

            if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            {
                Debug.LogError("URL is incorrect: " + uri);
                yield break;
            }

            MultipartFormDataContent form = new MultipartFormDataContent();

            HttpContent embd = new ByteArrayContent(embedding);
            form.Add(embd, "embedding", "data.embd");

            HttpContent metaContent = new StringContent(meta);
            form.Add(metaContent, "json");

            yield return Task.Run(() => SendRequest(uri, form)).AsCoroutine();
        }

        /// <summary>
        /// Отправка запроса: серия изображений и meta-данные к ним
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

            MultipartFormDataContent form = new MultipartFormDataContent();
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].Embedding != null)
                {
                    HttpContent embd = new ByteArrayContent(data[i].Embedding);
                    form.Add(embd, "embd" + i, "data.embd");
                }
                else
                {
                    HttpContent img = new ByteArrayContent(data[i].image);
                    form.Add(img, "mes" + i, CreateFileName());
                }

                HttpContent meta = new StringContent(data[i].meta);
                form.Add(meta, "mes" + i);
            }
            yield return Task.Run(() => SendRequest(uri, form)).AsCoroutine();
        }

        /// <summary>
        /// Выдает статус последнего запроса
        /// </summary>
        /// <returns>The status.</returns>
        public LocalisationStatus GetStatus()
        {
            return locationState.Status;
        }

        /// <summary>
        /// Выдает ошибку последнего запроса
        /// </summary>
        /// <returns>The error code.</returns>
        public ErrorCode GetErrorCode()
        {
            return locationState.Error;
        }

        /// <summary>
        /// Выдает ответ на последний запрос
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
            byte[] bytesOfImage = image.EncodeToJPG();
            return bytesOfImage;
        }

        /// <summary>
        /// Обновляет данные последнего ответа от сервера
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

        private void SendRequest(string uri, MultipartFormDataContent form)
        {
            using (var client = new HttpClient())
            {
                var result = client.PostAsync(uri, form);
                string resultContent = result.Result.Content.ReadAsStringAsync().Result;
                Debug.Log(resultContent);

                LocationState deserialized = null;
                try
                {
                    deserialized = DataCollector.Deserialize(resultContent);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, ErrorCode.SERVER_INTERNAL_ERROR, null);
                    return;
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
                    return;
                }
            }
        }
    }
}
