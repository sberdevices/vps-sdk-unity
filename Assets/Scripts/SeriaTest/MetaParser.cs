using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public static class MetaParser
    {
        public static Pose Parse(string meta)
        {
            Pose pose = new Pose();

            string[] meta_arr = meta.Split('\n');
            string[] pos = meta_arr[1].Split(' ');
            string[] rot = meta_arr[2].Split(' ');

            pose.position = new Vector3(float.Parse(pos[0]), float.Parse(pos[1]), float.Parse(pos[2]));
            pose.rotation = Quaternion.Euler(float.Parse(rot[0]), float.Parse(rot[1]), float.Parse(rot[2]));

            return pose;
        }
    }
}
