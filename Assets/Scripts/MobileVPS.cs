using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TensorFlowLite;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ARVRLab.VPSService
{
    public class MobileVPS
    {
        private const string FileName = "hfnet_i8_960.tflite";

        Interpreter interpreter;

        private float[,,] input;
        private HfnetResult output;

        private int width, height;

        private CancellationTokenSource tokenSource;
        private CancellationToken cancelToken;

        public MobileVPS()
        {
            var options = new InterpreterOptions
            {
                threads = 2,
            };

            interpreter = new Interpreter(FileUtil.LoadFile(FileName), options);
            interpreter.AllocateTensors();

            int[] idim0 = interpreter.GetInputTensorInfo(0).shape;
            height = idim0[1]; // 960
            width = idim0[2]; // 540
            int channels = idim0[3]; //1

            input = new float[height, width, channels];
            output = new HfnetResult();
        }

        ~MobileVPS()
        {
            interpreter?.Dispose();
        }

        public void StopTask()
        {
            tokenSource.Cancel();
        }

        public async Task<HfnetResult> GetFeaturesAsync(NativeArray<byte> buffer)
        {
            if (tokenSource != null)
            {
                tokenSource.Dispose();
            }
            tokenSource = new CancellationTokenSource();
            cancelToken = tokenSource.Token;

            return await Task.Run(() => doInference(buffer), cancelToken);
        }

        private HfnetResult doInference(NativeArray<byte> buffer)
        {
            // заполняем input - преобразуем байты во флоат, поворачиваем и зеркалим изображение
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        Debug.Log("Mobile VPS task canceled");
                        return null;
                    }

                    //против часовой - работает (хотя по логики так фичи вверх ногами)
                    input[height - j - 1, width - i - 1, 0] = (float)(buffer[((i + 1) * height - j - 1)]);
                    //по часовой - по логике так, но не работает
                    //input0[j, i, 0] = (float)(buffer[((i + 1) * height - j - 1)]);
                }
            }

            interpreter.SetInputTensorData(0, input);

            interpreter.Invoke();

            interpreter.GetOutputTensorData(0, output.globalDescriptor);
            interpreter.GetOutputTensorData(1, output.keyPoints);
            interpreter.GetOutputTensorData(2, output.descriptors);
            interpreter.GetOutputTensorData(3, output.scores);

            return output;
        }
    }

    public class HfnetResult
    {
        public float[] globalDescriptor;
        public float[,] keyPoints;
        public float[,] descriptors;
        public float[] scores;

        public HfnetResult()
        {
            globalDescriptor = new float[4096];
            keyPoints = new float[400, 2];
            descriptors = new float[400, 256];
            scores = new float[400];
        }
    }
}
