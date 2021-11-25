using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class FakeSerialCamera : MonoBehaviour, ICamera
    {
        private Vector2Int cameraResolution = new Vector2Int(1920, 1080);

        public Texture2D[] FakeTextures;
        private Texture2D ppFakeTexture;
        private Texture2D convertTexture;

        private Dictionary<VPSTextureRequirement, NativeArray<byte>> buffers;

        private int Counter = 0;

        private VPSTextureRequirement textureRequir;
        private float resizeCoef = 1.0f;

        private void Awake()
        {
            LocalizationImagesCollector.OnPhotoAdded += IncPhotoCounter;
        }

        /// <summary>
        /// Switch to next texture
        /// </summary>
        private void IncPhotoCounter()
        {
            Counter++;
            if (Counter >= FakeTextures.Length)
                Counter = 0;
            InitBuffers();
        }

        public void Init(VPSTextureRequirement[] requirements)
        {
            SetCameraFov();
            FreeBufferMemory();

            var distinctRequir = requirements.Distinct().ToList();
            buffers = distinctRequir.ToDictionary(r => r, r => new NativeArray<byte>(r.Width * r.Height * r.ChannelsCount(), Allocator.Persistent));

            InitBuffers();

            resizeCoef = (float)buffers.FirstOrDefault().Key.Width / (float)cameraResolution.y; 
        }

        /// <summary>
        /// Init all buffers from image by requrements
        /// </summary>
        private void InitBuffers()
        {
            if (buffers == null || buffers.Count == 0)
                return;

            foreach (var req in buffers.Keys)
            {
                convertTexture = Preprocess(FakeTextures[Counter], req.Format);
                if (convertTexture.width != req.Width || convertTexture.height != req.Height)
                {
                    RectInt inputRect = req.GetCropRect(convertTexture.width, convertTexture.height, ((float)req.Height) / ((float)req.Width));
                    convertTexture = CropScale.CropTexture(convertTexture, new Vector2(inputRect.height, inputRect.width), req.Format, CropOptions.CUSTOM, inputRect.x, inputRect.y);
                    convertTexture = CropScale.ScaleTexture(convertTexture, req.Width, req.Height, req.Format);
                }
                buffers[req].CopyFrom(convertTexture.GetRawTextureData());
            }
        }

        public Vector2 GetFocalPixelLength()
        {
            return new Vector2(1444.24768066f * resizeCoef, 1444.24768066f * resizeCoef);
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
            return new Vector2(req.Width * resizeCoef, req.Height * resizeCoef);
        }

        public bool IsCameraReady()
        {
            return FakeTextures[0] != null;
        }

        private void OnDestroy()
        {
            FreeBufferMemory();
        }

        /// <summary>
        /// Free all buffers
        /// </summary>
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

        /// <summary>
        /// Rotate FakeTexture and copy the red channel to green and blue
        /// </summary>
        private Texture2D Preprocess(Texture2D FakeTexture, TextureFormat format)
        {
            int w = FakeTexture.width;
            int h = FakeTexture.height;

            bool onlyFeatures = FindObjectOfType<VPSLocalisationService>().SendOnlyFeatures;
            if (onlyFeatures)
            {
                Texture2D rotatedTexture = new Texture2D(w, h, format, false);
                for (int i = 0; i < w; i++)
                {
                    for (int j = 0; j < h; j++)
                    {
                        Color pixel = FakeTexture.GetPixel(i, j);
                        pixel.g = 0;
                        pixel.b = 0;
                        rotatedTexture.SetPixel(i, j, pixel);
                    }
                }

                rotatedTexture.Apply();
                return rotatedTexture;
            }
            else
            {
                return FakeTexture;
            }
        }

        /// <summary>
        /// Set camera fov for correct rendering 
        /// </summary>
        private void SetCameraFov()
        {
            Camera camera = Camera.main;

            float h = cameraResolution.x;
            float fy = GetFocalPixelLength().y;

            float fovY = (float)(2 * Mathf.Atan(h / 2 / fy) * 180 / Mathf.PI);

            camera.fieldOfView = fovY;
        }

        public VPSOrientation GetOrientation()
        {
            return VPSOrientation.Portrait;
        }
    }
}
