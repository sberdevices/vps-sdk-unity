using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public static class EMBDCollector
    {
        public static byte[] ConvertToEMBD(byte fileVersion, byte neuralId, byte[] keyPoints, byte[] scores, byte[] descriptors, byte[] globalDescriptor)
        {
            List<byte> embd = new List<byte>();

            embd.Add(fileVersion);
            embd.Add(neuralId);

            byte[] keyPointsLength = BitConverter.GetBytes(keyPoints.Length);
            byte[] scoresLength = BitConverter.GetBytes(scores.Length);
            byte[] descriptorsLength = BitConverter.GetBytes(descriptors.Length);
            byte[] globalDescriptorLength = BitConverter.GetBytes(globalDescriptor.Length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(keyPointsLength);
                Array.Reverse(scoresLength);
                Array.Reverse(descriptorsLength);
                Array.Reverse(globalDescriptorLength);
            }

            embd.AddRange(keyPointsLength);
            embd.AddRange(keyPoints);

            embd.AddRange(scoresLength);
            embd.AddRange(scores);

            embd.AddRange(descriptorsLength);
            embd.AddRange(descriptors);

            embd.AddRange(globalDescriptorLength);
            embd.AddRange(globalDescriptor);

            return embd.ToArray();
        }
    }
}