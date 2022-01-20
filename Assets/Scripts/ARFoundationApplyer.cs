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
        /// Save camera pose before sending request
        /// </summary>
        public void LocalisationStart()
        {
            Vector3 pos = arSessionOrigin.camera.transform.position;
            Vector3 rot;
            if (RotateOnlyY)
            {
                rot = new Vector3(0, arSessionOrigin.camera.transform.eulerAngles.y, 0); 
            }
            else
            {
                rot = arSessionOrigin.camera.transform.eulerAngles;
            }
            startPose = new Pose(pos, Quaternion.Euler(rot));
        }

        /// <summary>
        /// Get current camera pose
        /// </summary>
        public Pose GetCurrentPose()
        {
            Vector3 pos = arSessionOrigin.camera.transform.position;
            Vector3 rot;
            if (RotateOnlyY)
            {
                rot = new Vector3(0, arSessionOrigin.camera.transform.eulerAngles.y, 0);
            }
            else
            {
                rot = arSessionOrigin.camera.transform.eulerAngles;
            }
            return new Pose(pos, Quaternion.Euler(rot));
        }

        /// <summary>
        /// Apply taked transform and return adjusted ARFoundation localisation
        /// </summary>
        /// <returns>The VPS Transform.</returns>
        public LocalisationResult ApplyVPSTransform(LocalisationResult localisation)
        {
            VPSLogger.LogFormat(LogLevel.VERBOSE, "Received localization position: {0}", localisation.LocalPosition);
            LocalisationResult correctedResult = localisation;

            correctedResult.LocalPosition = arSessionOrigin.transform.localPosition + localisation.LocalPosition - startPose.position;

            if (RotateOnlyY)
            {
                var qrot = Quaternion.Inverse(startPose.rotation) * Quaternion.Euler(localisation.LocalRotation);
                correctedResult.LocalRotation = qrot.eulerAngles;
            }

            VPSLogger.Log(LogLevel.NONE, "VPS localization successful");
            VPSLogger.LogFormat(LogLevel.DEBUG, "VPS position: {0}", correctedResult.LocalPosition);

            StopAllCoroutines();

            StartCoroutine(UpdatePosAndRot(correctedResult.LocalPosition, correctedResult.LocalRotation));

            VPSLogger.LogFormat(LogLevel.VERBOSE, "Corrected localization position: {0}", correctedResult.LocalPosition);

            return correctedResult;
        }

        /// <summary>
        /// Apply taked transform and return adjusted ARFoundation localisation
        /// relative to a custom start position
        /// </summary>
        /// <returns>The VPS Transform.</returns>
        public LocalisationResult ApplyVPSTransform(LocalisationResult localisation, Pose CustomStartPose)
        {
            VPSLogger.LogFormat(LogLevel.VERBOSE, "Received serial localization position: {0}", localisation.LocalPosition);
            LocalisationResult correctedResult = localisation;

            correctedResult.LocalPosition = arSessionOrigin.transform.localPosition + localisation.LocalPosition - CustomStartPose.position;

            if (RotateOnlyY)
            {
                var qrot = Quaternion.Inverse(CustomStartPose.rotation) * Quaternion.Euler(localisation.LocalRotation);
                correctedResult.LocalRotation = qrot.eulerAngles;
            }

            VPSLogger.Log(LogLevel.NONE, "VPS localization successful");
            VPSLogger.LogFormat(LogLevel.DEBUG, "VPS position: {0}", correctedResult.LocalPosition);

            StopAllCoroutines();

            StartCoroutine(UpdatePosAndRot(correctedResult.LocalPosition, correctedResult.LocalRotation));

            VPSLogger.LogFormat(LogLevel.VERBOSE, "Corrected serial localization position: {0}", correctedResult.LocalPosition);

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
            // if the offset is greater than MaxInterpolationDistance - move instantly
            if (!RotateOnlyY || Vector3.Distance(arSessionOrigin.transform.localPosition, NewPosition) > MaxInterpolationDistance)
            {
                arSessionOrigin.transform.localPosition = NewPosition;
                if (RotateOnlyY)
                    arSessionOrigin.transform.RotateAround(arSessionOrigin.camera.transform.position, Vector3.up, NewRotation.y);
                else
                    arSessionOrigin.transform.eulerAngles = NewRotation;
                yield break;
            }

            float CurAngle = 0;

            while (true)
            {
                arSessionOrigin.transform.localPosition = Vector3.Lerp(arSessionOrigin.transform.localPosition, NewPosition, LerpSpeed * Time.deltaTime);

                arSessionOrigin.transform.RotateAround(arSessionOrigin.camera.transform.position, Vector3.up, -CurAngle);
                CurAngle = Mathf.LerpAngle(CurAngle, NewRotation.y, LerpSpeed * Time.deltaTime);
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
                arSessionOrigin.camera.transform.position = Vector3.zero;
                arSessionOrigin.camera.transform.rotation = Quaternion.identity;
            }
        }
    }
}