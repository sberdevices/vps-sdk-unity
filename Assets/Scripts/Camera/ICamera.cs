using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public interface ICamera
    {
        void Init(VPSTextureRequirement FeautureExtractorRequirement, VPSTextureRequirement EncoderRequirement);
        bool IsCameraReady();
        Texture2D GetFrame();
        Vector2 GetFocalPixelLength();
        Vector2 GetPrincipalPoint();
        NativeArray<byte> GetImageFeatureExtractorBuffer();
        NativeArray<byte> GetImageEncoderBuffer();
        float GetResizeCoefficient();
    }
}
