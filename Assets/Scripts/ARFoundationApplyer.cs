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

            // subtract the sent position and rotation because the child has them
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

            // save current anchor position and rotation
            Vector3 startPosition = arSessionOrigin.transform.position;
            Quaternion startRotation = arSessionOrigin.transform.rotation;

            // set new position
            arSessionOrigin.transform.position = NewPosition;
            // we need rotate only camera, so we reset parent rotation
            arSessionOrigin.transform.rotation = Quaternion.identity;
            // and rotate parent around child on three axes
            RotateAroundThreeAxes(NewRotation);

            // save anchor position and rotation
            Vector3 targetPosition = arSessionOrigin.transform.position;
            Quaternion targetRotation = arSessionOrigin.transform.rotation;

            // if the offset is greater than MaxInterpolationDistance - don't use interpolation (move instantly)
            if (Vector3.Distance(startPosition, targetPosition) > MaxInterpolationDistance)
                yield break;

            // interpolate position and rotation from start pos to target
            float interpolant = 0;
            while (interpolant < 1)
            {
                interpolant += LerpSpeed * Time.deltaTime;
                arSessionOrigin.transform.position = Vector3.Lerp(startPosition, targetPosition, interpolant);
                arSessionOrigin.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, interpolant);
                yield return null;
            }
        }

        private void RotateAroundThreeAxes(Vector3 rotateVector)
        {
            // rotate anchor (parent) around camera (child)
            arSessionOrigin.transform.RotateAround(arSessionOrigin.camera.transform.position, Vector3.forward, rotateVector.z);
            arSessionOrigin.transform.RotateAround(arSessionOrigin.camera.transform.position, Vector3.right, rotateVector.x);
            arSessionOrigin.transform.RotateAround(arSessionOrigin.camera.transform.position, Vector3.up, rotateVector.y);
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