using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ARVRLab.VPSService
{
    public class DownloadNeuronStatus
    {
        private const string bucketPath = "https://testable1.s3pd01.sbercloud.ru/mobilevpstflite";

        public string Name;
        public string Url;
        public string DataPath;
        public float Progress;

        public DownloadNeuronStatus(string name)
        {
            Name = name;
            Url = Path.Combine(bucketPath, name);
            DataPath = Path.Combine(Application.persistentDataPath, name);
            Progress = 0f;
        }
    }

    public class VPSPrepareStatus
    {
        private DownloadNeuronStatus imageEncoder;
        private DownloadNeuronStatus imageFeatureExtractor;

        public event System.Action OnVPSReady;

        public VPSPrepareStatus()
        {
            imageEncoder = new DownloadNeuronStatus("mnv_960x540x1_4096.tflite");
            imageFeatureExtractor = new DownloadNeuronStatus("msp_960x540x1_256_400.tflite");

            // if mobileVPS already ready
            if (IsReady())
            {
                imageEncoder.Progress = 1;
                imageFeatureExtractor.Progress = 1;
            }
        }

        /// <summary>
        /// Download mobileVPS
        /// </summary>
        public IEnumerator DownloadNeurals()
        {
            while (!File.Exists(imageEncoder.DataPath) || !File.Exists(imageFeatureExtractor.DataPath))
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    Debug.Log("No internet to download MobileVPS");
                }
                yield return new WaitWhile(() => Application.internetReachability == NetworkReachability.NotReachable);
                Debug.Log("Start downloading MobileVPS");

                yield return DownloadNeural(imageFeatureExtractor);
                yield return DownloadNeural(imageEncoder);
            }

            Debug.Log("Mobile vps network downloaded successfully!");
            OnVPSReady?.Invoke();
        }

        public IEnumerator DownloadNeural(DownloadNeuronStatus neuron)
        {
            if (File.Exists(neuron.DataPath))
            {
                neuron.Progress = 1;
                yield break;
            }

            using (UnityWebRequest www = UnityWebRequest.Get(neuron.Url))
            {
                www.SendWebRequest();
                while (!www.isDone)
                {
                    neuron.Progress = www.downloadProgress;
                    Debug.Log(GetProgress());
                    yield return null;
                }

                // check error
                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.LogError("Can't download mobile vps network: " + www.error);
                    yield break;
                }

                neuron.Progress = www.downloadProgress;
                File.WriteAllBytes(neuron.DataPath, www.downloadHandler.data);
            }
        }

        /// <summary>
        /// Get download progress (between 0 and 1)
        /// </summary>
        public float GetProgress()
        {
            return (imageEncoder.Progress + imageFeatureExtractor.Progress) / 2f;
        }

        /// <summary>
        /// Is mobileVPS ready?
        /// </summary>
        private bool IsReady()
        {
            return File.Exists(imageEncoder.DataPath) && File.Exists(imageFeatureExtractor.DataPath);
        }
    }
}
