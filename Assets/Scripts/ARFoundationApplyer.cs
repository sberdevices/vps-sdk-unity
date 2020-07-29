using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARVRLab.VPSService
{
    public class ARFoundationApplyer : MonoBehaviour
    {
        public static ARFoundationApplyer Instance;

        ARSessionOrigin ArSessionOrigin;

        private Pose StartPose;

        public float LerpSpeed = 2.0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        void Start()
        {
            ArSessionOrigin = FindObjectOfType<ARSessionOrigin>();
            if (ArSessionOrigin == null)
            {
                Debug.LogError("ARSessionOrigin не найден");
            }
        }

        // Save camera pose at the moment of start sending request to server
        public void LocalisationStart()
        {
            Vector3 pos = ArSessionOrigin.camera.transform.position;
            Vector3 rot = new Vector3(0, ArSessionOrigin.camera.transform.eulerAngles.y, 0);
            StartPose = new Pose(pos, Quaternion.Euler(rot));
        }

        // Применяем полученные transform
        public void ApplyVPSTransform(LocalisationResult localisation)
        {
            LocalisationStart();///////////////////////////////////////

            Vector3 NewPosition = ArSessionOrigin.transform.localPosition + localisation.LocalPosition - StartPose.position;

            var rot = Quaternion.Euler(0, localisation.LocalRotationY, 0);
            var qrot = Quaternion.Inverse(StartPose.rotation) * rot;
            float NewRotationY = qrot.eulerAngles.y;

            Debug.Log("LocalisationDone happend");
            Debug.Log(NewPosition);

            StopAllCoroutines();
            StartCoroutine(UpdatePosAndRot(NewPosition, NewRotationY));
        }

        // Интерполяция
        IEnumerator UpdatePosAndRot(Vector3 NewPosition, float NewRotationY)
        {
            float CurAngle = 0;

            while (true)
            {
                ArSessionOrigin.transform.localPosition = Vector3.Lerp(ArSessionOrigin.transform.localPosition, NewPosition, LerpSpeed * Time.deltaTime);
                ArSessionOrigin.transform.RotateAround(ArSessionOrigin.camera.transform.position, Vector3.up, -CurAngle);
                CurAngle = Mathf.LerpAngle(CurAngle, NewRotationY, LerpSpeed * Time.deltaTime);
                ArSessionOrigin.transform.RotateAround(ArSessionOrigin.camera.transform.position, Vector3.up, CurAngle);
                yield return null;
            }
        }
    }
}