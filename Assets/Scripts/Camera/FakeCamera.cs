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
        [Tooltip("Текстура, которая будет отправлена")]
        public Texture2D FakeTexture;

        /// <summary>
        /// Тут нужно вычислить
        /// </summary>
        /// <returns></returns>
        public Vector2 GetFocalPixelLength()
        {
            return new Vector2(1396.5250f, 1396.5250f);
        }

        public Texture2D GetFrame()
        {
            return FakeTexture;
        }

        public Vector2 GetPrincipalPoint()
        {
            return new Vector2(FakeTexture.width * 0.5f, FakeTexture.height * 0.5f);
        }

        public bool IsCameraReady()
        {
            return FakeTexture != null;
        }
    }
}