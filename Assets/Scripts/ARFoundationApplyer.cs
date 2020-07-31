using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARVRLab.VPSService
{
    public class ARFoundationApplyer : MonoBehaviour
    {
        private ARSessionOrigin arSessionOrigin;

        private Pose startPose;

        public float LerpSpeed = 2.0f;

        private void Start()
        {
            arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
            if (arSessionOrigin == null)
            {
                Debug.LogError("ARSessionOrigin is not found");
            }
        }

        // Сохраняем pose камеры перед отправкой запроса на сервер
        public void LocalisationStart()
        {
            Vector3 pos = arSessionOrigin.camera.transform.position;
            Vector3 rot = new Vector3(0, arSessionOrigin.camera.transform.eulerAngles.y, 0);
            startPose = new Pose(pos, Quaternion.Euler(rot));
        }

        // Применяем полученные transform
        public void ApplyVPSTransform(LocalisationResult localisation)
        {
            //LocalisationStart();/////////////////////////////////////// убрать после внедрения ITracking

            Vector3 NewPosition = arSessionOrigin.transform.localPosition + localisation.LocalPosition - startPose.position;

            var rot = Quaternion.Euler(0, localisation.LocalRotationY, 0);
            var qrot = Quaternion.Inverse(startPose.rotation) * rot;
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
                arSessionOrigin.transform.localPosition = Vector3.Lerp(arSessionOrigin.transform.localPosition, NewPosition, LerpSpeed * Time.deltaTime);
                arSessionOrigin.transform.RotateAround(arSessionOrigin.camera.transform.position, Vector3.up, -CurAngle);
                CurAngle = Mathf.LerpAngle(CurAngle, NewRotationY, LerpSpeed * Time.deltaTime);
                arSessionOrigin.transform.RotateAround(arSessionOrigin.camera.transform.position, Vector3.up, CurAngle);
                yield return null;
            }
        }
    }
}