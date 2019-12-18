using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ViveSR.anipal.Eye;
using System.Runtime.Serialization;

public class CalibrationLaunch : MonoBehaviour
{
    public bool calibrated = false;

   public void LaunchCalibration()
    {
        if(calibrated == false)
        {
            SRanipal_Eye_API.LaunchEyeCalibration(IntPtr.Zero);
            calibrated = true;
        }
    }
}
