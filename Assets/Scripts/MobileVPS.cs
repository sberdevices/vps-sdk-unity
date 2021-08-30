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
        //private const string FileName = "hfnet_i8_960.tflite";

        private const string gbNeuronFileName = "mnv_0.5_mask_teacher_gray_32.tflite";
        private const string hfnetNeuronFileName = "hfnet_f32_960_sp.tflite";

        Interpreter gbInterpreter;
        Interpreter hfnetInterpreter;

        private float[,,] input;
        private HfnetResult hfnetResult;
        private GbResult gbResult;

        private int width, height;

        private CancellationTokenSource tokenSource;
        private CancellationToken cancelToken;

        public bool GbIsWorking = false;
        public bool HfnetIsWorking = false;

        public MobileVPS()
        {
            var options = new InterpreterOptions
            {
                threads = 2
            };

            var other = new InterpreterOptions
            {
                threads = 2
            };

            gbInterpreter = new Interpreter(FileUtil.LoadFile(gbNeuronFileName), options);
            gbInterpreter.AllocateTensors();

            hfnetInterpreter = new Interpreter(FileUtil.LoadFile(hfnetNeuronFileName), other);
            hfnetInterpreter.AllocateTensors();

            int[] idim0 = gbInterpreter.GetInputTensorInfo(0).shape;
            height = idim0[1]; // 960
            width = idim0[2]; // 540
            int channels = idim0[3]; //1

            input = new float[height, width, channels];
            hfnetResult = new HfnetResult();
            gbResult = new GbResult();
        }

        ~MobileVPS()
        {
            gbInterpreter?.Dispose();
            hfnetInterpreter?.Dispose();
        }

        public void StopTask()
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
        }

        public async Task<bool> StartPreprocess(NativeArray<byte> buffer)
        {
            if (tokenSource != null)
            {
                tokenSource.Dispose();
            }
            tokenSource = new CancellationTokenSource();
            cancelToken = tokenSource.Token;

            return await Task.Run(() => Preprocess(buffer), cancelToken);
        }

        public async Task<HfnetResult> GetFeaturesAsync()
        {
            return await Task.Run(() => GetFeatures());
        }

        public async Task<GbResult> GetGlobalDescriptorAsync()
        {
            return await Task.Run(() => GetGlobalDescriptor());
        }

        private bool Preprocess(NativeArray<byte> buffer)
        {
            // for input - convert bytes to float, rotate and mirror image
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    input[height - j - 1, width - i - 1, 0] = (float)(buffer[((i + 1) * height - j - 1)]);
                }

                if (cancelToken.IsCancellationRequested)
                {
                    Debug.LogError("Mobile VPS task canceled");
                    return false;
                }
            }

            return true;
        }

        private HfnetResult GetFeatures()
        {
            hfnetInterpreter.SetInputTensorData(0, input);
            if (cancelToken.IsCancellationRequested)
            {
                Debug.LogError("Mobile VPS task canceled");
                return null;
            }
            else
            {
                HfnetIsWorking = true;
            }
            //System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            //stopWatch.Start();
            hfnetInterpreter.Invoke();

            //stopWatch.Stop();
            //// Get the elapsed time as a TimeSpan value.
            //TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            //string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            //    ts.Hours, ts.Minutes, ts.Seconds,
            //    ts.Milliseconds / 10);
            //Debug.Log("RunTime " + elapsedTime);

            float[,] keyPoints = new float[400, 2];
            hfnetInterpreter.GetOutputTensorData(0, keyPoints);

            float[,] descriptors = new float[400, 256];
            hfnetInterpreter.GetOutputTensorData(1, descriptors);

            float[] scores = new float[400];
            hfnetInterpreter.GetOutputTensorData(2, scores);

            //stopWatch.Restart();

            hfnetResult.setKeyPoints(keyPoints);
            hfnetResult.setDescriptors(descriptors);
            hfnetResult.setScores(scores);

            //stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            //TimeSpan ts1 = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            //string elapsedTime1 = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            //    ts1.Hours, ts1.Minutes, ts1.Seconds,
            //    ts1.Milliseconds);
            //Debug.Log("PostProcessTime " + elapsedTime1);

            HfnetIsWorking = false;
            return hfnetResult;
        }

        private GbResult GetGlobalDescriptor()
        {
            gbInterpreter.SetInputTensorData(0, input);
            if (cancelToken.IsCancellationRequested)
            {
                Debug.LogError("Mobile VPS task canceled");
                return null;
            }
            else
            {
                GbIsWorking = true;
            }
            //System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            //stopWatch.Start();
            gbInterpreter.Invoke();

            //stopWatch.Stop();
            //// Get the elapsed time as a TimeSpan value.
            //TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            //string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            //    ts.Hours, ts.Minutes, ts.Seconds,
            //    ts.Milliseconds / 10);
            //Debug.Log("RunTime " + elapsedTime);

            float[] globalDescriptor = new float[4096];
            gbInterpreter.GetOutputTensorData(0, globalDescriptor);

            //stopWatch.Restart();

            gbResult.setGlobalDescriptor(globalDescriptor);

            //stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            //TimeSpan ts1 = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            //string elapsedTime1 = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            //    ts1.Hours, ts1.Minutes, ts1.Seconds,
            //    ts1.Milliseconds);
            //Debug.Log("PostProcessTime " + elapsedTime1);

            GbIsWorking = false;
            return gbResult;
        }
    }

    public class HfnetResult
    {
        public byte[] keyPoints;
        public byte[] descriptors;
        public byte[] scores;

        public HfnetResult()
        {
            keyPoints = new byte[400 * 2 * 2];
            descriptors = new byte[400 * 256 * 2];
            scores = new byte[400 * 2];
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

    public class GbResult
    {
        public byte[] globalDescriptor;

        public GbResult()
        {
            globalDescriptor = new byte[4096 * 2];
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
    }
}
