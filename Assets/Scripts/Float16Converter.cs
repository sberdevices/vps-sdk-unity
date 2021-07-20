using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public static class Float16Converter
{
    private static readonly ushort[] BaseTable = GenerateBaseTable();
    private static readonly sbyte[] ShiftTable = GenerateShiftTable();

    public static unsafe byte[] SingleToHalf(float single)
    {
        uint value = *(uint*)&single;

        ushort result = (ushort)(BaseTable[(value >> 23) & 0x1ff] + ((value & 0x007fffff) >> ShiftTable[value >> 23]));
        return BitConverter.GetBytes(result);
    }

    public static unsafe byte[] SigleArrayToHalfArray(float[] single)
    {
        NativeArray<float> floats = new NativeArray<float>(single, Allocator.TempJob);
        NativeArray<byte> bytes = new NativeArray<byte>(single.Length * 2, Allocator.TempJob);
        ParallelConvertJob job = new ParallelConvertJob()
        {
            floats = floats,
            bytes = bytes
        };

        JobHandle handle = job.Schedule(single.Length, 32);
        handle.Complete();

        if (handle.IsCompleted)
        {
            byte[] result = job.bytes.ToArray();
            floats.Dispose();
            bytes.Dispose();
            return result;
        }

        return null;
    }

    private struct ParallelConvertJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<float> floats;
        [NativeDisableParallelForRestriction]
        public NativeArray<byte> bytes;

        public void Execute(int i)
        {
            byte[] b = SingleToHalf(floats[i]);
            bytes[i * 2] = b[0];
            bytes[i * 2 + 1] = b[1];
        }
    }

    private static ushort[] GenerateBaseTable()
    {
        ushort[] baseTable = new ushort[512];
        for (int i = 0; i < 256; ++i)
        {
            sbyte e = (sbyte)(127 - i);
            if (e > 24)
            { // Very small numbers map to zero
                baseTable[i | 0x000] = 0x0000;
                baseTable[i | 0x100] = 0x8000;
            }
            else if (e > 14)
            { // Small numbers map to denorms
                baseTable[i | 0x000] = (ushort)(0x0400 >> (18 + e));
                baseTable[i | 0x100] = (ushort)((0x0400 >> (18 + e)) | 0x8000);
            }
            else if (e >= -15)
            { // Normal numbers just lose precision
                baseTable[i | 0x000] = (ushort)((15 - e) << 10);
                baseTable[i | 0x100] = (ushort)(((15 - e) << 10) | 0x8000);
            }
            else if (e > -128)
            { // Large numbers map to Infinity
                baseTable[i | 0x000] = 0x7c00;
                baseTable[i | 0x100] = 0xfc00;
            }
            else
            { // Infinity and NaN's stay Infinity and NaN's
                baseTable[i | 0x000] = 0x7c00;
                baseTable[i | 0x100] = 0xfc00;
            }
        }

        return baseTable;
    }

    private static sbyte[] GenerateShiftTable()
    {
        sbyte[] shiftTable = new sbyte[512];
        for (int i = 0; i < 256; ++i)
        {
            sbyte e = (sbyte)(127 - i);
            if (e > 24)
            { // Very small numbers map to zero
                shiftTable[i | 0x000] = 24;
                shiftTable[i | 0x100] = 24;
            }
            else if (e > 14)
            { // Small numbers map to denorms
                shiftTable[i | 0x000] = (sbyte)(e - 1);
                shiftTable[i | 0x100] = (sbyte)(e - 1);
            }
            else if (e >= -15)
            { // Normal numbers just lose precision
                shiftTable[i | 0x000] = 13;
                shiftTable[i | 0x100] = 13;
            }
            else if (e > -128)
            { // Large numbers map to Infinity
                shiftTable[i | 0x000] = 24;
                shiftTable[i | 0x100] = 24;
            }
            else
            { // Infinity and NaN's stay Infinity and NaN's
                shiftTable[i | 0x000] = 13;
                shiftTable[i | 0x100] = 13;
            }
        }

        return shiftTable;
    }
}
