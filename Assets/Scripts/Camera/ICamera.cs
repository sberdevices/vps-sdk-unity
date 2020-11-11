using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public interface ICamera
    {
        bool IsCameraReady();
        Texture2D GetFrame();
        Vector2 GetFocalPixelLength();
        Vector2 GetPrincipalPoint();
        float[,,] GetImageArray();
    }
}
