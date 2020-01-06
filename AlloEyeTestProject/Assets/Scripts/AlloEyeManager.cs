using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ViveSR.anipal.Eye;
using System.Runtime.Serialization;
using UnityEngine.UI;

namespace EyeTracking_lEC
{
    public class AlloEyeManager : MonoBehaviour
    {
        private FSMSystem fsm;
        public readonly Vector3 positionACanvasPosition = new Vector3(0.682f, 0.17f, -0.565f);
        public readonly Vector3 positionBCanvasPosition = new Vector3(1.248f, 0.17f, -0.579f);
        public readonly Vector3 objectDumpPosition = new Vector3(-10f, -10f, -10f);
        public readonly Vector3 viewerAUpPosition = new Vector3(-0.067f, 2.3f, 0.488f);
        public readonly Vector3 viewerADownPosition = new Vector3(-0.067f, 1f, 0.488f);
        public readonly Vector3 viewerBUpPosition = new Vector3(0.448f, 2.3f, 0.469f);
        public readonly Vector3 viewerBDownPosition = new Vector3(0.448f, 1f, 0.469f);
        public readonly float viewingTimeSeconds = 5f;
        public bool viewingDone = false;

        public void SetTransition(Transition t) { fsm.PerformTransition(t); }

        void Start()
        {
            MakeFSM();
        }
        void Update()
        {
            fsm.CurrentState.Reason(gameObject);
            fsm.CurrentState.Act(gameObject);
        }
        private void MakeFSM()
        {
            AwaitCalibrationState ac = new AwaitCalibrationState();
            ac.AddTransition(Transition.CalibrationButtonPressed, StateID.AwaitingInstructionsOK1);
            FirstInstructionsState i1 = new FirstInstructionsState();
            i1.AddTransition(Transition.Instructions1OKPressed, StateID.ViewerALiftUp);
            ViewerALiftUpState liftUpA = new ViewerALiftUpState();
            liftUpA.AddTransition(Transition.ViewerALifted, StateID.PositionAViewing);
            FirstViewing view1 = new FirstViewing();
            view1.AddTransition(Transition.PositionAViewingComplete, StateID.ViewerADropDown);
            ViewerADropDownState dropA = new ViewerADropDownState();
            dropA.AddTransition(Transition.ViewerADropped, StateID.Instructions2Showing);
            SecondInstructionsState i2 = new SecondInstructionsState();
            i2.AddTransition(Transition.Instructions2Pressed, StateID.ViewerBLiftUp);
            ViewerBLiftUpState liftUpB = new ViewerBLiftUpState();
            liftUpB.AddTransition(Transition.ViewerBLifted, StateID.PositionBViewing);
            SecondViewing view2 = new SecondViewing();
            view2.AddTransition(Transition.PositionBViewingComplete, StateID.ViewerBDropDown);
            ViewerBDropDownState dropB = new ViewerBDropDownState();
            dropB.AddTransition(Transition.ViewerBDropped, StateID.Question1Showing);

            fsm = new FSMSystem();
            fsm.AddState(ac);
            fsm.AddState(i1);
            fsm.AddState(liftUpA);
            fsm.AddState(view1);
            fsm.AddState(dropA);
            fsm.AddState(i2);
            fsm.AddState(liftUpB);
            fsm.AddState(view2);
            fsm.AddState(dropB);

        }
        public void ViewTime()
        {
            StartCoroutine(ViewTimeCoroutine());
        }
        IEnumerator ViewTimeCoroutine()
        {
            yield return new WaitForSeconds(viewingTimeSeconds);
            viewingDone = true;
        }
        private void OnGUI()
        {
            // Make a background box
            GUI.Box(new Rect(10, 10, 100, 90), "Loader Menu");

            if (GUI.Button(new Rect(20,40, 100,20),"Launch Calibration"))
            {
                SRanipal_Eye_API.LaunchEyeCalibration(IntPtr.Zero);
            }
        }
    }
    public class AwaitCalibrationState : FSMState
    {
        public GameObject calibrationButtonObject;
        public GameObject calibrationCanvas;

