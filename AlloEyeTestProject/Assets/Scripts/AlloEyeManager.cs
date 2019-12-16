using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ViveSR.anipal.Eye;
using System.Runtime.Serialization;

namespace EyeTracking_lEC
{
    public class AlloEyeManager : MonoBehaviour
    {
        bool calibrationDone = false;
        bool positionADone = false;
        bool positionBDone = false;
        bool firstInstructionsOK = false;
         
        // Start is called before the first frame update
        void Start()
        {
            //load all objects here

            //load option to calibrate
        }

        // Update is called once per frame
        void Update()
        {
            //if calibration not done, and calibration button pressed, load calibration. Calibration button then disappears. Calibration marked as done.

            //if calibration done, then if player not within certain area, instructions to move a little this way or that. Position marked as done.

            //instructions to study scene for a later memory test on the objects. Ok button. If pressed, switch bool...

            //

            //
        }

        private void OnGUI()
        {
            // Make a background box
            GUI.Box(new Rect(10, 10, 100, 90), "Loader Menu");

            if (GUI.Button(new Rect(20,40, 80,20),"Launch Calibration"))
            {
                SRanipal_Eye_API.LaunchEyeCalibration(IntPtr.Zero);
            }
        }
    }
}
