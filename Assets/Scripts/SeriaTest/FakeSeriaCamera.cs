using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public class FakeSeriaCamera : MonoBehaviour, ICamera
    {
        [Tooltip("Target photo resolution")]
        private Vector2Int desiredResolution = new Vector2Int(960, 540);

        public Texture2D[] FakeTextures;

        // Не проверено
        private Texture2D imageFeatureExtractorTexture;
        private Texture2D imageEncoderTexture;

        private int Counter = 0;

        private float resizeCoefficient = 1.0f;

        private VPSTextureRequirement feautureExtractorRequirement;
        private VPSTextureRequirement encoderRequirement;

        private void Start()
        {
            resizeCoefficient = desiredResolution.x / FakeTextures[0].width;

            for (int i = 0; i < FakeTextures.Length; i++)
            {
                FakeTextures[i] = Preprocess(FakeTextures[i]);
            }
        }

        public void Init(VPSTextureRequirement FeautureExtractorRequirement, VPSTextureRequirement EncoderRequirement)
        {
            feautureExtractorRequirement = FeautureExtractorRequirement;
            encoderRequirement = EncoderRequirement;

            imageFeatureExtractorTexture = new Texture2D(1, 1);
            imageEncoderTexture = new Texture2D(1, 1);
            InitBuffers();
        }

        private void InitBuffers()
        {
            if (feautureExtractorRequirement == null || encoderRequirement == null)
                return;

            RectInt inputRect = feautureExtractorRequirement.GetCropRect(FakeTextures[Counter].width, FakeTextures[Counter].height, feautureExtractorRequirement.Width / feautureExtractorRequirement.Height);
            imageFeatureExtractorTexture = CropScale.CropTexture(FakeTextures[Counter], new Vector2(inputRect.height, inputRect.width), CropOptions.CUSTOM, inputRect.x, inputRect.y);
            imageFeatureExtractorTexture = CropScale.ScaleTexture(imageFeatureExtractorTexture, feautureExtractorRequirement.Width, feautureExtractorRequirement.Height);

            if (feautureExtractorRequirement.Equals(encoderRequirement))
            {
                imageEncoderTexture = imageFeatureExtractorTexture;
            }
            else
            {
                inputRect = encoderRequirement.GetCropRect(FakeTextures[Counter].width, FakeTextures[Counter].height, encoderRequirement.Width / encoderRequirement.Height);
                imageEncoderTexture = CropScale.CropTexture(FakeTextures[Counter], new Vector2(inputRect.height, inputRect.width), CropOptions.CUSTOM, inputRect.x, inputRect.y);
                imageEncoderTexture = CropScale.ScaleTexture(imageEncoderTexture, encoderRequirement.Width, encoderRequirement.Height);
            }
        }

        public Vector2 GetFocalPixelLength()
        {
            return new Vector2(1396.5250f, 1396.5250f);
        }

        public Texture2D GetFrame()
        {
            Counter++;
            if (Counter >= 3)
                Counter = 1;
            return FakeTextures[Counter - 1];
        }

        public NativeArray<byte> GetImageFeatureExtractorBuffer()
        {
            Counter++;
            if (Counter >= FakeTextures.Length)
                Counter = 1;
            InitBuffers();
            return imageFeatureExtractorTexture.GetRawTextureData<byte>();
        }

        public NativeArray<byte> GetImageEncoderBuffer()
        {
            return imageEncoderTexture.GetRawTextureData<byte>();
        }

        public Vector2 GetPrincipalPoint()
        {
            return new Vector2(FakeTextures[0].width * 0.5f, FakeTextures[0].height * 0.5f);
        }

        public bool IsCameraReady()
        {
            return FakeTextures[0] != null;
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

        private Texture2D Preprocess(Texture2D FakeTexture)
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

            return rotatedTexture;
        }
    }
}
