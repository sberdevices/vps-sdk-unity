﻿using System;
using System.Collections;
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
        [Tooltip("Target photo resolution")]
        private Vector2Int desiredResolution = new Vector2Int(960, 540);
        private TextureFormat format = TextureFormat.RGB24;

        [Tooltip("Texture for sending")]
        public Texture2D FakeTexture;

        private Texture2D ppFakeTexture;
        private Texture2D convertTexture;
        private Dictionary<VPSTextureRequirement, NativeArray<byte>> buffers;

        private Image mockImage;

        private float resizeCoefficient = 1.0f;

        private VPSTextureRequirement textureRequir;

        private void Awake()
        {
            textureRequir = new VPSTextureRequirement(desiredResolution.x, desiredResolution.y, format);
        }

        public void Init(VPSTextureRequirement[] requirements)
        {
            FreeBufferMemory();

            var distinctRequir = requirements.Distinct().ToList();
            buffers = distinctRequir.ToDictionary(r => r, r => new NativeArray<byte>(r.Width * r.Height, Allocator.Persistent));

            InitBuffers();
        }

        private void OnValidate()
        {
            if (FakeTexture == null)
                return;

            if (textureRequir == null)
                Awake();

            ppFakeTexture = Preprocess(textureRequir.Format);
            resizeCoefficient = (float)ppFakeTexture.width / (float)desiredResolution.x;

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

            var toCopy = buffers.Keys.Where(key => key.Equals(textureRequir));
            var toCreate = buffers.Keys.Except(toCopy);

            foreach (var req in toCopy)
            {
                buffers[req].CopyFrom(ppFakeTexture.GetRawTextureData());
            }

            foreach (var req in toCreate)
            {
                convertTexture = Preprocess(req.Format);
                RectInt inputRect = req.GetCropRect(convertTexture.width, convertTexture.height, req.Width / req.Height);
                convertTexture = CropScale.CropTexture(convertTexture, new Vector2(inputRect.height, inputRect.width), CropOptions.CUSTOM, inputRect.x, inputRect.y);
                convertTexture = CropScale.ScaleTexture(convertTexture, req.Width, req.Height);
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
            return new Vector2(722.1238403320312f, 722.1238403320312f);
        }

        public Texture2D GetFrame()
        {
            return ppFakeTexture;
        }

        public NativeArray<byte> GetBuffer(VPSTextureRequirement requir)
        {
            return buffers[requir];
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
            return resizeCoefficient;
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
                rotatedTexture = new Texture2D(h, w);
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