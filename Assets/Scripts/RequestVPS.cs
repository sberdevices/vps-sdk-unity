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
    public class RequestVPS
    {
        private string ServerUrl;
        private string api_path = "vps/api/v1/job";

        private LocationState locationState = new LocationState();

        public RequestVPS(string url)
        {
            ServerUrl = url;
        }

        public IEnumerator SendVpsRequest(Texture2D Image, string Meta)
        {
            string uri = Path.Combine(ServerUrl, api_path);

            if (!Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            {
                Debug.LogError("URL is incorrect: " + uri);
                yield return null;
            }

            WWWForm form = new WWWForm();
            form.AddField("image", "file");
            form.AddBinaryData("image", GetByteArrayFromImage(Image), CreateFileName());
            form.AddField("json", Meta);

            using (UnityWebRequest www = UnityWebRequest.Post(uri, form))
            {
                www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.LogError("Network error!");
                    UpdateLocalisationState(LocalisationStatus.GPS_ONLY, ErrorCode.NO_INTERNET, null);
                    //JobHandlerVPS.FailHandler(null);
                }
                else
                {
                    Debug.Log("Request finished with code: " + www.responseCode);

                    if (www.responseCode != 200)
                    {
                        //JobHandlerVPS.FailHandler(null);
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
                    }

                    if (deserialized != null /*&& deserialized.data != null*/)
                    {
                        Debug.Log("Server status " + deserialized.Status);
                        locationState = deserialized;

                        if (deserialized.Status == LocalisationStatus.NO_LOCALISATION)
                        {
                            //JobHandlerVPS.ProgressHandler(deserialized);
                        }
                        else if (deserialized.Status == LocalisationStatus.VPS_READY)
                        {
                            //JobHandlerVPS.DoneHandler(deserialized);
                        }
                        else //LocalisationStatus.GPS_ONLY
                        {
                            //JobHandlerVPS.FailHandler(deserialized);
                        }
                    }
                    else
                    {
                        Debug.LogError("There is no data come from server");
                        //JobHandlerVPS.FailHandler(null);
                    }
                }
            }
        }

        public LocalisationStatus GetStatus()
        {
            return locationState.Status;
        }

        public LocalisationResult GetResponce()
        {
            return locationState.Localisation;
        }

        public ErrorCode GetErrorCode()
        {
            return locationState.Error;
        }

        private string CreateFileName()
        {
            string file = "";
            DateTime dateTime = DateTime.Now;
            file = dateTime.ToString("yyyy-MM-dd-HH-mm-ss");
            file += ".jpg";
            return file;
        }

        // Get a byte array from RawImage.texture
        private byte[] GetByteArrayFromImage(Texture2D image)
        {
            byte[] bytesOfImage = image.EncodeToJPG();
            return bytesOfImage;
        }

        private void UpdateLocalisationState(LocalisationStatus Status, ErrorCode Error, LocalisationResult Localisation)
        {
            locationState.Status = Status;
            locationState.Error = Error;
            locationState.Localisation = Localisation;
        }
    }
}