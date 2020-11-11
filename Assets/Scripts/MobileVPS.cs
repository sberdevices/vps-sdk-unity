using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TensorFlowLite
{
    public class MobileVPS
    {
        private const string FileName = "hfnet_i8_960.tflite";
        public Texture2D TestTexture;

        Interpreter interpreter;

        public MobileVPS()
        {
            var options = new InterpreterOptions
            {
                threads = 2,
            };

            interpreter = new Interpreter(FileUtil.LoadFile(FileName), options);
            interpreter.AllocateTensors();
        }

        //private void OnDestroy()
        //{
        //    interpreter?.Dispose();
        //}

        public async Task<float[,]> GetFeaturesAsync(Color[] buffer)
        {
            return await Task.Run(() => doInference(buffer));
        }

        public float[,] doInference(Color[] buffer)
        {
            Debug.Log("START");
            var idim0 = interpreter.GetInputTensorInfo(0).shape;
            var height = idim0[1]; //960
            var width = idim0[2]; //540
            var channels = idim0[3]; //1

            var input0 = new float[height, width, channels];

            //var pixels = TestTexture.GetPixels();
            for (int i = 0; i < buffer.Length; i++)
            {
                try
                {
                    input0[i / width, i % width, 0] = (float)(buffer[i].grayscale * 255);
                }
                catch (Exception ex)
                {
                    Debug.Log("EXCEPTION: " + ex.Message);
                    return null;
                }
                //float b = (float)ptr[offset + 0] / 255.0f;
                //float g = (float)ptr[offset + 1] / 255.0f;
                //float r = (float)ptr[offset + 2] / 255.0f;
                //float a = (float)ptr[offset + 3] / 255.0f;

                //UnityEngine.Color color = new UnityEngine.Color(r, g, b, a);
                //texture.SetPixel(j, height - i, color);
            }

            interpreter.SetInputTensorData(0, input0);

            var output0 = new float[4096];
            var output1 = new float[400, 2];
            var output2 = new float[400, 256];
            var output3 = new float[400];

            interpreter.Invoke();

            interpreter.GetOutputTensorData(0, output0);
            interpreter.GetOutputTensorData(1, output1);
            interpreter.GetOutputTensorData(2, output2);
            interpreter.GetOutputTensorData(3, output3);

            Debug.Log("DONE");

            Debug.Log("SCORE FOR THIS: " + output3[0]);
            return output1;

            //Texture2D tex = FeatureTexture.mainTexture as Texture2D;

            //for (int i = 0; i < output1.Length / 2; i++)
            //{
            //    tex.SetPixel((int)output1[i, 0], (int)output1[i, 1], Color.yellow);
            //    Debug.Log(output1[i, 0] + ":" + output1[i, 1]);
            //}

            //for (int i = 0; i < tex.height; i++)
            //    for (int j = 0; j < tex.width; j++)
            //    {
            //        if (tex.GetPixel(j, i) == Color.yellow)
            //        {
            //            tex.SetPixel(j - 1, i - 1, Color.green);
            //            tex.SetPixel(j - 1, i, Color.green);
            //            tex.SetPixel(j - 1, i + 1, Color.green);
            //            tex.SetPixel(j, i - 1, Color.green);
            //            tex.SetPixel(j, i + 1, Color.green);
            //            tex.SetPixel(j + 1, i - 1, Color.green);
            //            tex.SetPixel(j + 1, i, Color.green);
            //            tex.SetPixel(j + 1, i + 1, Color.green);
            //        }
            //    }
            //tex.Apply();

            //File.WriteAllBytes("/Users/admin/Downloads/MyTex.png", tex.EncodeToPNG());
        }

        private byte[] GetByteArrayFromImage(Texture2D image)
        {
            byte[] bytesOfImage = image.EncodeToPNG();
            return bytesOfImage;
        }

        private unsafe float[,,] GetImageAsFloats(IntPtr imageIntPtr, int width, int height)
        {
            byte* ptr = (byte*)imageIntPtr.ToPointer();
            var input = new float[height, width, 1];

            int offset = 0;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    input[i, j, 0] = (float)(ptr[offset + 2]);
                    //float b = (float)ptr[offset + 0] / 255.0f;
                    //float g = (float)ptr[offset + 1] / 255.0f;
                    //float r = (float)ptr[offset + 2] / 255.0f;
                    //float a = (float)ptr[offset + 3] / 255.0f;
                    offset += 4;

                    //UnityEngine.Color color = new UnityEngine.Color(r, g, b, a);
                    //texture.SetPixel(j, height - i, color);
                }
            }

            return input;
        }
    }
}
