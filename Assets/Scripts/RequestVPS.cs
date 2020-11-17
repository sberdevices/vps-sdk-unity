using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ARVRLab.ARVRLab.VPSService.JSONs;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace ARVRLab.VPSService
{
    /// <summary>
    /// Запрос к серверу VPS
    /// </summary>
    public class RequestVPS
    {
        public static string RequestTime = "";

        private string serverUrl;
        // api для локализации через серию фотографий
        private string api_path_firstloc = "vps/api/first_loc/job";
        // api для стандартной работы
        private string api_path = "vps/api/v1/job";

        private LocationState locationState = new LocationState();

        public RequestVPS(string url)
        {
            serverUrl = url;
        }

        /// <summary>
        /// Отправка запроса: изображение и meta-данные
        /// </summary>
        /// <returns>The vps request.</returns>
        /// <param name="image">Image.</param>
        /// <param name="meta">Meta.</param>
        public IEnumerator SendVpsRequest(Texture2D image, string meta, string keyPoints, string scores, string descriptors, string globalDescriptor)
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
            form.AddField("keyPoints", keyPoints);
            form.AddField("scores", scores);
            form.AddField("descriptors", descriptors);
            form.AddField("globalDescriptor", globalDescriptor);

            //==================
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            //==================
            using (UnityWebRequest www = UnityWebRequest.Post(uri, form))
            {
                www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

                yield return www.SendWebRequest();

                //==================
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                RequestTime = String.Format("{0:00}:{1:00}",
                    ts.Seconds, ts.Milliseconds / 10);
                Debug.Log("Request Time " + RequestTime);
                //==================

                if (www.isNetworkError || www.isHttpError)
                {
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, ErrorCode.NO_INTERNET, null);
                    Debug.LogError("Network error!");
                    Debug.LogError(www.error);
                    Debug.LogError(www.responseCode);
                }
                else
                {
                    Debug.Log("Request finished with code: " + www.responseCode);

                    if (www.responseCode != 200)
                    {
                        UpdateLocalisationState(LocalisationStatus.GPS_ONLY, ErrorCode.SERVER_INTERNAL_ERROR, null);
                        yield return null;
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

            WWWForm form = new WWWForm();
            
            for (int i = 0; i < data.Count; i++)
            {
                form.AddBinaryData("mes" + i, data[i].image, CreateFileName());
                form.AddField("mes" + i, data[i].meta);
            }

            using (UnityWebRequest www = UnityWebRequest.Post(uri, form))
            {
                www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, ErrorCode.NO_INTERNET, null);
                    Debug.LogError("Network error!");
                }
                else
                {
                    Debug.Log("Request finished with code: " + www.responseCode);

                    if (www.responseCode != 200)
                    {
                        UpdateLocalisationState(LocalisationStatus.GPS_ONLY, ErrorCode.SERVER_INTERNAL_ERROR, null);
                        yield return null;
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
    }
}