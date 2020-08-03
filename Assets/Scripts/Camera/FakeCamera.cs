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


        /// <summary>
        /// Тут нужно вычислить
        /// </summary>
        /// <returns></returns>
        public Vector2 GetFocalPixelLength()
        {
            float angle = 45f;
            // вроде бы так
            return new Vector2((texture.width * 0.5f) / Mathf.Tan(angle * 0.5f), (texture.height * 0.5f) / Mathf.Tan(angle * 0.5f));
        }

        public Texture2D GetFrame()
        {
            return texture;
        }

        public Vector2 GetPrincipalPoint()
        {
            return new Vector2(texture.width * 0.5f, texture.height * 0.5f);
        }

        public bool IsCameraReady()
        {
            return texture != null;
        }
    }
}