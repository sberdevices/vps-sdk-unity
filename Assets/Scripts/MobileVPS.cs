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
                        VPSLogger.Log(LogLevel.DEBUG, "Mobile VPS task canceled");
                        return null;
                    }

                    input[height - j - 1, width - i - 1, 0] = (float)(buffer[((i + 1) * height - j - 1)]);
                }
            }
            interpreter.SetInputTensorData(0, input);
            if (cancelToken.IsCancellationRequested)
            {
                VPSLogger.Log(LogLevel.DEBUG, "Mobile VPS task canceled");
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
            TimeSpan neuronRunTS = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string neuronRunTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                neuronRunTS.Hours, neuronRunTS.Minutes, neuronRunTS.Seconds,
                neuronRunTS.Milliseconds / 10);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "RunTime {0}", neuronRunTime);

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
            TimeSpan postProcessTS = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string postProcessTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                postProcessTS.Hours, postProcessTS.Minutes, postProcessTS.Seconds,
                postProcessTS.Milliseconds);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "PostProcessTime {0}", postProcessTime);

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
            for (int i = 0; i < 4096; i++)
            {
                byte[] gd = BitConverter.GetBytes(Mathf.FloatToHalf(globDesc[i]));
                globalDescriptor[i * 2] = gd[0];
                globalDescriptor[i * 2 + 1] = gd[1];
            }
        }

        public void setKeyPoints(float[,] points)
        {
            for (int i = 0; i < 400; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    byte[] kp = BitConverter.GetBytes(Mathf.FloatToHalf(points[i, j]));
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
                    byte[] d = BitConverter.GetBytes(Mathf.FloatToHalf(descs[i, j]));
                    descriptors[i * 256 * 2 + j * 2] = d[0];
                    descriptors[i * 256 * 2 + j * 2 + 1] = d[1];
                }
            }
        }


        public void setScores(float[] scrs)
        {
            for (int i = 0; i < 400; i++)
            {
                byte[] s = BitConverter.GetBytes(Mathf.FloatToHalf(scrs[i]));
                scores[i * 2] = s[0];
                scores[i * 2 + 1] = s[1];
            }
        }
    }
}
