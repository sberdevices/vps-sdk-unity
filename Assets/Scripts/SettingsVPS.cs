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

        public SettingsVPS(string buildingName, string buildingGuid, ServerType serverType)
        {
            Url = URLController.CreateURL(buildingName, buildingGuid, serverType);
        }

        public SettingsVPS(string customUrl)
        {
            Url = customUrl;
        }
    }
}