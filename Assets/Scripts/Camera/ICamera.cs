using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public interface ICamera
    {
        void Init(VPSTextureRequirement[] requirements);
        bool IsCameraReady();
        Texture2D GetFrame(VPSTextureRequirement requir);
        Vector2 GetFocalPixelLength();
        Vector2 GetPrincipalPoint();
        NativeArray<byte> GetBuffer(VPSTextureRequirement requir);
        float GetResizeCoefficient();
    }
}
