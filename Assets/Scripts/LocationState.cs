using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ARVRLab.VPSService
{
    public enum LocalisationStatus { NO_LOCALISATION, GPS_ONLY, VPS_READY }

    public enum ErrorCode
    {
        NO_ERROR, NO_INTERNET, NO_GPS_PERMISSION, NO_CAMERA, NO_CAMERA_PERMISION,
        TRACKING_NOT_AVALIABLE, SERVER_INTERNAL_ERROR, LOCALISATION_FAIL, TIMEOUT_ERROR
    }

    public class LocationState
    {
        public LocalisationStatus Status;
        public ErrorCode Error;
        public LocalisationResult Localisation;

        public LocationState()
        {
            Status = LocalisationStatus.NO_LOCALISATION;
            Error = ErrorCode.NO_ERROR;
            Localisation = new LocalisationResult();
        }
    }
}
