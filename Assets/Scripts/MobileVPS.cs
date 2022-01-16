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
        private const string imageFeatureExtractorFileName = "msp_960x540x1_256_400.tflite";
        private const string imageEncoderFileName = "mnv_960x540x1_4096.tflite";

        Interpreter imageFeatureExtractorInterpreter;
        Interpreter imageEncoderInterpreter;

        private float[,,] imageFeatureExtractorInput;
        private float[,,] imageEncoderInput;

        private float[,] keyPointsOutput;
        private float[,] descriptorsOutput;
        private float[] scoresOutput;
        private float[,] globalDescriptorOutput;

        private ImageFeatureExtractorResult imageFeatureExtractorResult;
        private ImageEncoderResult imageEncoderResult;

        private CancellationTokenSource tokenSource;
        private CancellationToken cancelToken;

        public bool ImageFeatureExtractorIsWorking = false;
        public bool ImageEncoderIsWorking = false;

        public VPSTextureRequirement imageFeatureExtractorRequirements;
        public VPSTextureRequirement imageEncoderRequirements;

        public MobileVPS()
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            var imageFeatureExtractorOptions = new InterpreterOptions
            {
                threads = 2
            };

            var imageEncoderOptions = new InterpreterOptions
            {
                threads = 2
            };

            imageFeatureExtractorInterpreter = new Interpreter(FileUtil.LoadFile(imageFeatureExtractorFileName), imageFeatureExtractorOptions);
            imageFeatureExtractorInterpreter.AllocateTensors();

            imageEncoderInterpreter = new Interpreter(FileUtil.LoadFile(imageEncoderFileName), imageEncoderOptions);
            imageEncoderInterpreter.AllocateTensors();

            // ImageFeatureExtractor inputs
            int[] inputShape = imageFeatureExtractorInterpreter.GetInputTensorInfo(0).shape;
            imageFeatureExtractorInput = new float[inputShape[1], inputShape[2], inputShape[3]];
            TextureFormat format = inputShape[3] == 1 ? TextureFormat.R8 : TextureFormat.RGB24;
            imageFeatureExtractorRequirements = new VPSTextureRequirement(inputShape[1], inputShape[2], format);

            // ImageEncoder inputs
            inputShape = imageEncoderInterpreter.GetInputTensorInfo(0).shape;
            imageEncoderInput = new float[inputShape[1], inputShape[2], inputShape[3]];
            format = inputShape[3] == 1 ? TextureFormat.R8 : TextureFormat.RGB24;
            imageEncoderRequirements = new VPSTextureRequirement(inputShape[1], inputShape[2], format);

            //keypoints
            int[] kpOutputShape = imageFeatureExtractorInterpreter.GetOutputTensorInfo(0).shape;
            keyPointsOutput = new float[kpOutputShape[0], kpOutputShape[1]];
            //descriptors
            int[] dOutputShape = imageFeatureExtractorInterpreter.GetOutputTensorInfo(1).shape;
            descriptorsOutput = new float[dOutputShape[0], dOutputShape[1]];
            //scores
            int[] sOutputShape = imageFeatureExtractorInterpreter.GetOutputTensorInfo(2).shape;
            scoresOutput = new float[sOutputShape[0]];
            //globalDescriptor
            int[] gdOutputShape = imageEncoderInterpreter.GetOutputTensorInfo(0).shape;
            globalDescriptorOutput = new float[gdOutputShape[0], gdOutputShape[1]];

            imageFeatureExtractorResult = new ImageFeatureExtractorResult(kpOutputShape, dOutputShape, sOutputShape);
            imageEncoderResult = new ImageEncoderResult(gdOutputShape);

            stopWatch.Stop();
            TimeSpan mvpsInitTS = stopWatch.Elapsed;

            string mvpsInitTime = String.Format("{0:N10}", mvpsInitTS.TotalSeconds);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] MVPSInitTime {0}", mvpsInitTime);
        }

        ~MobileVPS()
        {
            imageFeatureExtractorInterpreter?.Dispose();
            imageEncoderInterpreter?.Dispose();
        }

        public void StopTask()
        {
            VPSLogger.Log(LogLevel.DEBUG, "MVPS task has been canceled");
            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
        }

        public async Task<bool> StartPreprocess(NativeArray<byte> featureExtractorBuffer, NativeArray<byte> encoderBuffer)
        {
            if (tokenSource != null)
            {
                tokenSource.Dispose();
            }
            tokenSource = new CancellationTokenSource();
            cancelToken = tokenSource.Token;

            return await Task.Run(() => Preprocess(featureExtractorBuffer, encoderBuffer), cancelToken);
        }

        private bool Preprocess(NativeArray<byte> featureExtractorBuffer, NativeArray<byte> encoderBuffer)
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            if (!ConvertToFloat(featureExtractorBuffer, ref imageFeatureExtractorInput, imageFeatureExtractorRequirements))
                return false;
            if (imageFeatureExtractorRequirements.Equals(imageEncoderRequirements))
            {
                imageEncoderInput = imageFeatureExtractorInput;
            }
            else
            {
                if (!ConvertToFloat(encoderBuffer, ref imageEncoderInput, imageEncoderRequirements))
                    return false;
            }

            stopWatch.Stop();
            TimeSpan mvpsPreprocessTS = stopWatch.Elapsed;

            string mvpsPreprocessTime = String.Format("{0:N10}", mvpsPreprocessTS.TotalSeconds);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] MVPSImagePreprocessTime {0}", mvpsPreprocessTime);

            return true;
        }

        public async Task<ImageFeatureExtractorResult> GetFeaturesAsync()
        {
            return await Task.Run(() => GetFeatures());
        }

        public async Task<ImageEncoderResult> GetGlobalDescriptorAsync()
        {
            return await Task.Run(() => GetGlobalDescriptor());
        }

        private bool ConvertToFloat(NativeArray<byte> input, ref float[,,] result, VPSTextureRequirement requirement)
        {
            int width = requirement.Height;
            int height = requirement.Width;
            // for input - convert bytes to float, rotate and mirror image
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    result[height - j - 1, width - i - 1, 0] = (float)(input[((i + 1) * height - j - 1)]);
                }

                if (cancelToken.IsCancellationRequested)
                {
                    VPSLogger.Log(LogLevel.DEBUG, "MVPS task has been canceled");
                    return false;
                }
            }

            return true;
        }

        private ImageFeatureExtractorResult GetFeatures()
        {
            imageFeatureExtractorInterpreter.SetInputTensorData(0, imageFeatureExtractorInput);
            if (cancelToken.IsCancellationRequested)
            {
                VPSLogger.Log(LogLevel.DEBUG, "MVPS task has been canceled");
                return null;
            }
            else
            {
                ImageFeatureExtractorIsWorking = true;
            }

            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            imageFeatureExtractorInterpreter.Invoke();

            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan neuronRunTS = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string neuronRunTime = String.Format("{0:N10}", neuronRunTS.TotalSeconds);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] FeatureExtractorInferenceTime {0}", neuronRunTime);

            imageFeatureExtractorInterpreter.GetOutputTensorData(0, keyPointsOutput);
            imageFeatureExtractorInterpreter.GetOutputTensorData(1, descriptorsOutput);
            imageFeatureExtractorInterpreter.GetOutputTensorData(2, scoresOutput);

            stopWatch.Restart();

            imageFeatureExtractorResult.setKeyPoints(keyPointsOutput);
            imageFeatureExtractorResult.setDescriptors(descriptorsOutput);
            imageFeatureExtractorResult.setScores(scoresOutput);

            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan postProcessTS = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string postProcessTime = String.Format("{0:N10}", postProcessTS.TotalSeconds);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] FeatureExtractorPostProcessTime {0}", postProcessTime);

            ImageFeatureExtractorIsWorking = false;
            return imageFeatureExtractorResult;
        }

        private ImageEncoderResult GetGlobalDescriptor()
        {
            imageEncoderInterpreter.SetInputTensorData(0, imageEncoderInput);
            if (cancelToken.IsCancellationRequested)
            {
                VPSLogger.Log(LogLevel.DEBUG, "Mobile VPS task canceled");
                return null;
            }
            else
            {
                ImageEncoderIsWorking = true;
            }

            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            imageEncoderInterpreter.Invoke();

            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan neuronRunTS = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string neuronRunTime = String.Format("{0:N10}", neuronRunTS.TotalSeconds);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] EncoderInferenceTime {0}", neuronRunTime);

            imageEncoderInterpreter.GetOutputTensorData(0, globalDescriptorOutput);

            stopWatch.Restart();

            imageEncoderResult.setGlobalDescriptor(globalDescriptorOutput);

            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan postProcessTS = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string postProcessTime = String.Format("{0:N10}", postProcessTS.TotalSeconds);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric] EncoderPostProcessTime {0}", postProcessTime);

            ImageEncoderIsWorking = false;
            return imageEncoderResult;
        }
    }

    public class ImageFeatureExtractorResult
    {
        public byte[] keyPoints;
        public byte[] descriptors;
        public byte[] scores;

        private int kpRows, kpCols;
        private int dRows, dCols;
        private int sCols;

        public ImageFeatureExtractorResult(int[] kpOutputShape, int[] dOutputShape, int[] sOutputShape)
        {
            kpRows = kpOutputShape[0];
            kpCols = kpOutputShape[1];
            keyPoints = new byte[kpRows * kpCols * 2];
            dRows = dOutputShape[0];
            dCols = dOutputShape[1];
            descriptors = new byte[dRows * dCols * 2];
            sCols = sOutputShape[0];
            scores = new byte[sCols * 2];
        }

        public void setKeyPoints(float[,] points)
        {
            for (int i = 0; i < kpRows; i++)
            {
                for (int j = 0; j < kpCols; j++)
                {
                    byte[] kp = BitConverter.GetBytes(Mathf.FloatToHalf(points[i, j]));
                    keyPoints[i * kpCols * 2 + j * 2] = kp[0];
                    keyPoints[i * kpCols * 2 + j * 2 + 1] = kp[1];
                }
            }
        }

        public void setDescriptors(float[,] descs)
        {
            for (int i = 0; i < dRows; i++)
            {
                for (int j = 0; j < dCols; j++)
                {
                    byte[] d = BitConverter.GetBytes(Mathf.FloatToHalf(descs[i, j]));
                    descriptors[i * dCols * 2 + j * 2] = d[0];
                    descriptors[i * dCols * 2 + j * 2 + 1] = d[1];
                }
            }
        }


        public void setScores(float[] scrs)
        {
            for (int i = 0; i < sCols; i++)
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
        private int gbRows, gbCols;

        public ImageEncoderResult(int[] gdOutputShape)
        {
            gbRows = gdOutputShape[0];
            gbCols = gdOutputShape[1];
            globalDescriptor = new byte[gbRows * gbCols * 2];
        }

        public void setGlobalDescriptor(float[,] globDesc)
        {
            for (int i = 0; i < gbRows; i++)
            {
                for (int j = 0; j < gbCols; j++)
                {
                    byte[] d = BitConverter.GetBytes(Mathf.FloatToHalf(globDesc[i, j]));
                    globalDescriptor[i * gbCols * 2 + j * 2] = d[0];
                    globalDescriptor[i * gbCols * 2 + j * 2 + 1] = d[1];
                }
            }
        }
    }
}
