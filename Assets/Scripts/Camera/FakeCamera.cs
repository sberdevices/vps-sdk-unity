using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ARVRLab.VPSService
{
    /// <summary>
    /// Фейковая камера - выдает заданную картинку
    /// </summary>
    public class FakeCamera : MonoBehaviour, ICamera
    {
        public RawImage Raw_image;
        private Texture2D texture;

        private void Start()
        {
            GetTextureFromFake();
        }

        /// <summary>
        /// Забираем текстуру с RawImage
        /// </summary>
        /// <returns>The texture from fake.</returns>
        private Texture2D GetTextureFromFake()
        {
            RenderTexture rt = new RenderTexture(Raw_image.texture.width, Raw_image.texture.height, 0);
            RenderTexture.active = rt;
            Graphics.Blit(Raw_image.texture, rt);

            texture = new Texture2D(Raw_image.texture.width, Raw_image.texture.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            texture.Apply();

            return texture;
        }

        public Vector2 GetFocalPixelLength()
        {
            return Vector2.zero;
        }

        public Texture2D GetFrame()
        {
            return texture;
        }

        public Vector2 GetPrincipalPoint()
        {
            return Vector2.zero;
        }

        public bool IsCameraReady()
        {
            return texture != null;
        }
    }
}