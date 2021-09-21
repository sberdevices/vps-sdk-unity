using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace ARVRLab.VPSService
{
    public enum VPSTextureType { LOCALISATION_TEXTURE, FEATURE_EXTRACTOR, IMAGE_ENCODER };

    public class VPSTextureRequirement
    {
        public VPSTextureType Type;
        public int Width;
        public int Height;
        public TextureFormat Format;

        public VPSTextureRequirement(VPSTextureType type, int width, int height, TextureFormat format)
        {
            Type = type;
            Width = width;
            Height = height;
            Format = format;
        }

        public XRCpuImage.ConversionParams GetConversionParams(XRCpuImage image)
        {
            XRCpuImage.ConversionParams conversionParams = new XRCpuImage.ConversionParams(image, Format);
            conversionParams.inputRect = GetCropRect(image.width, image.height, ((float)Height) / ((float)Width));
            conversionParams.outputDimensions = new Vector2Int(Width, Height);
            return conversionParams;
        }

        public RectInt GetCropRect(int width, int height, float cropCoefficient)
        {
            int requiredWidth = width;
            int requiredHeight = (int)(width * cropCoefficient);
            int xpos = 0;
            int ypos = 0;

            if (requiredHeight > height)
            {
                requiredHeight = height;
                requiredWidth = (int)(width * (1 / cropCoefficient));
                xpos = (width - requiredWidth) / 2;
            }
            else
            {
                ypos = (height - requiredHeight) / 2;
            }

            return new RectInt(xpos, ypos, requiredWidth, requiredHeight);
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                VPSTextureRequirement requir = (VPSTextureRequirement)obj;
                return (Width == requir.Width) && (Height == requir.Height) && (Format == requir.Format);
            }
        }
    }
}
