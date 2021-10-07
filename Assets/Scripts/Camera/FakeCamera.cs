﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private Vector2Int cameraResolution = new Vector2Int(1920, 1080);

        [Tooltip("Texture for sending")]
        public Texture2D FakeTexture;

        private Texture2D ppFakeTexture;
        private Texture2D convertTexture;
        private Dictionary<VPSTextureRequirement, NativeArray<byte>> buffers;

        private Image mockImage;
        private float resizeCoef = 1.0f;

        public void Init(VPSTextureRequirement[] requirements)
        {
            SetCameraFov();
            FreeBufferMemory();

            var distinctRequir = requirements.Distinct().ToList();
            buffers = distinctRequir.ToDictionary(r => r, r => new NativeArray<byte>(r.Width * r.Height * r.ChannelsCount(), Allocator.Persistent));

            InitBuffers();
            resizeCoef = (float)buffers.FirstOrDefault().Key.Width / (float)cameraResolution.x; 
        }

        private void OnValidate()
        {
            if (FakeTexture == null)
                return;

            FreeBufferMemory();
            InitBuffers();

            if (Application.isEditor && Application.isPlaying)
            {
                ShowMockFrame(FakeTexture);
                PrepareApplyer();
            }
        }

        private void InitBuffers()
        {
            if (buffers == null || buffers.Count == 0)
                return;

            foreach (var req in buffers.Keys)
            {
                convertTexture = Preprocess(req.Format);
                if (convertTexture.width != req.Width || convertTexture.height != req.Height)
                {
                    RectInt inputRect = req.GetCropRect(convertTexture.width, convertTexture.height, req.Width / req.Height);
                    convertTexture = CropScale.CropTexture(convertTexture, new Vector2(inputRect.height, inputRect.width), CropOptions.CUSTOM, inputRect.x, inputRect.y);
                    convertTexture = CropScale.ScaleTexture(convertTexture, req.Width, req.Height);
                }
                buffers[req].CopyFrom(convertTexture.GetRawTextureData());
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
            return new Vector2(1444.24768066f, 1444.24768066f);
        }

        public Texture2D GetFrame(VPSTextureRequirement requir)
        {
            if (ppFakeTexture == null || ppFakeTexture.width != requir.Width || ppFakeTexture.height != requir.Height || ppFakeTexture.format != requir.Format)
            {
                ppFakeTexture = new Texture2D(requir.Width, requir.Height, requir.Format, false);
            }

            ppFakeTexture.LoadRawTextureData(buffers[requir]);
            ppFakeTexture.Apply();
            return ppFakeTexture;
        }

        public NativeArray<byte> GetBuffer(VPSTextureRequirement requir)
        {
            return buffers[requir];
        }

        public Vector2 GetPrincipalPoint()
        {
            VPSTextureRequirement req = buffers.FirstOrDefault().Key;
            return new Vector2(req.Width, req.Height);
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
            if (buffers == null)
                return;

            foreach (var buffer in buffers.Values)
            {
                if (buffer != null && buffer.IsCreated)
                    buffer.Dispose();
            }
            buffers.Clear();
        }

        public float GetResizeCoefficient()
        {
            return resizeCoef;
        }

        private Texture2D Preprocess(TextureFormat format)
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
                rotatedTexture = new Texture2D(h, w, format, false);
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
                rotatedTexture = new Texture2D(h, w, format, false);
                rotatedTexture.SetPixels32(rotated);
            }

            rotatedTexture.Apply();
            return rotatedTexture;
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
                    VPSLogger.Log(LogLevel.ERROR, "No virtual camera on scene!");
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

        private void SetCameraFov()
        {
            Camera camera = Camera.main;

            float h = Screen.height;
            float fy = GetFocalPixelLength().y;

            float fovY = (float)(2 * Mathf.Atan(h / 2 / fy) * 180 / Mathf.PI);

            // устанавливает vertical
            camera.fieldOfView = fovY;
        }
    }
}