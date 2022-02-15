﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class LocalisationResult
    {
        public Vector3 LocalPosition;
        public Vector3 LocalRotation;
        public double GpsLatitude;
        public double GpsLongitude;
        public float Heading;
        public float Accuracy;
        public double Timestamp;

        public LocalisationResult()
        {
            LocalPosition = Vector3.zero;
            LocalRotation = Vector3.zero;
            GpsLatitude = 0;
            GpsLongitude = 0;
            Heading = 0;
            Accuracy = 0;
            Timestamp = 0;
        }
    }
}