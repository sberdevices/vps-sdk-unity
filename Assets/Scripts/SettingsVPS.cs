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
        public float calibrationTimeout = 5;

        public string defaultLocationId = "";

        public SettingsVPS(string buildingUrl, string buildingGuid)
        {
            Url = buildingUrl;
            defaultLocationId = buildingGuid;
        }
    }
}