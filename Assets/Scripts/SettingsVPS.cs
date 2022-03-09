using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class SettingsVPS
    {
        // Server url
        public string Url = "";
        // Delay between sending
        public float localizationTimeout = 1;
        // Delay between sending
        public float calibrationTimeout = 2.5f;

        public SettingsVPS(string buildingUrl)
        {
            Url = buildingUrl;
        }
    }
}