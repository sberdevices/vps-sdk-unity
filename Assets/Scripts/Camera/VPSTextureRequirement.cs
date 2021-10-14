using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace ARVRLab.VPSService
{
    public class VPSTextureRequirement
    {
        public int Width;
        public int Height;
        public TextureFormat Format;

        public VPSTextureRequirement(int width, int height, TextureFormat format)
        {
            Width = width;
            Height = height;
            Format = format;
        }

        /// <summary>
        /// Create ConversionParams to convert XRCpuImage to Texture2D
        /// </summary>
        public XRCpuImage.ConversionParams GetConversionParams(XRCpuImage image)
        {
            XRCpuImage.ConversionParams conversionParams = new XRCpuImage.ConversionParams(image, Format);
            conversionParams.inputRect = GetCropRect(image.width, image.height, ((float)Height) / ((float)Width));
            conversionParams.outputDimensions = new Vector2Int(Width, Height);
            return conversionParams;
        }

        /// <summary>
        /// Calculate rect in the center of the image to crop
        /// </summary>
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

        /// <summary>
        /// Convert TextureFormat to channels count
        /// </summary>
        public int ChannelsCount()
        {
            if (Format == TextureFormat.R8)
                return 1;
            if (Format == TextureFormat.RGB24)
                return 3;

            return -1;
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

        public override int GetHashCode()
        {
            string hashstring = "";
            hashstring += Width.GetHashCode().ToString();
            hashstring += Height.GetHashCode().ToString();
            hashstring += Format.GetHashCode().ToString();

            return hashstring.GetHashCode();
        }
    }
}
