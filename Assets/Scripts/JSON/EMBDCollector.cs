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

            embd.AddRange(BitConverter.GetBytes(keyPointsString.Length));
            embd.AddRange(Encoding.ASCII.GetBytes(keyPointsString));

            embd.AddRange(BitConverter.GetBytes(scoresString.Length));
            embd.AddRange(Encoding.ASCII.GetBytes(scoresString));

            embd.AddRange(BitConverter.GetBytes(descriptorsString.Length));
            embd.AddRange(Encoding.ASCII.GetBytes(descriptorsString));

            embd.AddRange(BitConverter.GetBytes(globalDescriptorString.Length));
            embd.AddRange(Encoding.ASCII.GetBytes(globalDescriptorString));

            return embd.ToArray();
        }

        /// <summary>
        /// Переводит результаты работы нейронки в массив байт
        /// </summary>
        private static byte[] ConvertFloatToByteArray(float[] floats)
        {
            var byteArray = new byte[floats.Length * 4];
            Buffer.BlockCopy(floats, 0, byteArray, 0, byteArray.Length);

            return byteArray;
        }

        /// <summary>
        /// Переводит результаты работы нейронки в массив байт
        /// </summary>
        private static byte[] ConvertFloatToByteArray(float[,] floats)
        {
            var byteArray = new byte[floats.Length * 4];
            Buffer.BlockCopy(floats, 0, byteArray, 0, byteArray.Length);

            return byteArray;
        }
    }
}