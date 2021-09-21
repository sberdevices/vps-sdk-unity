using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ARVRLab.VPSService
{
    public class VPSPrepareStatus
    {
        private const string url = "https://testable1.s3pd01.sbercloud.ru/vpsmobiletflite/230421/hfnet_i8_960.tflite";
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
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    VPSLogger.Log(LogLevel.ERROR, "No internet to download MobileVPS");
                }
                yield return new WaitWhile(() => Application.internetReachability == NetworkReachability.NotReachable);
                using (UnityWebRequest www = UnityWebRequest.Get(url))
                {
                    www.SendWebRequest();
                    VPSLogger.Log(LogLevel.DEBUG, "Start downloading MobileVPS");
                    while (!www.isDone)
                    {
                        progress = www.downloadProgress;
                        VPSLogger.LogFormat(LogLevel.DEBUG, "Current progress: {0}", progress);
                        yield return null;
                    }

                    // check error
                    if (www.isNetworkError || www.isHttpError)
                    {
                        VPSLogger.LogFormat(LogLevel.ERROR, "Can't download mobile vps network: {0}", www.error);
                        yield return null;
                        continue;
                    }

                    progress = www.downloadProgress;
                    File.WriteAllBytes(dataPath, www.downloadHandler.data);
                    VPSLogger.Log(LogLevel.DEBUG, "Mobile vps network downloaded successfully!");
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
        private bool IsReady()
        {
            return File.Exists(dataPath);
        }
    }
}
