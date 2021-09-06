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
        private const string ImageFeatureExtractorFileName = "msp_960x540x1_256_400.tflite";
        private const string imageEncoderFileName = "mnv_960x540x1_4096.tflite";

        Interpreter imageFeatureExtractorInterpreter;
        Interpreter imageEncoderInterpreter;

        private float[,,] input;
        private ImageFeatureExtractorResult imageFeatureExtractorResult;
        private ImageEncoderResult imageEncoderResult;

        private int width, height;

        private CancellationTokenSource tokenSource;
        private CancellationToken cancelToken;

        public bool ImageFeatureExtractorIsWorking = false;
        public bool ImageEncoderIsWorking = false;

        public MobileVPS()
        {
            var imageFeatureExtractorOptions = new InterpreterOptions
            {
                threads = 2
            };
            imageFeatureExtractorOptions.AddGpuDelegate();

            var imageEncoderOptions = new InterpreterOptions
            {
                threads = 2
            };

            imageFeatureExtractorInterpreter = new Interpreter(FileUtil.LoadFile(ImageFeatureExtractorFileName), imageFeatureExtractorOptions);
            imageFeatureExtractorInterpreter.AllocateTensors();

            imageEncoderInterpreter = new Interpreter(FileUtil.LoadFile(imageEncoderFileName), imageEncoderOptions);
            imageEncoderInterpreter.AllocateTensors();

            int[] idim0 = imageEncoderInterpreter.GetInputTensorInfo(0).shape;
            height = idim0[1]; // 960
            width = idim0[2]; // 540
            int channels = idim0[3]; //1

            input = new float[height, width, channels];
            imageFeatureExtractorResult = new ImageFeatureExtractorResult();
            imageEncoderResult = new ImageEncoderResult();
        }

        ~MobileVPS()
        {
            imageFeatureExtractorInterpreter?.Dispose();
            imageEncoderInterpreter?.Dispose();
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

        public async Task<ImageFeatureExtractorResult> GetFeaturesAsync()
        {
            return await Task.Run(() => GetFeatures());
        }

        public async Task<ImageEncoderResult> GetGlobalDescriptorAsync()
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

        private ImageFeatureExtractorResult GetFeatures()
        {
            imageFeatureExtractorInterpreter.SetInputTensorData(0, input);
            if (cancelToken.IsCancellationRequested)
            {
                Debug.LogError("Mobile VPS task canceled");
                return null;
            }
            else
            {
                ImageFeatureExtractorIsWorking = true;
            }
            //System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            //stopWatch.Start();
            imageFeatureExtractorInterpreter.Invoke();

            //stopWatch.Stop();
            //// Get the elapsed time as a TimeSpan value.
            //TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            //string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            //    ts.Hours, ts.Minutes, ts.Seconds,
            //    ts.Milliseconds / 10);
            //Debug.Log("RunTime " + elapsedTime);

            float[,] keyPoints = new float[400, 2];
            imageFeatureExtractorInterpreter.GetOutputTensorData(0, keyPoints);

            float[,] descriptors = new float[400, 256];
            imageFeatureExtractorInterpreter.GetOutputTensorData(1, descriptors);

            float[] scores = new float[400];
            imageFeatureExtractorInterpreter.GetOutputTensorData(2, scores);

            //stopWatch.Restart();

            imageFeatureExtractorResult.setKeyPoints(keyPoints);
            imageFeatureExtractorResult.setDescriptors(descriptors);
            imageFeatureExtractorResult.setScores(scores);

            //stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            //TimeSpan ts1 = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            //string elapsedTime1 = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            //    ts1.Hours, ts1.Minutes, ts1.Seconds,
            //    ts1.Milliseconds);
            //Debug.Log("PostProcessTime " + elapsedTime1);

            ImageFeatureExtractorIsWorking = false;
            return imageFeatureExtractorResult;
        }

        private ImageEncoderResult GetGlobalDescriptor()
        {
            imageEncoderInterpreter.SetInputTensorData(0, input);
            if (cancelToken.IsCancellationRequested)
            {
                Debug.LogError("Mobile VPS task canceled");
                return null;
            }
            else
            {
                ImageEncoderIsWorking = true;
            }
            //System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            //stopWatch.Start();
            imageEncoderInterpreter.Invoke();

            //stopWatch.Stop();
            //// Get the elapsed time as a TimeSpan value.
            //TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            //string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            //    ts.Hours, ts.Minutes, ts.Seconds,
            //    ts.Milliseconds / 10);
            //Debug.Log("RunTime " + elapsedTime);

            float[] globalDescriptor = new float[4096];
            imageEncoderInterpreter.GetOutputTensorData(0, globalDescriptor);

            //stopWatch.Restart();

            imageEncoderResult.setGlobalDescriptor(globalDescriptor);

            //stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            //TimeSpan ts1 = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            //string elapsedTime1 = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            //    ts1.Hours, ts1.Minutes, ts1.Seconds,
            //    ts1.Milliseconds);
            //Debug.Log("PostProcessTime " + elapsedTime1);

            ImageEncoderIsWorking = false;
            return imageEncoderResult;
        }
    }

    public class ImageFeatureExtractorResult
    {
        public byte[] keyPoints;
        public byte[] descriptors;
        public byte[] scores;

        public ImageFeatureExtractorResult()
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

    public class ImageEncoderResult
    {
        public byte[] globalDescriptor;

        public ImageEncoderResult()
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
