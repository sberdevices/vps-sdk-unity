﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class SettingsVPS
    {
        public string Url = "http://arvrlab.supercamera.eu.ngrok.io/";
        public float Timeout = 5;
        public int PhotosInSeria = 5;
        public bool AlwaysUseForceVPS = true;
        public bool SendOnlyFeatures = true;
    }
}