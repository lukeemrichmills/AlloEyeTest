using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using ViveSR.anipal.Eye;

namespace EyeTracking_lEC
{
    ///new type for list; types of data for data collection through eye tracking: 
    ///name of object being focused on or off, time stamp, position of object, and collision point (i.e. gaze point location)
    public class GazeData 
    {
        public string objectID;
        public float time;
        public float objPosX;
        public float objPosY;
        public float objPosZ;
        public float objScaleX;
        public float objScaleY;
        public float objScaleZ;
        public float colX;
        public float colY;
        public float colZ;

        public GazeData(float newTime, string newName, float oPX, float oPY, float oPZ,
                        float oSX, float oSY, float oSZ,
                        float cX, float cY, float cZ)
        {
            objectID = newName;
            time = newTime;
            //direction = newDir;
            objPosX = oPX;
            objPosY = oPY;
            objPosZ = oPZ;
            objScaleX = oSX;
            objScaleY = oSY;
            objScaleZ = oSZ;
            colX = cX;
            colY = cY;
            colZ = cZ;
        }
    }

    ///main class used to detect eye focus on objects through raycasting. 
    ///This class adapted from SRanipal EyeFocusSample script
    public class SRanipal_EyeFocusSample_LEM : MonoBehaviour
    {
        private FocusInfo FocusInfo;
        private readonly float MaxDistance = 20;
        private readonly GazeIndex[] GazePriority = new GazeIndex[] { GazeIndex.COMBINE, GazeIndex.LEFT, GazeIndex.RIGHT };
        private static EyeData eyeData = new EyeData();
        private bool eye_callback_registered = false;
        public List<GazeData> gazeDataCollection = new List<GazeData>();
       
        GameObject previousFocus = null; //this will save the object that was previously focused on (including object ID)
        //int focusOn = 1;
        //int focusOff = 0;
        Vector3 nullVector = new Vector3(99f, 99f, 99f); //used for collision point at 'focus off'

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
                
                int objectLayer = 8; //object layer 8 is set to array objects
                bool eye_focus;
                if (eye_callback_registered)
                    eye_focus = SRanipal_Eye.Focus(index, out GazeRay, out FocusInfo, 0, MaxDistance, (1 << objectLayer), eyeData);
                else
                    eye_focus = SRanipal_Eye.Focus(index, out GazeRay, out FocusInfo, 0, MaxDistance, (1 << objectLayer));

                //code that saves focus data, change this for collecting data or coding interactions with objects
                if (eye_focus) 
                {
                    if(FocusInfo.collider.gameObject.CompareTag("ArrayObject"))
                    RegisterGaze(Time.time, FocusInfo.collider.gameObject.name, FocusInfo.transform.position,
                                 FocusInfo.transform.localScale, FocusInfo.point); //saves eye ray collision point
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
        //function to record eye focus data to list
        public void RegisterGaze(float time, string objectID, Vector3 objPosition, Vector3 objScale, Vector3 colPoint)
        {
            GazeData gd = new GazeData(time, objectID, objPosition.x, objPosition.y, objPosition.z,
                                   objScale.x, objScale.y, objScale.z,
                                   colPoint.x, colPoint.y, colPoint.z);
            
            gazeDataCollection.Add(gd);                 
        }
    }
}