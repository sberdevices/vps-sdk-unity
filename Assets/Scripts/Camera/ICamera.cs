using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public enum VPSOrientation { LandscapeLeft, Portrait, LandscapeRight, PortraitUpsideDown }

    public interface ICamera
    {
        /// <summary>
        /// Initializes requirements for camera image
        /// </summary>
        void Init(VPSTextureRequirement[] requirements);
        /// <summary>
        /// Check the readiness of the camera
        /// </summary>
        bool IsCameraReady();
        /// <summary>
        /// Get current camera image
        /// </summary>
        Texture2D GetFrame(VPSTextureRequirement requir);
        /// <summary>
        /// Get camera focal pixel length
        /// </summary>
        Vector2 GetFocalPixelLength();
        /// <summary>
        /// Get camera principal point
        /// </summary>
        Vector2 GetPrincipalPoint();
        /// <summary>
        /// Get image as NativeArray by requirement
        /// </summary>
        NativeArray<byte> GetBuffer(VPSTextureRequirement requir);
        /// <summary>
        /// Get device orientation
        /// </summary>
        VPSOrientation GetOrientation();
    }
}
