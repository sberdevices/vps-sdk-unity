using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ARVRLab.VPSService
{
    /// <summary>
    /// Return FakeTexture image
    /// </summary>
    public class FakeCamera : MonoBehaviour, ICamera
    {
        [Tooltip("Target photo resolution")]
        private Vector2Int desiredResolution = new Vector2Int(960, 540);

        [Tooltip("Texture for sending")]
        public Texture2D FakeTexture;

        private NativeArray<byte> buffer;

        private float resizeCoefficient = 1.0f;

        private void Start()
        {
            resizeCoefficient = (float)desiredResolution.x / (float)FakeTexture.width;
        }

        public Vector2 GetFocalPixelLength()
        {
            return new Vector2(722.1238403320312f, 722.1238403320312f);
        }

        public Texture2D GetFrame()
        {
            return FakeTexture;
        }

        public NativeArray<byte> GetImageArray()
        {
            FreeBufferMemory();
            buffer = new NativeArray<byte>(FakeTexture.GetRawTextureData(), Allocator.Persistent); // 960*540*4
            return buffer;
        }

        public Vector2 GetPrincipalPoint()
        {
            return new Vector2(479.7787170410156f, 359.7473449707031f);
        }

        public bool IsCameraReady()
        {
            return FakeTexture != null;
        }

        private void OnDestroy()
        {
            FreeBufferMemory();
        }

        private void FreeBufferMemory()
        {
            if (buffer.IsCreated)
            {
                buffer.Dispose();
            }
        }

        public float GetResizeCoefficient()
        {
            return resizeCoefficient;
        }
    }
}