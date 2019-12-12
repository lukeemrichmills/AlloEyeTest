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
    ///name of object being focused on or off, time stamp, and direction of focus (i.e. on or off)
    public class FocusChange 
    {
        public string objectID;
        public float time;
        public int direction;
        

        public FocusChange(string newName, float newTime, int newDir)
        {
            objectID = newName;
            time = newTime;
            direction = newDir;
        }        
    }

    public class FixationPoint
    {
        public float time;
        public float colX;
        public float colY;
        public float colZ;

        public FixationPoint(float newtime, float X, float Y, float Z)
        {
            time = newtime;
            colX = X;
            colY = Y;
            colZ = Z;
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
        List<FocusChange> focusObjectCollection = new List<FocusChange>();
        List<FixationPoint> fixationPointCollection = new List<FixationPoint>();
        GameObject previousFocus = null; //this will save the object that was previously focused on (including object ID)
        int focusOn = 1;
        int focusOff = 0;
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
                    RegisterFixationPoint(Time.time, FocusInfo.point);

                    if (previousFocus != null && FocusInfo.collider.gameObject != previousFocus)
                    {
                        RegisterFocus(previousFocus.name, Time.time, focusOff);
                        Debug.Log("Focus off " + previousFocus.name + " at time: " + Time.time);
                        RegisterFocus(FocusInfo.collider.gameObject.name, Time.time, focusOn);
                        Debug.Log("Focus on " + FocusInfo.collider.gameObject.name + " at time: " + Time.time);
                        previousFocus = FocusInfo.collider.gameObject;
                    }
                    else if (previousFocus == null && FocusInfo.collider.gameObject != previousFocus) //'previousFocus == null' not strictly necessary
                    {
                        RegisterFocus(FocusInfo.collider.gameObject.name, Time.time, focusOn);
                        Debug.Log("Focus on " + FocusInfo.collider.gameObject.name + " at time: " + Time.time);
                        previousFocus = FocusInfo.collider.gameObject;
                    }
                    break;
                }
                else
                {
                    if (previousFocus != null)
                    {
                        RegisterFocus(previousFocus.name, Time.time, focusOff);
                        Debug.Log("Focus off " + previousFocus.name + " at time: " + Time.time);
                        previousFocus = null;
                    }
                    break;
                }                
            }
        }
        //called when user stops playmode
        private void OnApplicationQuit()
        {
            SaveToCSV(focusObjectCollection, fixationPointCollection);
            //saves list of eye tracking data to csv file
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
        private void RegisterFocus(string objectID, float timestamp , int dir)
        {
            focusObjectCollection.Add(new FocusChange(objectID, timestamp, dir));
            
        }

        private void RegisterFixationPoint(float timestamp, Vector3 colPoint)
        {
            fixationPointCollection.Add(new FixationPoint(timestamp, colPoint.x, colPoint.y, colPoint.z));
        }

        //function to save data collection list to CSV file
        void SaveToCSV(List<FocusChange> focusObjectList, List<FixationPoint> fixationPointList)
        {
            string filePathObject = Application.dataPath + "/CSV/" + "Focus_Change.csv";
            string filePathFixPoint = Application.dataPath + "/CSV/" + "Fixation_Point.csv";

            StreamWriter writer1 = new StreamWriter(filePathObject);
            writer1.WriteLine("Object,Time,Focus On/Off");
            for (int i = 0; i < focusObjectList.Count; i++)
            {
                writer1.WriteLine(focusObjectList[i].objectID + "," 
                    + focusObjectList[i].time + "," 
                    + focusObjectList[i].direction);
            }
            writer1.Flush();

            StreamWriter writer2 = new StreamWriter(filePathFixPoint);
            writer2.WriteLine("Time,X,Y,Z");
            for (int i = 0; i < fixationPointList.Count; i++)
            {
                writer2.WriteLine(fixationPointList[i].time + ","
                    + fixationPointList[i].colX + ","
                    + fixationPointList[i].colY + "," + fixationPointList[i].colZ);
            }
            writer2.Flush();

            //writer.Close();
            Debug.Log("CSV file written");
        }

//        private string getPath()
//        {
//#if UNITY_EDITOR
//            return Application.dataPath + "/CSV/" + "Data_Output.csv";

//        }
    }
}