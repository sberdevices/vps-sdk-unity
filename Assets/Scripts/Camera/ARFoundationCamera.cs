using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARVRLab.VPSService
{
    public class ARFoundationCamera : MonoBehaviour, ICamera
    {
        public ARCameraManager cameraManager;
        public RawImage image;
        private Texture2D texture;

        // Start is called before the first frame update
        void Start()
        {
            RenderTexture rt = new RenderTexture(image.texture.width, image.texture.height, 0);
            RenderTexture.active = rt;
            Graphics.Blit(image.texture, rt);

            texture = new Texture2D(image.texture.width, image.texture.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            texture.Apply();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public unsafe Texture2D GetFrame()
        {
            return texture;
            //// Пытаемся получить последнее изображение с камеры
            //XRCameraImage image;
            //if (!cameraManager.TryGetLatestImage(out image))
            //{
            //    return null;
            //}

            //var format = TextureFormat.RGBA32;

            //// Создаем текстуру
            //if (currentTexture == null || currentTexture.width != CameraProperties.DownscalledResolution.x || currentTexture.height != CameraProperties.DownscalledResolution.y)
            //{
            //    currentTexture = new Texture2D(CameraProperties.DownscalledResolution.x, CameraProperties.DownscalledResolution.y, format, false);
            //}

            //// Настраиваем параметры: задаем формат, отражаем по горизонтали (лево | право)
            //var conversionParams = new XRCameraImageConversionParams(image, format, CameraImageTransformation.MirrorY);
            //// Задаем downscale до нужного разрешения
            //conversionParams.outputDimensions = new Vector2Int(CameraProperties.DownscalledResolution.x, CameraProperties.DownscalledResolution.y);

            //// Получаем ссылку на массив байтов текущей текстуры
            //var rawTextureData = currentTexture.GetRawTextureData<byte>();
            //try
            //{
            //    // Копируем байты из изображения с камеры в текстуру
            //    image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
            //}
            //finally
            //{
            //    // Высвобождаем память
            //    image.Dispose();
            //}

            //currentTexture.Apply();

            //return currentTexture;
        }

        public Vector2 GetFocalPixelLength()
        {
            XRCameraIntrinsics intrins;
            if (cameraManager.TryGetIntrinsics(out intrins))
            {
                return intrins.focalLength;
            }

            return Vector2.zero;
        }

        public Vector2 GetPrincipalPoint()
        {
            XRCameraIntrinsics intrins;
            if (cameraManager.TryGetIntrinsics(out intrins))
            {
                return intrins.focalLength;
            }

            return Vector2.zero;
        }

        public bool IsCameraReady()
        {
            return texture != null;
        }
    }
}