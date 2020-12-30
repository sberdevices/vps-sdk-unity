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

        [Tooltip("Максимальное расстояние, на котором применяется интерполяция")]
        public float MaxInterpolationDistance = 5;

        [Tooltip("Скорость интерполяции")]
        public float LerpSpeed = 2.0f;

        private void Start()
        {
            arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
            if (arSessionOrigin == null)
            {
                Debug.LogError("ARSessionOrigin is not found");
            }
        }

        /// <summary>
        /// Сохраняем pose камеры перед отправкой запроса на сервер
        /// </summary>
        public void LocalisationStart()
        {
            Vector3 pos = arSessionOrigin.camera.transform.position;
            Vector3 rot = new Vector3(0, arSessionOrigin.camera.transform.eulerAngles.y, 0);
            startPose = new Pose(pos, Quaternion.Euler(rot));
        }

        /// <summary>
        /// Выдает текущую pose камеры
        /// </summary>
        public Pose GetCurrentPose()
        {
            Vector3 pos = arSessionOrigin.camera.transform.position;
            Vector3 rot = new Vector3(0, arSessionOrigin.camera.transform.eulerAngles.y, 0);
            return new Pose(pos, Quaternion.Euler(rot));
        }

        /// <summary>
        /// Применяем полученные transform и возвращает скорректированную с учетом поправки ARFoundation локализацию
        /// </summary>
        /// <returns>The VPST ransform.</returns>
        /// <param name="localisation">Localisation.</param>
        public LocalisationResult ApplyVPSTransform(LocalisationResult localisation)
        {
            LocalisationResult correctedResult = new LocalisationResult();

            correctedResult.LocalPosition = arSessionOrigin.transform.localPosition + localisation.LocalPosition - startPose.position;

            var rot = Quaternion.Euler(0, localisation.LocalRotationY, 0);
            var qrot = Quaternion.Inverse(startPose.rotation) * rot;
            correctedResult.LocalRotationY = qrot.eulerAngles.y;

            Debug.Log("LocalisationDone happend");
            Debug.Log(correctedResult.LocalPosition);

            StopAllCoroutines();

            // важно учитывать был ли это force vps или нет
            StartCoroutine(UpdatePosAndRot(correctedResult.LocalPosition, correctedResult.LocalRotationY));

            return correctedResult;
        }

        /// <summary>
        /// Применяем полученные transform и возвращает скорректированную с учетом поправки ARFoundation локализацию
        /// относительно заданной стартовой позиции
        /// </summary>
        /// <returns>The VPST ransform.</returns>
        /// <param name="localisation">Localisation.</param>
        /// <param name="CustomStartPose">Позиция, с которой была отправлена фотография.</param>
        public LocalisationResult ApplyVPSTransform(LocalisationResult localisation, Pose CustomStartPose)
        {
            LocalisationResult correctedResult = new LocalisationResult();

            correctedResult.LocalPosition = arSessionOrigin.transform.localPosition + localisation.LocalPosition; // ToDo: тут нужен CustomStartPose - подумать

            var rot = Quaternion.Euler(0, localisation.LocalRotationY, 0);
            var qrot = Quaternion.Inverse(CustomStartPose.rotation) * rot;
            correctedResult.LocalRotationY = qrot.eulerAngles.y;
            //correctedResult.LocalRotationY = localisation.LocalRotationY + arSessionOrigin.transform.localRotation.eulerAngles.y - CustomStartPose.rotation.eulerAngles.y;

            StopAllCoroutines();

            // важно учитывать был ли это force vps или нет
            StartCoroutine(UpdatePosAndRot(correctedResult.LocalPosition, correctedResult.LocalRotationY));

            return correctedResult;
        }

        /// <summary>
        /// Применяем NewPosition и NewRotationY с использованием интерполяции
        /// </summary>
        /// <returns>The position and rot.</returns>
        /// <param name="NewPosition">New position.</param>
        /// <param name="NewRotationY">New rotation y.</param>
        IEnumerator UpdatePosAndRot(Vector3 NewPosition, float NewRotationY)
        {
            // если смещение больше MaxInterpolationDistance - перемещаем мгновенно
            if (Vector3.Distance(arSessionOrigin.transform.localPosition, NewPosition) > MaxInterpolationDistance)
            {
                arSessionOrigin.transform.RotateAround(arSessionOrigin.camera.transform.position, Vector3.up, NewRotationY);
                arSessionOrigin.transform.localPosition = NewPosition;
                yield break;
            }

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

        public void ResetTracking()
        {
            StopAllCoroutines();
            if (arSessionOrigin != null)
            {
                arSessionOrigin.transform.position = Vector3.zero;
                arSessionOrigin.transform.rotation = Quaternion.identity;
            }
        }
    }
}