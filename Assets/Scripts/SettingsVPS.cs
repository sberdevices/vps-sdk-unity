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
        public float Timeout = 5;

        public string defaultLocationId = "";

        public SettingsVPS(string buildingUrl, string buildingGuid)
        {
            Url = buildingUrl;
            defaultLocationId = buildingGuid;
        }
    }
}