using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using ViveSR.anipal.Eye;

namespace EyeTracking_lEC
{
    public class FocusChange : MonoBehaviour
    {
        //objectID
        //timestamp
        //direction
    }

    public class SRanipal_EyeFocusSample_LEM : MonoBehaviour
    {
        private FocusInfo FocusInfo;
        private readonly float MaxDistance = 20;
        private readonly GazeIndex[] GazePriority = new GazeIndex[] { GazeIndex.COMBINE, GazeIndex.LEFT, GazeIndex.RIGHT };
        private static EyeData eyeData = new EyeData();
        private bool eye_callback_registered = false;
        List<FocusChange> focusDataCollection = new List<FocusChange>();
        GameObject previousFocus = null; //this will save the object that was previously focused on (including object ID)
        int focusOn = 1;
        int focusOff = 2;

        private void Start()
        {
            if (!SRanipal_Eye_Framework.Instance.EnableEye)
            {
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
                SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT) return;

            if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
            {
                SRanipal_Eye.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
                eye_callback_registered = true;
            }
            else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
            {
                SRanipal_Eye.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
                eye_callback_registered = false;
            }

            foreach (GazeIndex index in GazePriority)
            {
                Ray GazeRay;
                //int dart_board_layer_id = LayerMask.NameToLayer("NoReflection");
                int objectLayer = 8; //object layer 8 is set to array objects
                bool eye_focus;
                if (eye_callback_registered)
                    eye_focus = SRanipal_Eye.Focus(index, out GazeRay, out FocusInfo, 0, MaxDistance, (1 << objectLayer), eyeData);
                else
                    eye_focus = SRanipal_Eye.Focus(index, out GazeRay, out FocusInfo, 0, MaxDistance, (1 << objectLayer));

                if (eye_focus == false) //CHANGE STUFF HERE
                {
                    if (previousFocus != null)
                    {
                        RegisterFocus(previousFocus.name, Time.time, focusOff);
                        previousFocus = null;
                    }
                    break;                    
                }
                else
                {
                    if (previousFocus != null && FocusInfo.collider.gameObject != previousFocus)
                    {
                        RegisterFocus(previousFocus.name, Time.time, focusOff);
                        RegisterFocus(FocusInfo.collider.gameObject.name, Time.time, focusOn);
                        previousFocus = FocusInfo.collider.gameObject;
                    }
                    else if (previousFocus == null && FocusInfo.collider.gameObject != previousFocus) //'previousFocus == null' not strictly necessary
                    {
                        RegisterFocus(FocusInfo.collider.gameObject.name, Time.time, focusOn);
                        previousFocus = FocusInfo.collider.gameObject;
                    }
                    break;
                }                
            }
        }
        private void Release()
        {
            if (eye_callback_registered == true)
            {
                SRanipal_Eye.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));
                eye_callback_registered = false;
            }
        }
        private static void EyeCallback(ref EyeData eye_data)
        {
            eyeData = eye_data;
        }

        private void RegisterFocus(string objectID, float timestamp , int dir)
        {
        //    list.append(newentry);
        }

    }
}