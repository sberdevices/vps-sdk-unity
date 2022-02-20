using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARVRLab.VPSService
{
    public class ARFoundationApplyer : MonoBehaviour
    {
        private ARSessionOrigin arSessionOrigin;

        [Tooltip("Max distance for interpolation")]
        public float MaxInterpolationDistance = 5;

        [Tooltip("Interpolation speed")]
        public float LerpSpeed = 2.0f;

        [Tooltip("Override only North direction or entire phone rotation")]
        public bool RotateOnlyY = true;

        private void Start()
        {
            arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
            if (arSessionOrigin == null)
            {
                VPSLogger.Log(LogLevel.ERROR, "ARSessionOrigin is not found");
            }
        }

        /// <summary>
        /// Apply taked transform and return adjusted ARFoundation localisation
        /// </summary>
        /// <returns>The VPS Transform.</returns>
        public LocalisationResult ApplyVPSTransform(LocalisationResult localisation)
        {
            VPSLogger.LogFormat(LogLevel.VERBOSE, "Received localization position: {0}", localisation.VpsPosition);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "Received localization rotation: {0}", localisation.VpsRotation);
            LocalisationResult correctedResult = (LocalisationResult)localisation.Clone();

            correctedResult.VpsPosition -= correctedResult.TrackingPosition;
            correctedResult.VpsRotation -= correctedResult.TrackingRotation;

            StopAllCoroutines();
            StartCoroutine(UpdatePosAndRot(correctedResult.VpsPosition, correctedResult.VpsRotation));

            VPSLogger.LogFormat(LogLevel.VERBOSE, "Corrected localization position: {0}", correctedResult.VpsPosition);
            VPSLogger.LogFormat(LogLevel.VERBOSE, "Corrected localization rotation: {0}", correctedResult.VpsRotation);

            return correctedResult;
        }

        /// <summary>
        /// Apply NewPosition and NewRotationY with interpolation
        /// </summary>
        /// <returns>The position and rotation.</returns>
        /// <param name="NewPosition">New position.</param>
        /// <param name="NewRotation">New rotation y.</param>
        IEnumerator UpdatePosAndRot(Vector3 NewPosition, Vector3 NewRotation)
        {
            if (RotateOnlyY)
            {
                NewRotation.x = 0;
                NewRotation.z = 0;
            }

            // if the offset is greater than MaxInterpolationDistance - move instantly
            //if (Vector3.Distance(arSessionOrigin.transform.localPosition, NewPosition) > MaxInterpolationDistance)
            {
                arSessionOrigin.transform.localPosition = NewPosition;
                arSessionOrigin.transform.rotation = Quaternion.identity;

                RotateAroundThreeAxes(NewRotation);

                yield break;
            }

            //Vector3 CurRotation = Vector3.zero;

            //while (true)
            //{
            //    arSessionOrigin.transform.localPosition = Vector3.Lerp(arSessionOrigin.transform.localPosition, NewPosition, LerpSpeed * Time.deltaTime);

            //    RotateAroundThreeAxes(-CurRotation);
            //    CurRotation.x = Mathf.LerpAngle(CurRotation.x, NewRotation.x, LerpSpeed * Time.deltaTime);
            //    CurRotation.y = Mathf.LerpAngle(CurRotation.y, NewRotation.y, LerpSpeed * Time.deltaTime);
            //    CurRotation.z = Mathf.LerpAngle(CurRotation.z, NewRotation.z, LerpSpeed * Time.deltaTime);
            //    RotateAroundThreeAxes(CurRotation);
            //    yield return null;
            //}
        }

        private void RotateAroundThreeAxes(Vector3 rotateVector)
        {
            arSessionOrigin.transform.RotateAround(arSessionOrigin.camera.transform.position, arSessionOrigin.camera.transform.forward, rotateVector.z);
            arSessionOrigin.transform.RotateAround(arSessionOrigin.camera.transform.position, arSessionOrigin.camera.transform.right, rotateVector.x);
            arSessionOrigin.transform.RotateAround(arSessionOrigin.camera.transform.position, arSessionOrigin.camera.transform.up, rotateVector.y);
        }

        public void ResetTracking()
        {
            StopAllCoroutines();
            if (arSessionOrigin != null)
            {
                arSessionOrigin.transform.position = Vector3.zero;
                arSessionOrigin.transform.rotation = Quaternion.identity;
                arSessionOrigin.camera.transform.position = Vector3.zero;
                arSessionOrigin.camera.transform.rotation = Quaternion.identity;
            }
        }
    }
}