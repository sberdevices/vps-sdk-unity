using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SystemHalf;
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

        public bool Working = false;

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
            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
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
            // for input - convert bytes to float, rotate and mirror image
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        Debug.LogError("Mobile VPS task canceled");
                        return null;
                    }

                    input[height - j - 1, width - i - 1, 0] = (float)(buffer[((i + 1) * height - j - 1)]);
                }
            }
            interpreter.SetInputTensorData(0, input);
            if (cancelToken.IsCancellationRequested)
            {
                Debug.LogError("Mobile VPS task canceled");
                return null;
            }
            else
            {
                Working = true;
            }
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            interpreter.Invoke();

            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Debug.Log("RunTime " + elapsedTime);

            float[] globalDescriptor = new float[4096];
            interpreter.GetOutputTensorData(0, globalDescriptor);

            float[,] keyPoints = new float[400, 2];
            interpreter.GetOutputTensorData(1, keyPoints);

            float[,] descriptors = new float[400, 256];
            interpreter.GetOutputTensorData(2, descriptors);

            float[] scores = new float[400];
            interpreter.GetOutputTensorData(3, scores);

            stopWatch.Restart();

            output.setGlobalDescriptor(globalDescriptor);

            output.setKeyPoints(keyPoints);

            output.setDescriptors(descriptors);

            output.setScores(scores);

            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts1 = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime1 = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts1.Hours, ts1.Minutes, ts1.Seconds,
                ts1.Milliseconds / 10);
            Debug.Log("GovnoTime " + elapsedTime1);

            Working = false;
            return output;
        }
    }

    public class HfnetResult
    {
        public byte[] globalDescriptor;
        public byte[] keyPoints;
        public byte[] descriptors;
        public byte[] scores;

        public HfnetResult()
        {
            globalDescriptor = new byte[4096 * 2];
            keyPoints = new byte[400 * 2 * 2];
            descriptors = new byte[400 * 256 * 2];
            scores = new byte[400 * 2];
        }

        public void setGlobalDescriptor(float[] globDesc)
        {
            for (int i = 0; i < 4096 * 2; i += 2)
            {
                byte[] gd = float16.GetBytes(new float16(globDesc[i / 2]));
                globalDescriptor[i] = gd[0];
                globalDescriptor[i+1] = gd[1];
            }
        }

        public void setKeyPoints(float[,] points)
        {
            for (int i = 0; i < 400; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    byte[] kp = float16.GetBytes(new float16(points[i,j]));
                    keyPoints[i * 2 * 2 + j * 2] = kp[0];
                    keyPoints[i * 2 * 2 + j * 2 + 1] = kp[1];
                }
            }
        }

        public void setDescriptors(float[,] descs)
        {
            for (int i = 0; i < 400; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    byte[] d = float16.GetBytes(new float16(descs[i, j]));
                    descriptors[i * 256 * 2 + j * 2] = d[0];
                    descriptors[i * 256 * 2 + j * 2 + 1] = d[1];
                }
            }
        }


        public void setScores(float[] scrs)
        {
            for (int i = 0; i < 400 * 2; i += 2)
            {
                byte[] s = float16.GetBytes(new float16(scrs[i / 2]));
                scores[i] = s[0];
                scores[i + 1] = s[1];
            }
        }
    }
}
