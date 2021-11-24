using System.IO;
using ARVRLab.ARVRLab.VPSService.JSONs;
using Unity.Collections;
using UnityEngine;


namespace ARVRLab.VPSService
{
    public static class DebugUtils
    {
        /// <summary>
        /// Save images in PersistentDataPath before sending them to the VPS server.
        /// Use it to debug camera frames.
        /// </summary>
        public static bool SaveImagesLocaly = false;

        /// <summary>
        /// Save bytes buffer to PersistentDataPath as .jpg image.
        /// If not null, suffix will be added to file name.
        /// </summary>
        public static void SaveDebugImage(NativeArray<byte> image, VPSTextureRequirement reqs, RequestStruct metaMsg, string suffix = null)
        {
            var texture = new Texture2D(reqs.Width, reqs.Height, reqs.Format, false);
            texture.LoadRawTextureData(image);
            SaveDebugImage(texture, metaMsg, suffix);
            Object.Destroy(texture);
        }

        /// <summary>
        /// Save Texture2D to PersistentDataPath as .jpg image.
        /// If not null, suffix will be added to file name.
        /// </summary>
        public static void SaveDebugImage(Texture2D image, RequestStruct metaMsg, string suffix = null)
        {
            var msgID = metaMsg.data.id;

            string fileName;
            if (suffix == null)
                fileName = $"{msgID}.jpg";
            else
                fileName = $"{msgID}_{suffix}.jpg";

            var path = Path.Combine(Application.persistentDataPath, fileName);

            var jpg = image.EncodeToJPG(100);
            File.WriteAllBytes(path, jpg);

            VPSLogger.Log(LogLevel.DEBUG, $"Saved camera image: {path}");
        }
    }
}