﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ARVRLab.VPSService
{
    public class VPSPrepareStatus
    {
        private const string url = "http://metaservices.arvr.sberlabs.com/upload/weights/hfnet_i8_960.tflite";
        private string dataPath;
        private float progress = 0;

        public event System.Action OnVPSReady;

        public VPSPrepareStatus()
        {
            dataPath = Path.Combine(Application.persistentDataPath, "hfnet_i8_960.tflite");
            // if mobileVPS already ready
            if (IsReady())
            {
                progress = 1;
            }
        }

        /// <summary>
        /// Download mobileVPS
        /// </summary>
        public IEnumerator DownloadNeural()
        {
            while (true)
            {
                yield return new WaitWhile(() => Application.internetReachability == NetworkReachability.NotReachable);
                using (UnityWebRequest www = UnityWebRequest.Get(url))
                {
                    www.SendWebRequest();
                    while (!www.isDone)
                    {
                        progress = www.downloadProgress;
                        yield return null;
                    }

                    // check error
                    if (www.isNetworkError || www.isHttpError)
                    {
                        Debug.LogError("Can't download mobile vps network: " + www.error);
                        yield return null;
                        continue;
                    }

                    progress = www.downloadProgress;
                    File.WriteAllBytes(dataPath, www.downloadHandler.data);
                    Debug.Log("Mobile vps network downloaded successfully!");
                    OnVPSReady?.Invoke();
                    break;
                }
            }
        }

        /// <summary>
        /// Get download progress (between 0 and 1)
        /// </summary>
        public float GetProgress()
        {
            return progress;
        }

        /// <summary>
        /// Is mobileVPS ready?
        /// </summary>
        public bool IsReady()
        {
            return File.Exists(dataPath);
        }
    }
}
