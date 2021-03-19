using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public static class EMBDCollector
    {
        public static byte[] ConvertToEMBD(byte fileVersion, byte neuralId, float[,] keyPoints, float[] scores, float[,] descriptors, float[] globalDescriptor)
        {
            List<byte> embd = new List<byte>();

            embd.Add(fileVersion);
            embd.Add(neuralId);

            string keyPointsString = Convert.ToBase64String(ConvertFloatToByteArray(keyPoints));
            string scoresString = Convert.ToBase64String(ConvertFloatToByteArray(scores));
            string descriptorsString = Convert.ToBase64String(ConvertFloatToByteArray(descriptors));
            string globalDescriptorString = Convert.ToBase64String(ConvertFloatToByteArray(globalDescriptor));

            byte[] bytes;

            bytes = BitConverter.GetBytes(keyPointsString.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            embd.AddRange(bytes);
            embd.AddRange(Encoding.ASCII.GetBytes(keyPointsString));

            bytes = BitConverter.GetBytes(scoresString.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            embd.AddRange(bytes);
            embd.AddRange(Encoding.ASCII.GetBytes(scoresString));

            bytes = BitConverter.GetBytes(descriptorsString.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            embd.AddRange(bytes);
            embd.AddRange(Encoding.ASCII.GetBytes(descriptorsString));

            bytes = BitConverter.GetBytes(globalDescriptorString.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            embd.AddRange(bytes);
            embd.AddRange(Encoding.ASCII.GetBytes(globalDescriptorString));

            return embd.ToArray();
        }

        /// <summary>
        /// Pack mobileVPS result to byte array
        /// </summary>
        private static byte[] ConvertFloatToByteArray(float[] floats)
        {
            var byteArray = new byte[floats.Length * 4];
            Buffer.BlockCopy(floats, 0, byteArray, 0, byteArray.Length);

            return byteArray;
        }

        /// <summary>
        /// Pack mobileVPS result to byte array
        /// </summary>
        private static byte[] ConvertFloatToByteArray(float[,] floats)
        {
            var byteArray = new byte[floats.Length * 4];
            Buffer.BlockCopy(floats, 0, byteArray, 0, byteArray.Length);

            return byteArray;
        }
    }
}