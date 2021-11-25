using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace ARVRLab.VPSService
{
    public enum FakeTextureLoadingType { TEXTURE, IMAGE_PATH };
    /// <summary>
    /// Return FakeTexture image
    /// </summary>
    public class FakeCamera : MonoBehaviour, ICamera
    {
        private Vector2Int cameraResolution = new Vector2Int(1920, 1080);
        public FakeTextureLoadingType LoadingType;

        [Tooltip("Texture for sending")]
        public Texture2D FakeTexture;
        [Tooltip("Texture for sending")]
        public string ImageLocalPath;

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
            resizeCoef = (float)buffers.FirstOrDefault().Key.Width / (float)cameraResolution.y; 
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                if (LoadingType == FakeTextureLoadingType.TEXTURE)
                {
                    if (FakeTexture == null)
                        return;
                }
                else
                {
                    string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ImageLocalPath);
                    FakeTexture.LoadImage(File.ReadAllBytes(fullPath));
                }

                InitBuffers();

#if UNITY_EDITOR
                EditorApplication.delayCall = () => ShowMockFrame(FakeTexture);
                PrepareApplyer();
#endif
            }
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
                convertTexture = Preprocess(req.Format);
                if (convertTexture.width != req.Width || convertTexture.height != req.Height)
                {
                    RectInt inputRect = req.GetCropRect(convertTexture.width, convertTexture.height, ((float)req.Height) / ((float)req.Width));
                    convertTexture = CropScale.CropTexture(convertTexture, new Vector2(inputRect.height, inputRect.width), req.Format, CropOptions.CUSTOM, inputRect.x, inputRect.y);
                    convertTexture = CropScale.ScaleTexture(convertTexture, req.Width, req.Height, req.Format);
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
            return FakeTexture != null;
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

        /// <summary>
        /// Rotate FakeTexture and copy the red channel to green and blue
        /// </summary>
        private Texture2D Preprocess(TextureFormat format)
        {
            int w = FakeTexture.width;
            int h = FakeTexture.height;

            bool onlyFeatures = FindObjectOfType<VPSLocalisationService>().SendOnlyFeatures;
            Texture2D rotatedTexture = new Texture2D(w, h, format, false);
            if (onlyFeatures)
            {
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
            }
            else
            {
                rotatedTexture.SetPixels(FakeTexture.GetPixels());
                rotatedTexture.Apply();
            }

            return rotatedTexture;
        }

        /// <summary>
        /// Create mock frame on the background
        /// </summary>
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
                    VPSLogger.Log(LogLevel.ERROR, "Virtual camera is not found");
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