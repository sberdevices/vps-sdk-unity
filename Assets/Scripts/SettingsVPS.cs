using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class SettingsVPS
    {
        public string Url = "http://89.108.75.122:5001/";
        public float Timeout = 5;
        public int ForceVpsCount = 3; // пока не используется
        public int PhotosInSeria = 5;
    }
}