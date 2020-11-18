using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ARVRLab.VPSService
{
    /// <summary>
    /// Фейковая камера - выдает заданную картинку
    /// </summary>
    public class FakeCamera : MonoBehaviour, ICamera
    {
        [Tooltip("Текстура, которая будет отправлена")]
        public Texture2D FakeTexture;

        private NativeArray<byte> buffer;

        public Vector2 GetFocalPixelLength()
        {
            return new Vector2(1396.5250f, 1396.5250f);
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
            return new Vector2(FakeTexture.width * 0.5f, FakeTexture.height * 0.5f);
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
    }
}