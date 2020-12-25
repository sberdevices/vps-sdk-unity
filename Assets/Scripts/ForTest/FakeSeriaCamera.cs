using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class FakeSeriaCamera : MonoBehaviour, ICamera
    {
        public Texture2D[] FakeTextures;

        private int Counter = 0;

        private NativeArray<byte> buffer;

        public Vector2 GetFocalPixelLength()
        {
            return new Vector2(1396.5250f, 1396.5250f);
        }

        public Texture2D GetFrame()
        {
            Counter++;
            if (Counter >= FakeTextures.Length)
                Counter = 1;
            return FakeTextures[Counter - 1];
        }

        public NativeArray<byte> GetImageArray()
        {
            FreeBufferMemory();
            Counter++;
            if (Counter >= FakeTextures.Length)
                Counter = 1;
            buffer = new NativeArray<byte>(FakeTextures[Counter - 1].GetRawTextureData(), Allocator.Persistent);
            return buffer;
        }

        public Vector2 GetPrincipalPoint()
        {
            return new Vector2(FakeTextures[0].width * 0.5f, FakeTextures[0].height * 0.5f);
        }

        public bool IsCameraReady()
        {
            return FakeTextures[0] != null;
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
    }
}
