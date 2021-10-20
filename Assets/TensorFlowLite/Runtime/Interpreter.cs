﻿/* Copyright 2018 The TensorFlow Authors. All Rights Reserved.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
==============================================================================*/
using System;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using TfLiteInterpreter = System.IntPtr;
using TfLiteInterpreterOptions = System.IntPtr;
using TfLiteModel = System.IntPtr;
using TfLiteTensor = System.IntPtr;
using TfLiteDelegate = System.IntPtr;
using UnityEngine;

namespace TensorFlowLite
{
    /// <summary>
    /// Simple C# bindings for the experimental TensorFlowLite C API.
    /// </summary>
    public class Interpreter : IDisposable
    {
        public struct TensorInfo
        {
            public string name { get; internal set; }
            public DataType type { get; internal set; }
            public int[] shape { get; internal set; }
            public QuantizationParams quantizationParams { get; internal set; }

            public override string ToString()
            {
                return string.Format("name: {0}, type: {1}, dimensions: {2}, quantizationParams: {3}",
                  name,
                  type,
                  "[" + string.Join(",", shape) + "]",
                  "{" + quantizationParams + "}");
            }
        }

        private TfLiteModel model = IntPtr.Zero;
        private TfLiteInterpreter interpreter = IntPtr.Zero;
        private InterpreterOptions options = null;

        public Interpreter(byte[] modelData) : this(modelData, null) { }

        public Interpreter(byte[] modelData, InterpreterOptions options)
        {
            GCHandle modelDataHandle = GCHandle.Alloc(modelData, GCHandleType.Pinned);
            IntPtr modelDataPtr = modelDataHandle.AddrOfPinnedObject();
            model = TfLiteModelCreate(modelDataPtr, modelData.Length);
            if (model == IntPtr.Zero) throw new Exception("Failed to create TensorFlowLite Model");

            this.options = options;

            interpreter = TfLiteInterpreterCreate(model, options.nativePtr);
            if (interpreter == IntPtr.Zero) throw new Exception("Failed to create TensorFlowLite Interpreter");
        }


        public void Dispose()
        {
            if (interpreter != IntPtr.Zero) TfLiteInterpreterDelete(interpreter);
            interpreter = IntPtr.Zero;

            if (model != IntPtr.Zero) TfLiteModelDelete(model);
            model = IntPtr.Zero;

            if (options != null) options.Dispose();
        }

        public void Invoke()
        {
            ThrowIfError(TfLiteInterpreterInvoke(interpreter));
        }

        public int GetInputTensorCount()
        {
            return TfLiteInterpreterGetInputTensorCount(interpreter);
        }

        public void SetInputTensorData(int inputTensorIndex, Array inputTensorData)
        {
            GCHandle tensorDataHandle = GCHandle.Alloc(inputTensorData, GCHandleType.Pinned);
            IntPtr tensorDataPtr = tensorDataHandle.AddrOfPinnedObject();
            TfLiteTensor tensor = TfLiteInterpreterGetInputTensor(interpreter, inputTensorIndex);
            ThrowIfError(TfLiteTensorCopyFromBuffer(
                tensor, tensorDataPtr, Buffer.ByteLength(inputTensorData)));
        }

        public unsafe void SetInputTensorData<T>(int inputTensorIndex, NativeArray<T> inputTensorData) where T : struct
        {
            IntPtr tensorDataPtr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(inputTensorData);
            TfLiteTensor tensor = TfLiteInterpreterGetInputTensor(interpreter, inputTensorIndex);
            ThrowIfError(TfLiteTensorCopyFromBuffer(
                tensor, tensorDataPtr, inputTensorData.Length * UnsafeUtility.SizeOf<T>()));
        }

        public void ResizeInputTensor(int inputTensorIndex, int[] inputTensorShape)
        {
            ThrowIfError(TfLiteInterpreterResizeInputTensor(
                interpreter, inputTensorIndex, inputTensorShape, inputTensorShape.Length));
        }

        public void AllocateTensors()
        {
            ThrowIfError(TfLiteInterpreterAllocateTensors(interpreter));
        }

        public int GetOutputTensorCount()
        {
            return TfLiteInterpreterGetOutputTensorCount(interpreter);
        }

        public void GetOutputTensorData(int outputTensorIndex, Array outputTensorData)
        {
            GCHandle tensorDataHandle = GCHandle.Alloc(outputTensorData, GCHandleType.Pinned);
            IntPtr tensorDataPtr = tensorDataHandle.AddrOfPinnedObject();
            TfLiteTensor tensor = TfLiteInterpreterGetOutputTensor(interpreter, outputTensorIndex);
            ThrowIfError(TfLiteTensorCopyToBuffer(
                tensor, tensorDataPtr, Buffer.ByteLength(outputTensorData)));
        }

