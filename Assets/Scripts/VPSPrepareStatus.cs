using System.Collections;
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
            // если файл уже закачан, прогресс равен единице
            if (IsReady())
            {
                progress = 1;
            }
        }

        /// <summary>
        /// Корутина загрузки с сервера
        /// </summary>
        public IEnumerator DownloadNeural()
        {
            yield return new WaitWhile(() => Application.internetReachability == NetworkReachability.NotReachable);
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.SendWebRequest();
                while (!www.isDone)
                {
                    progress = www.downloadProgress;
                    Debug.Log(progress);
                    yield return null;
                }
                // после окончания скачивания прогресс равен единице
                progress = www.downloadProgress;

                // проверка ошибки
                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.LogError("Can't download mobile vps network: " + www.error);
                    yield break;
                }

                File.WriteAllBytes(dataPath, www.downloadHandler.data);
                Debug.Log("Mobile vps network downloaded successfully!");
                OnVPSReady?.Invoke();
            }
        }

        /// <summary>
        /// Возвращает прогресс скачивания от 0 до 1
        /// </summary>
        public float GetProgress()
        {
            return progress;
        }

        /// <summary>
        /// Проверяет, скачана ли нейронка
        /// </summary>
        public bool IsReady()
        {
            return File.Exists(dataPath);
        }
    }
}
