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
            LocalisationResult correctedResult = new LocalisationResult();
            correctedResult.VpsPosition = localisation.VpsPosition;
            correctedResult.VpsRotation = localisation.VpsRotation;
            correctedResult.TrackingPosition = localisation.TrackingPosition;
            correctedResult.TrackingRotation = localisation.TrackingRotation;

            var qrot = Quaternion.Euler(localisation.VpsRotation) * Quaternion.Inverse(Quaternion.Euler(correctedResult.TrackingRotation));
            correctedResult.VpsRotation = qrot.eulerAngles;

            arSessionOrigin.transform.eulerAngles = correctedResult.VpsRotation;

            Pose StartPose = arSessionOrigin.transform.TransformPose(new Pose(localisation.TrackingPosition, Quaternion.Euler(localisation.TrackingRotation)));

            correctedResult.VpsPosition = arSessionOrigin.transform.localPosition + localisation.VpsPosition - StartPose.position;

            VPSLogger.Log(LogLevel.NONE, "VPS localization successful");

            StartCoroutine(UpdatePosAndRot(correctedResult.VpsPosition, correctedResult.VpsRotation));

            VPSLogger.LogFormat(LogLevel.VERBOSE, "Corrected localization position: {0}", correctedResult.VpsPosition);

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
            if (Vector3.Distance(arSessionOrigin.transform.localPosition, NewPosition) > MaxInterpolationDistance)
            {
                arSessionOrigin.transform.position = NewPosition;
                arSessionOrigin.transform.eulerAngles = NewRotation;
                yield break;
            }

            Quaternion NewRotQuaternion = Quaternion.Euler(NewRotation);

            while (true)
            {
                arSessionOrigin.transform.position = Vector3.Lerp(arSessionOrigin.transform.localPosition, NewPosition, LerpSpeed * Time.deltaTime);
                arSessionOrigin.transform.rotation = Quaternion.Lerp(arSessionOrigin.transform.localRotation, NewRotQuaternion, LerpSpeed * Time.deltaTime);
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
                arSessionOrigin.camera.transform.position = Vector3.zero;
                arSessionOrigin.camera.transform.rotation = Quaternion.identity;
            }
        }
    }
}