        public TensorInfo GetInputTensorInfo(int index)
        {
            TfLiteTensor tensor = TfLiteInterpreterGetInputTensor(interpreter, index);
            return GetTensorInfo(tensor);
        }

        public TensorInfo GetOutputTensorInfo(int index)
        {
            TfLiteTensor tensor = TfLiteInterpreterGetOutputTensor(interpreter, index);
            return GetTensorInfo(tensor);
        }

        /// <summary>
        /// Returns a string describing version information of the TensorFlow Lite library.
        /// TensorFlow Lite uses semantic versioning.
        /// </summary>
        /// <returns>A string describing version information</returns>
        public static string GetVersion()
        {
            return Marshal.PtrToStringAnsi(TfLiteVersion());
        }

        private static string GetTensorName(TfLiteTensor tensor)
        {
            return Marshal.PtrToStringAnsi(TfLiteTensorName(tensor));
        }

        private static TensorInfo GetTensorInfo(TfLiteTensor tensor)
        {
            int[] dimensions = new int[TfLiteTensorNumDims(tensor)];
            for (int i = 0; i < dimensions.Length; i++)
            {
                dimensions[i] = TfLiteTensorDim(tensor, i);
            }
            return new TensorInfo()
            {
                name = GetTensorName(tensor),
                type = TfLiteTensorType(tensor),
                shape = dimensions,
                quantizationParams = TfLiteTensorQuantizationParams(tensor),
            };
        }

        private static void ThrowIfError(Status status)
        {
            if (status == Status.Error) throw new Exception("TensorFlowLite operation failed.");
            if (status == Status.DelegateError) throw new Exception("TensorFlowLite delegage operation failed.");
        }

        #region Externs

#if UNITY_IOS && !UNITY_EDITOR
        private const string TensorFlowLibrary = "__Internal";
#else
        private const string TensorFlowLibrary = "libtensorflowlite_c";
#endif

        // TfLiteStatus
        public enum Status
        {
            Ok = 0,
            Error = 1,
            DelegateError = 2
        }

        // TfLiteType
        public enum DataType
        {
            NoType = 0,
            Float32 = 1,
            Int32 = 2,
            UInt8 = 3,
            Int64 = 4,
            String = 5,
            Bool = 6,
            Int16 = 7,
            Complex64 = 8,
            Int8 = 9,
            Float16 = 10,
            Float64 = 11,
        }

        public struct QuantizationParams
        {
            public float scale;
            public int zeroPoint;

            public override string ToString()
            {
                return string.Format("scale: {0} zeroPoint: {1}", scale, zeroPoint);
            }
        }

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe IntPtr TfLiteVersion();

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe TfLiteInterpreter TfLiteModelCreate(IntPtr model_data, int model_size);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe void TfLiteModelDelete(TfLiteModel model);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe TfLiteInterpreter TfLiteInterpreterCreate(
            TfLiteModel model,
            TfLiteInterpreterOptions optional_options);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe void TfLiteInterpreterDelete(TfLiteInterpreter interpreter);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe int TfLiteInterpreterGetInputTensorCount(
            TfLiteInterpreter interpreter);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe TfLiteTensor TfLiteInterpreterGetInputTensor(
            TfLiteInterpreter interpreter,
            int input_index);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe Status TfLiteInterpreterResizeInputTensor(
            TfLiteInterpreter interpreter,
            int input_index,
            int[] input_dims,
            int input_dims_size);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe Status TfLiteInterpreterAllocateTensors(
            TfLiteInterpreter interpreter);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe Status TfLiteInterpreterInvoke(TfLiteInterpreter interpreter);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe int TfLiteInterpreterGetOutputTensorCount(
            TfLiteInterpreter interpreter);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe TfLiteTensor TfLiteInterpreterGetOutputTensor(
            TfLiteInterpreter interpreter,
            int output_index);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe DataType TfLiteTensorType(TfLiteTensor tensor);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe int TfLiteTensorNumDims(TfLiteTensor tensor);

        [DllImport(TensorFlowLibrary)]
        private static extern int TfLiteTensorDim(TfLiteTensor tensor, int dim_index);

        [DllImport(TensorFlowLibrary)]
        private static extern uint TfLiteTensorByteSize(TfLiteTensor tensor);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe IntPtr TfLiteTensorName(TfLiteTensor tensor);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe QuantizationParams TfLiteTensorQuantizationParams(TfLiteTensor tensor);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe Status TfLiteTensorCopyFromBuffer(
            TfLiteTensor tensor,
            IntPtr input_data,
            int input_data_size);

        [DllImport(TensorFlowLibrary)]
        private static extern unsafe Status TfLiteTensorCopyToBuffer(
            TfLiteTensor tensor,
            IntPtr output_data,
            int output_data_size);

        #endregion
    }
}