        public AwaitCalibrationState()
        {
            stateID = StateID.AwaitingCalibration;
        }
        public override void Reason(GameObject manager)
        {
           calibrationButtonObject = GameObject.Find("CalibrationButton");

           if (calibrationButtonObject.GetComponent<CalibrationLaunch>().calibrated == true)
           {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.CalibrationButtonPressed);
           }
        }
        public override void Act(GameObject manager)
        {
            calibrationCanvas = GameObject.Find("CalibrationCanvas");
            calibrationCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().positionACanvasPosition;
        }
    }
    public class FirstInstructionsState : FSMState
    {
        public GameObject calibrationCanvas;
        public GameObject firstInstructionsCanvas;
        public GameObject startButton;

        public FirstInstructionsState()
        {
            stateID = StateID.AwaitingInstructionsOK1;
        }
        public override void Reason(GameObject manager)
        {
            startButton = GameObject.Find("StartButton");
            if(startButton.GetComponent<RegisterPress>().pressBool == true)
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.Instructions1OKPressed);
            }       
        }
        public override void Act(GameObject manager)
        {
            calibrationCanvas = GameObject.Find("CalibrationCanvas");
            firstInstructionsCanvas = GameObject.Find("FirstInstructionsCanvas");
            firstInstructionsCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().positionACanvasPosition; 
            calibrationCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().objectDumpPosition;
        }
    }
    public class ViewerALiftUpState : FSMState
    {
        public GameObject firstInstructionsCanvas;
        public GameObject screenA;

        public ViewerALiftUpState()
        {
            stateID = StateID.ViewerALiftUp;
        }
        public override void Reason(GameObject manager)
        {
            screenA = GameObject.Find("ScreenA");
            if(screenA.transform.position == manager.GetComponent<AlloEyeManager>().viewerAUpPosition)
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.ViewerALifted);
            }
        }
        public override void Act(GameObject manager)
        {
            firstInstructionsCanvas = GameObject.Find("FirstInstructionsCanvas");
            firstInstructionsCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().objectDumpPosition;

            screenA = GameObject.Find("ScreenA");
            Vector3 upPosition = manager.GetComponent<AlloEyeManager>().viewerAUpPosition;
            screenA.GetComponent<UpDown>().Lifting(upPosition);
        }
    }
    public class FirstViewing : FSMState
    {
        bool vD;
        bool vD2 = false;
        public FirstViewing()
        {
            stateID = StateID.PositionAViewing;
        }
        public override void Reason(GameObject manager)
        {
            bool vD = manager.GetComponent<AlloEyeManager>().viewingDone;
            if (vD2 == false)
            {
                manager.GetComponent<AlloEyeManager>().ViewTime();
                vD2 = true;
            }
            if (vD == true)
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.PositionAViewingComplete);
                manager.GetComponent<AlloEyeManager>().viewingDone = false;
            }
        }
        public override void Act(GameObject manager)
        {
            //do nothing
        }
    }
    public class ViewerADropDownState : FSMState
    {
        // public GameObject walkInstructionsCanvas
        public GameObject screenA;
        public GameObject movingObject;
        public Vector3 shiftVector;

        public ViewerADropDownState()
        {
            stateID = StateID.ViewerADropDown;
        }
        public override void Reason(GameObject manager)
        {
            screenA = GameObject.Find("ScreenA");
            if (screenA.transform.position == manager.GetComponent<AlloEyeManager>().viewerADownPosition)
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.ViewerADropped);
            }
        }
        public override void Act(GameObject manager)
        {
            screenA = GameObject.Find("ScreenA");
            Vector3 downPosition = manager.GetComponent<AlloEyeManager>().viewerADownPosition;
            screenA.GetComponent<UpDown>().Dropping(downPosition);
            movingObject = GameObject.Find("Sphere");
            shiftVector = new Vector3(movingObject.transform.position.x - 0.14f,
                                      movingObject.transform.position.y,
                                      movingObject.transform.position.z - 0.08f);
            movingObject.transform.position = shiftVector;
        }
    }
    public class SecondInstructionsState : FSMState
    {
        public GameObject walkInstructionsCanvas;
        public GameObject secondInstructionsCanvas;
        public GameObject startButton;

        public SecondInstructionsState()
        {
            stateID = StateID.Instructions2Showing;
        }
        public override void Reason(GameObject manager)
        {
            startButton = GameObject.Find("StartButton2");
            if (startButton.GetComponent<RegisterPress>().pressBool == true)
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.Instructions2Pressed);
            }
        }
        public override void Act(GameObject manager)
        {
            walkInstructionsCanvas = GameObject.Find("WalkInstructionsCanvas");
            secondInstructionsCanvas = GameObject.Find("SecondInstructionsCanvas");

            walkInstructionsCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().positionACanvasPosition;
            secondInstructionsCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().positionBCanvasPosition;
        }
    }
    public class ViewerBLiftUpState : FSMState
    {
        public GameObject secondInstructionsCanvas;
        public GameObject walkInstructionsCanvas;
        public GameObject screenB;

        public ViewerBLiftUpState()
        {
            stateID = StateID.ViewerBLiftUp;
        }
        public override void Reason(GameObject manager)
        {
            screenB = GameObject.Find("ScreenB");
            if (screenB.transform.position == manager.GetComponent<AlloEyeManager>().viewerBUpPosition)
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.ViewerBLifted);
            }
        }
        public override void Act(GameObject manager)
        {
            secondInstructionsCanvas = GameObject.Find("SecondInstructionsCanvas");
            secondInstructionsCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().objectDumpPosition;
            walkInstructionsCanvas = GameObject.Find("WalkInstructionsCanvas");
            walkInstructionsCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().objectDumpPosition;
            screenB = GameObject.Find("ScreenB");
            Vector3 upPosition = manager.GetComponent<AlloEyeManager>().viewerBUpPosition;
            screenB.GetComponent<UpDown>().Lifting(upPosition);
        }
    }
    public class SecondViewing : FSMState
    {
        bool vD;
        bool vD2 = false;
        public SecondViewing()
        {
            stateID = StateID.PositionBViewing;
        }
        public override void Reason(GameObject manager)
        {
            bool vD = manager.GetComponent<AlloEyeManager>().viewingDone;
            if (vD2 == false)
            {
                manager.GetComponent<AlloEyeManager>().ViewTime();
                vD2 = true;
            }
            if (vD == true)
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.PositionBViewingComplete);
            }
        }
        public override void Act(GameObject manager)
        {
            //do nothing
        }
    }
    public class ViewerBDropDownState : FSMState
    {
        // public GameObject walkInstructionsCanvas
        public GameObject screenB;

        public ViewerBDropDownState()
        {
            stateID = StateID.ViewerBDropDown;
        }
        public override void Reason(GameObject manager)
        {
            screenB = GameObject.Find("ScreenB");
            if (screenB.transform.position == manager.GetComponent<AlloEyeManager>().viewerBDownPosition)
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.ViewerBDropped);
            }
        }
        public override void Act(GameObject manager)
        {
            screenB = GameObject.Find("ScreenB");
            Vector3 downPosition = manager.GetComponent<AlloEyeManager>().viewerBDownPosition;
            screenB.GetComponent<UpDown>().Dropping(downPosition);
        }
    }
}
