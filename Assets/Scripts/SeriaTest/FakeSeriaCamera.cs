using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class FakeSeriaCamera : MonoBehaviour, ICamera
    {
        [Tooltip("Target photo resolution")]
        private Vector2Int desiredResolution = new Vector2Int(960, 540);
        private TextureFormat format = TextureFormat.RGB24;

        public Texture2D[] FakeTextures;
        private Texture2D[] ppFakeTextures;
        private Texture2D convertTexture;

        // Не проверено
        private Dictionary<VPSTextureRequirement, NativeArray<byte>> buffers;

        private int Counter = 0;

        private float resizeCoefficient = 1.0f;
        private VPSTextureRequirement textureRequir;

        private void Awake()
        {
            textureRequir = new VPSTextureRequirement(desiredResolution.x, desiredResolution.y, format);
        }

        private void Start()
        {
            resizeCoefficient = (float)FakeTextures[0].width / (float)desiredResolution.x;
            ppFakeTextures = new Texture2D[FakeTextures.Length];

            for (int i = 0; i < FakeTextures.Length; i++)
            {
                ppFakeTextures[i] = Preprocess(FakeTextures[i], textureRequir.Format);
            }
        }

        public void Init(VPSTextureRequirement[] requirements)
        {
            FreeBufferMemory();

            var distinctRequir = requirements.Distinct().ToList();
            buffers = distinctRequir.ToDictionary(r => r, r => new NativeArray<byte>(r.Width * r.Height, Allocator.Persistent));

            InitBuffers();
        }

        private void InitBuffers()
        {
            if (buffers == null || buffers.Count == 0)
                return;

            var toCopy = buffers.Keys.Where(key => key.Equals(textureRequir));
            var toCreate = buffers.Keys.Except(toCopy);

            foreach (var req in toCopy)
            {
                buffers[req].CopyFrom(ppFakeTextures[Counter].GetRawTextureData());
            }

            foreach (var req in toCreate)
            {
                convertTexture = Preprocess(FakeTextures[Counter], req.Format);
                RectInt inputRect = req.GetCropRect(convertTexture.width, convertTexture.height, req.Width / req.Height);
                convertTexture = CropScale.CropTexture(convertTexture, new Vector2(inputRect.height, inputRect.width), CropOptions.CUSTOM, inputRect.x, inputRect.y);
                convertTexture = CropScale.ScaleTexture(convertTexture, req.Width, req.Height);
                buffers[req].CopyFrom(convertTexture.GetRawTextureData());
            }
        }

        public Vector2 GetFocalPixelLength()
        {
            return new Vector2(1396.5250f, 1396.5250f);
        }

        public Texture2D GetFrame()
        {
            Counter++;
            if (Counter > ppFakeTextures.Length)
            {
                Counter = 1;
            }
            return ppFakeTextures[Counter - 1];
        }

        public NativeArray<byte> GetBuffer(VPSTextureRequirement requir)
        {
            //Counter++;
            //if (Counter >= FakeTextures.Length)
            //    Counter = 1;
            //InitBuffers();
            //FreeBufferMemory();
            return buffers[requir];
        }

        public Vector2 GetPrincipalPoint()
        {
            return new Vector2(ppFakeTextures[0].width * 0.5f, ppFakeTextures[0].height * 0.5f);
        }

        public bool IsCameraReady()
        {
            return ppFakeTextures[0] != null;
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

        private Texture2D Preprocess(Texture2D FakeTexture, TextureFormat format)
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
    }
}
