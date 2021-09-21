using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

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

        private Texture2D ppFakeTexture;
        private Texture2D imageFeatureExtractorTexture;
        private Texture2D imageEncoderTexture;

        private Image mockImage;

        private float resizeCoefficient = 1.0f;

        private VPSTextureRequirement feautureExtractorRequirement;
        private VPSTextureRequirement encoderRequirement;

        public void Init(VPSTextureRequirement FeautureExtractorRequirement, VPSTextureRequirement EncoderRequirement)
        {
            FreeBufferMemory();

            feautureExtractorRequirement = FeautureExtractorRequirement;
            encoderRequirement = EncoderRequirement;

            imageFeatureExtractorTexture = new Texture2D(1, 1);
            imageEncoderTexture = new Texture2D(1, 1);
            InitBuffers();
        }

        private void OnValidate()
        {
            Preprocess();
            resizeCoefficient = (float)ppFakeTexture.width / (float)desiredResolution.x;

            InitBuffers();

            if (Application.isEditor && Application.isPlaying)
            {
                ShowMockFrame(FakeTexture);
                PrepareApplyer();
            }
        }

        private void InitBuffers()
        {
            if (feautureExtractorRequirement == null || encoderRequirement == null)
                return;

            RectInt inputRect = feautureExtractorRequirement.GetCropRect(ppFakeTexture.width, ppFakeTexture.height, feautureExtractorRequirement.Width / feautureExtractorRequirement.Height);
            imageFeatureExtractorTexture = CropScale.CropTexture(ppFakeTexture, new Vector2(inputRect.height, inputRect.width), CropOptions.CUSTOM, inputRect.x, inputRect.y);
            imageFeatureExtractorTexture = CropScale.ScaleTexture(imageFeatureExtractorTexture, feautureExtractorRequirement.Width, feautureExtractorRequirement.Height);

            if (feautureExtractorRequirement.Equals(encoderRequirement))
            {
                imageEncoderTexture = imageFeatureExtractorTexture;
            }
            else
            {
                inputRect = encoderRequirement.GetCropRect(ppFakeTexture.width, ppFakeTexture.height, encoderRequirement.Width / encoderRequirement.Height);
                imageEncoderTexture = CropScale.CropTexture(ppFakeTexture, new Vector2(inputRect.height, inputRect.width), CropOptions.CUSTOM, inputRect.x, inputRect.y);
                imageEncoderTexture = CropScale.ScaleTexture(imageEncoderTexture, encoderRequirement.Width, encoderRequirement.Height);
            }
        }

        private void PrepareApplyer()
        {
            var applyer = FindObjectOfType<ARFoundationApplyer>();
            if (applyer)
                applyer.RotateOnlyY = false;
        }

        public Vector2 GetFocalPixelLength()
        {
            return new Vector2(722.1238403320312f, 722.1238403320312f);
        }

        public Texture2D GetFrame()
        {
            return ppFakeTexture;
        }

        public NativeArray<byte> GetImageEncoderBuffer()
        {
            return imageEncoderTexture.GetRawTextureData<byte>();
        }

        public NativeArray<byte> GetImageFeatureExtractorBuffer()
        {
            return imageFeatureExtractorTexture.GetRawTextureData<byte>();
        }

        public Vector2 GetPrincipalPoint()
        {
            return new Vector2(480f, 270f);
        }

        public bool IsCameraReady()
        {
            return ppFakeTexture != null;
        }

        private void OnDestroy()
        {
            FreeBufferMemory();
        }

        private void FreeBufferMemory()
        {
            imageFeatureExtractorTexture = null;
            imageEncoderTexture = null;
        }

        public float GetResizeCoefficient()
        {
            return resizeCoefficient;
        }

        private void Preprocess()
        {
            Color32[] original = FakeTexture.GetPixels32();
            Color32[] rotated = new Color32[original.Length];
            int w = FakeTexture.width;
            int h = FakeTexture.height;

            int iRotated, iOriginal;

            for (int j = 0; j < h; ++j)
            {
                for (int i = 0; i < w; ++i)
                {
                    iRotated = (i + 1) * h - j - 1;
                    iOriginal = j * w + i;
                    rotated[iRotated] = original[iOriginal];
                }
            }

            bool onlyFeatures = FindObjectOfType<VPSLocalisationService>().SendOnlyFeatures;
            Texture2D rotatedTexture;
            if (onlyFeatures)
            {
                rotatedTexture = new Texture2D(h, w, TextureFormat.R8, false);
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        Color pixel = FakeTexture.GetPixel(j, i);
                        pixel.g = 0;
                        pixel.b = 0;
                        rotatedTexture.SetPixel(h - i - 1, w - j - 1, pixel);
                    }
                }
            }
            else
            {
                rotatedTexture = new Texture2D(h, w);
                rotatedTexture.SetPixels32(rotated);
            }

            rotatedTexture.Apply();
            ppFakeTexture = rotatedTexture;
        }

        private void ShowMockFrame(Texture mockTexture)
        {
            if (!mockImage)
            {
                var canvasGO = new GameObject("FakeCamera");
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;

                var camera = FindObjectOfType<Camera>();
                if (!camera)
                {
                    Debug.LogError("No virtual camera on scene!");
                    return;
                }

                canvas.worldCamera = camera;
                canvas.planeDistance = camera.farClipPlane - 10f;

                var imgGO = new GameObject("FakeFrame");
                var imgTransform = imgGO.AddComponent<RectTransform>();
                imgTransform.SetParent(canvasGO.transform, false);

                imgTransform.anchorMin = Vector2.zero;
                imgTransform.anchorMax = Vector2.one;

                mockImage = imgGO.AddComponent<Image>();
                mockImage.preserveAspect = true;
            }

            mockImage.sprite = Sprite.Create((Texture2D)mockTexture, new Rect(0, 0, mockTexture.width, mockTexture.height), Vector2.zero);
        }
    }
}