﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class FakeSeriaTracking : MonoBehaviour, ITracking
    {
        private string DefaultGuidPointcloud = "";

        private TrackingData trackingData;

        private int Counter = 0;

        public string ImagesPath = "/Users/admin/arnavigation/Modules/VPSServise/Assets/Images";

        public TextAsset[] CustomPoses;

        public VPSBuilding Buiding;

        private void Awake()
        {
            string [] files = Directory.GetFiles(ImagesPath);
            CustomPoses = new TextAsset[files.Length / 4];
            int j1 = 0, j2 = 0;

            FakeSeriaCamera cam = GetComponent<FakeSeriaCamera>();
            cam.FakeTextures = new Texture2D[files.Length / 4];

            GetComponent<ServiceProvider>().PhotosInSeria = files.Length / 4;
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Contains("meta"))
                    continue;

                if (files[i].Contains("dat") || files[i].Contains("txt"))
                {
                    CustomPoses[j1] = new TextAsset(File.ReadAllText(files[i]));
                    j1++;
                }
                else
                {
                    cam.FakeTextures[j2] = new Texture2D(1920, 1080);
                    cam.FakeTextures[j2].LoadImage(File.ReadAllBytes(files[i]));
                    j2++;
                }
            }
        }

        private void Start()
        {
            if (Buiding == VPSBuilding.Bootcamp)
            {
                DefaultGuidPointcloud = "eeb38592-4a3c-4d4b-b4c6-38fd68331521";
            }
            else if (Buiding == VPSBuilding.Polytech)
            {
                DefaultGuidPointcloud = "Polytech";
            }
            trackingData = new TrackingData
            {
                GuidPointcloud = DefaultGuidPointcloud
            };
        }

        private void UpdateTrackingData()
        {
            Pose currentPose = MetaParser.Parse(CustomPoses[Counter].text);
            trackingData.Position = currentPose.position;
            trackingData.Rotation = currentPose.rotation;
            Counter++;
            if (Counter >= CustomPoses.Length)
                Counter = 0;
        }

        /// <summary>
        /// Значения трекинга обновляются только при запросе на выдачу
        /// </summary>
        /// <returns>The local tracking.</returns>
        public TrackingData GetLocalTracking()
        {
            UpdateTrackingData();
            return trackingData;
        }

        public void SetGuidPointcloud(string guid)
        {
            trackingData.GuidPointcloud = guid;
            trackingData.IsLocalisedFloor = true;
        }
    }
}
