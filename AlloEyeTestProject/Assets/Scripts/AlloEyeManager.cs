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
        //finite state machine
        private FSMSystem fsm;
        public void SetTransition(Transition t) { fsm.PerformTransition(t); }

        //define vectors for positions
        public readonly Vector3 positionACanvasPosition = new Vector3(0.682f, 0.17f, -0.565f);
        public readonly Vector3 positionBCanvasPosition = new Vector3(1.248f, 0.17f, -0.579f);
        public readonly Vector3 objectDumpPosition = new Vector3(-10f, -10f, -10f);
        public readonly Vector3 viewerAUpPosition = new Vector3(-0.067f, 2.3f, 0.488f);
        public readonly Vector3 viewerADownPosition = new Vector3(-0.067f, 1f, 0.488f);
        public readonly Vector3 viewerBUpPosition = new Vector3(0.448f, 2.3f, 0.469f);
        public readonly Vector3 viewerBDownPosition = new Vector3(0.448f, 1f, 0.469f);

        //define times for waiting
        public readonly float viewingTimeSeconds = 5f;
        public readonly float trialEndWaitTime = 3f; //also in seconds

        //define booleans
        public bool viewingDone = false; //bool used for determining whether any viewing phase has been completed
        public bool firstViewOnce = false; //bool used for determining if first viewing phase has been completed (for no-move condition)

        //define trial variables
        public int trialCounter = 0;
        public int trialLimit = 3; //change this for when to stop
        public int moveCode = 1; //1 = move (walk); 2 = move (teleport); 3 = don't move (stick). Change to enum?
        public bool tableRotation = false; //false = no table rotation; true = table rotates.
        public bool objectShift = true; //true = an object moves; false = no objects move. 
        //public enum MoveCode { Walk, Teleport, Stick};   

        //define objects in array including table
        public GameObject sphere;
        public GameObject cube;
        public GameObject cylinder;
        public GameObject capsule;
        public List<GameObject> arrayObjectsPrePlace = new List<GameObject>();
        public List<GameObject> arrayObjectsPostPlace = new List<GameObject>();
        public GameObject table;

        Vector3 randPos;
        readonly float minDistance = 0.2f;

        void Start()
        {
            //get table
            table = GameObject.Find("Table");
            Vector3 tableTop = new Vector3(table.transform.position.x, 0.6392f, table.transform.position.z);
            //get array objects and add to a pre-placement list
            sphere = GameObject.Find("Sphere");
            arrayObjectsPrePlace.Add(sphere); 
            cube = GameObject.Find("Cube");
            arrayObjectsPrePlace.Add(cube);
            cylinder = GameObject.Find("Cylinder");
            arrayObjectsPrePlace.Add(cylinder);
            capsule = GameObject.Find("Capsule");
            arrayObjectsPrePlace.Add(capsule);
            //put in object dump
            foreach (GameObject go in arrayObjectsPrePlace)
            {
                go.transform.position = objectDumpPosition;
            }

            //generate random position on table
            randPos = tableTop + (UnityEngine.Random.insideUnitSphere * (table.transform.localScale.x / 2 - 0.1f));
            randPos = new Vector3(randPos.x, 0.6392f, randPos.z); //fixed to top of table

            while (arrayObjectsPrePlace.Count > 0) //while still objects left to place
            {
                int randSelect = UnityEngine.Random.Range(0, arrayObjectsPrePlace.Count); //select a random object to place
                GameObject objectToPlace = arrayObjectsPrePlace[randSelect]; //save to another object
                arrayObjectsPrePlace.Remove(arrayObjectsPrePlace[randSelect]); //remove this from pre placement array
                Debug.Log(arrayObjectsPrePlace.Count + " arrayObjects");

                //Placing Objects on Table
                int farEnoughCount  = 0; //counter for how many objects are too close to the object being placed, default set to 0
                while (farEnoughCount < arrayObjectsPostPlace.Count) //while the number of objects that are far enough away from the object being placed is less than the total number of objects on the table...
                {
                    farEnoughCount = 0; //reset counter with every new random position
                    //generate random position on table
                    randPos = tableTop + (UnityEngine.Random.insideUnitSphere * (table.transform.localScale.x / 2 - 0.1f));
                    randPos = new Vector3(randPos.x, 0.6392f, randPos.z); //fixed to top of table
                    
                    foreach (GameObject go in arrayObjectsPostPlace) //for each object on the table
                    {
                        float d = Vector3.Distance(randPos, go.transform.position); //for debugging
                        if (Vector3.Distance(randPos, go.transform.position) > minDistance) //if the distance between random position and objects on table is greater than min distance
                        {
                            farEnoughCount++;
                        }
                    }
                }
                objectToPlace.transform.position = randPos; // place object
                arrayObjectsPostPlace.Add(objectToPlace); //add to list of objects on table
            }
            //MoveCode trialMoveCode = MoveCode.Stick;
            MakeFSM();
        }
        void Update()
        {
            fsm.CurrentState.Reason(gameObject);
            fsm.CurrentState.Act(gameObject);
        }
        private void MakeFSM()
        {
            //define each state in FSM and their possible transitions
            AwaitCalibrationState ac = new AwaitCalibrationState();
            ac.AddTransition(Transition.CalibrationButtonPressed, StateID.AwaitingInstructionsOK1);
            FirstInstructionsState i1 = new FirstInstructionsState();
            i1.AddTransition(Transition.Instructions1OKPressed, StateID.ViewerALiftUp);
            ViewerALiftUpState liftUpA = new ViewerALiftUpState();
            liftUpA.AddTransition(Transition.ViewerALifted, StateID.PositionAViewing);
            FirstViewing view1 = new FirstViewing();
            view1.AddTransition(Transition.PositionAViewingComplete, StateID.ViewerADropDown);
            ViewerADropDownState dropA = new ViewerADropDownState();
            dropA.AddTransition(Transition.ViewerADropped, StateID.ArrayAdjustments); //all conditions
            dropA.AddTransition(Transition.ViewerADropped2, StateID.Question1Showing); //no move condition
            ArrayAdjustments aa = new ArrayAdjustments();
            aa.AddTransition(Transition.ArrayAdjusted, StateID.Instructions2Showing);
            SecondInstructionsState i2 = new SecondInstructionsState();
            i2.AddTransition(Transition.Instructions2BPressed, StateID.ViewerBLiftUp); //from position B (move conditions)
            i2.AddTransition(Transition.Instructions2APressed, StateID.ViewerALiftUp); //from position A (no move condition)
            ViewerBLiftUpState liftUpB = new ViewerBLiftUpState();
            liftUpB.AddTransition(Transition.ViewerBLifted, StateID.PositionBViewing);
            SecondViewing view2 = new SecondViewing();
            view2.AddTransition(Transition.PositionBViewingComplete, StateID.ViewerBDropDown);
            ViewerBDropDownState dropB = new ViewerBDropDownState();
            dropB.AddTransition(Transition.ViewerBDropped, StateID.Question1Showing);
            Question1Showing q1 = new Question1Showing();
            q1.AddTransition(Transition.Question1Answered, StateID.Question2Showing);
            Question2Showing q2 = new Question2Showing();
            q2.AddTransition(Transition.Question2Answered, StateID.TrialEndSwitch);
            TrialEndSwitch tes = new TrialEndSwitch();
            tes.AddTransition(Transition.TrialSwitched, StateID.AwaitingInstructionsOK1);

            //add each state to create FSM
            fsm = new FSMSystem();
            fsm.AddState(ac);
            fsm.AddState(i1);
            fsm.AddState(liftUpA);
            fsm.AddState(view1);
            fsm.AddState(dropA);
            fsm.AddState(aa);
            fsm.AddState(i2);
            fsm.AddState(liftUpB);
            fsm.AddState(view2);
            fsm.AddState(dropB);
            fsm.AddState(q1);
            fsm.AddState(q2);
            fsm.AddState(tes);
        }
        public void WaitTime(float time)
        {
            StartCoroutine(WaitTimeCoroutine(time));
        }
        IEnumerator WaitTimeCoroutine(float time)
        {
            yield return new WaitForSeconds(time);
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
                startButton.GetComponent<RegisterPress>().pressBool = false; //reset button for next trial
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
                manager.GetComponent<AlloEyeManager>().WaitTime(manager.GetComponent<AlloEyeManager>().viewingTimeSeconds);
                vD2 = true;
            }
            if (vD == true)
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.PositionAViewingComplete);
                manager.GetComponent<AlloEyeManager>().viewingDone = false;
                vD2 = false;
                
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
        
        public ViewerADropDownState()
        {
            stateID = StateID.ViewerADropDown;
        }
        public override void Reason(GameObject manager)
        {
            screenA = GameObject.Find("ScreenA");
            if (screenA.transform.position == manager.GetComponent<AlloEyeManager>().viewerADownPosition
                & manager.GetComponent<AlloEyeManager>().firstViewOnce == false) //viewer down AND haven't viewed before
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.ViewerADropped); //transition to array adjustments
                manager.GetComponent<AlloEyeManager>().firstViewOnce = true; //mark first viewing as done
            }
            else if (screenA.transform.position == manager.GetComponent<AlloEyeManager>().viewerADownPosition) //viewer down (AND have viewed before)
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.ViewerADropped2); //transition to questions
            }
        }
        public override void Act(GameObject manager)
        {
            screenA = GameObject.Find("ScreenA");
            Vector3 downPosition = manager.GetComponent<AlloEyeManager>().viewerADownPosition;
            screenA.GetComponent<UpDown>().Dropping(downPosition);
        }
    }
    public class ArrayAdjustments : FSMState
    {
        public GameObject movingObject;
        public Vector3 shiftVector;
        public bool adjustmentsMade = false;
        public GameObject table;

        public ArrayAdjustments()
        {
            stateID = StateID.ArrayAdjustments;
        }
        public override void Reason(GameObject manager)
        {
            if (adjustmentsMade == true)
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.ArrayAdjusted);
            }
        }
        public override void Act(GameObject manager)
        {
            //whether object shifts or not
            if (manager.GetComponent<AlloEyeManager>().objectShift == true)
            {
                    movingObject = GameObject.Find("Sphere");
                    shiftVector = new Vector3(movingObject.transform.position.x - 0.14f,
                                              movingObject.transform.position.y,
                                              movingObject.transform.position.z - 0.08f);
                    movingObject.transform.position = shiftVector;
            }
            //whether table rotates or not
            if (manager.GetComponent<AlloEyeManager>().tableRotation == true)
            {
                //rotate table
                //find table
                //transform.rotate 90f by y axis - rotate by same angle as positions?
                table = GameObject.Find("Table");
                table.transform.Rotate(0f, 90f, 0f);
            }
            adjustmentsMade = true;
        }
    }
    public class SecondInstructionsState : FSMState
    {
        public GameObject walkInstructionsCanvas;
        public GameObject teleportInstructionsCanvas;
        public GameObject secondInstructionsCanvas;
        public GameObject startButton;

        public SecondInstructionsState()
        {
            stateID = StateID.Instructions2Showing;
        }
        public override void Reason(GameObject manager)
        {
            startButton = GameObject.Find("StartButton2");
            if (startButton.GetComponent<RegisterPress>().pressBool == true &
                manager.GetComponent<AlloEyeManager>().moveCode != 3)
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.Instructions2BPressed);
                startButton.GetComponent<RegisterPress>().pressBool = false; //reset button for next trial
                secondInstructionsCanvas = GameObject.Find("SecondInstructionsCanvas");
                secondInstructionsCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().objectDumpPosition;
            }
            else if(startButton.GetComponent<RegisterPress>().pressBool == true)
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.Instructions2APressed);
                startButton.GetComponent<RegisterPress>().pressBool = false; //reset button for next trial
                secondInstructionsCanvas = GameObject.Find("SecondInstructionsCanvas");
                secondInstructionsCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().objectDumpPosition;
            }
        }
        public override void Act(GameObject manager)
        {
            secondInstructionsCanvas = GameObject.Find("SecondInstructionsCanvas");
            if (manager.GetComponent<AlloEyeManager>().moveCode != 3)
            {
                secondInstructionsCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().positionBCanvasPosition;

                if (manager.GetComponent<AlloEyeManager>().moveCode == 1)
                {
                    walkInstructionsCanvas = GameObject.Find("WalkInstructionsCanvas");
                    walkInstructionsCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().positionACanvasPosition;
                }
                else
                {
                    teleportInstructionsCanvas = GameObject.Find("TeleportInstructionsCanvas");
                    teleportInstructionsCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().positionACanvasPosition;
                }
            }
            else
            {
                secondInstructionsCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().positionACanvasPosition;
                secondInstructionsCanvas.GetComponent<RectTransform>().eulerAngles = new Vector3(0f, 42.56f, 0f); //define this at the beginning
            }
        }
    }
    public class ViewerBLiftUpState : FSMState
    {
        public GameObject secondInstructionsCanvas;
        public GameObject walkInstructionsCanvas;
        public GameObject teleportInstructionsCanvas;
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
            teleportInstructionsCanvas = GameObject.Find("TeleportInstructionsCanvas");
            teleportInstructionsCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().objectDumpPosition;
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
                manager.GetComponent<AlloEyeManager>().WaitTime(manager.GetComponent<AlloEyeManager>().viewingTimeSeconds);
                vD2 = true;
            }
            if (vD == true)
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.PositionBViewingComplete);
                manager.GetComponent<AlloEyeManager>().viewingDone = false;
                vD2 = false;
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
        public GameObject walkBackCanvas;
        public GameObject teleportBackCanvas;

        public ViewerBDropDownState()
        {
            stateID = StateID.ViewerBDropDown;
        }
        public override void Reason(GameObject manager)
        {
            screenB = GameObject.Find("ScreenB");
            if (screenB.transform.position == manager.GetComponent<AlloEyeManager>().viewerBDownPosition
                & manager.GetComponent<AlloEyeManager>().moveCode == 1)
            {
                walkBackCanvas = GameObject.Find("WalkBackInstructionsCanvas");
                walkBackCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().positionBCanvasPosition;
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.ViewerBDropped);
            }
            else if (screenB.transform.position == manager.GetComponent<AlloEyeManager>().viewerBDownPosition
                    & manager.GetComponent<AlloEyeManager>().moveCode == 2)
                 {
                    teleportBackCanvas = GameObject.Find("TeleportBackInstructionsCanvas");
                    teleportBackCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().positionBCanvasPosition;
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
    public class Question1Showing : FSMState
    {
        public GameObject q1Canvas;
        public GameObject yButton;
        public GameObject nButton;

        public Question1Showing()
        {
            stateID = StateID.Question1Showing;
        }
        public override void Reason(GameObject manager)
        {
            yButton = GameObject.Find("Yes1");
            nButton = GameObject.Find("No1");
            if (yButton.GetComponent<RegisterPress>().pressBool == true | nButton.GetComponent<RegisterPress>().pressBool == true)
            {
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.Question1Answered);
                q1Canvas = GameObject.Find("Question1");
                q1Canvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().objectDumpPosition;
                yButton.GetComponent<RegisterPress>().pressBool = false; //reset button for next trial
                nButton.GetComponent<RegisterPress>().pressBool = false; //reset button for next trial
            }
        }
        public override void Act(GameObject manager)
        {
            q1Canvas = GameObject.Find("Question1");
            q1Canvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().positionACanvasPosition;
        }
    }
    public class Question2Showing : FSMState
    {
        public GameObject q2Canvas;
        public GameObject walkBackCanvas;
        public GameObject teleportBackCanvas;
        public GameObject cubeButton;
        public GameObject sphereButton;
        public GameObject cylinderButton;
        public GameObject capsuleButton;

        public Question2Showing()
        {
            stateID = StateID.Question2Showing;
        }
        public override void Reason(GameObject manager)
        {
            cubeButton = GameObject.Find("CubeButton");
            sphereButton = GameObject.Find("SphereButton");
            cylinderButton = GameObject.Find("CylinderButton");
            capsuleButton = GameObject.Find("CapsuleButton");

            if (cubeButton.GetComponent<RegisterPress>().pressBool == true | sphereButton.GetComponent<RegisterPress>().pressBool == true
                | cylinderButton.GetComponent<RegisterPress>().pressBool == true | capsuleButton.GetComponent<RegisterPress>().pressBool == true)
            {
                //transition
                manager.GetComponent<AlloEyeManager>().SetTransition(Transition.Question2Answered);

                //remove canvases
                q2Canvas = GameObject.Find("Question2");
                q2Canvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().objectDumpPosition;
                walkBackCanvas = GameObject.Find("WalkBackInstructionsCanvas");
                walkBackCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().objectDumpPosition;
                teleportBackCanvas = GameObject.Find("TeleportBackInstructionsCanvas");
                teleportBackCanvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().objectDumpPosition;

                //reset buttons for next trial
                cubeButton.GetComponent<RegisterPress>().pressBool = false; 
                sphereButton.GetComponent<RegisterPress>().pressBool = false; 
                cylinderButton.GetComponent<RegisterPress>().pressBool = false; 
                capsuleButton.GetComponent<RegisterPress>().pressBool = false; 
            }
        }
        public override void Act(GameObject manager)
        {
            q2Canvas = GameObject.Find("Question2");
            q2Canvas.GetComponent<RectTransform>().localPosition = manager.GetComponent<AlloEyeManager>().positionACanvasPosition;
        }
    }
    public class TrialEndSwitch : FSMState
    {
        bool vD;
        bool vD2 = false;
        public GameObject table;

        public TrialEndSwitch()
        {
            stateID = StateID.TrialEndSwitch;
        }
        public override void Reason(GameObject manager)
        {
            bool vD = manager.GetComponent<AlloEyeManager>().viewingDone;
            if (manager.GetComponent<AlloEyeManager>().trialCounter < manager.GetComponent<AlloEyeManager>().trialLimit)
            {
                if (vD2 == false)
                {
                    manager.GetComponent<AlloEyeManager>().WaitTime(manager.GetComponent<AlloEyeManager>().trialEndWaitTime);
                    vD2 = true;
                }
                if (vD == true)
                {
                    //change independent variables - eventually need to find way to randomise with equal variation across conditions
                    //currently the variables simply switch in a set way
                    manager.GetComponent<AlloEyeManager>().moveCode++;
                    manager.GetComponent<AlloEyeManager>().objectShift = !manager.GetComponent<AlloEyeManager>().objectShift;
                    manager.GetComponent<AlloEyeManager>().tableRotation = !manager.GetComponent<AlloEyeManager>().tableRotation;

                    //transition
                    manager.GetComponent<AlloEyeManager>().SetTransition(Transition.TrialSwitched);

                    //reset bools
                    manager.GetComponent<AlloEyeManager>().firstViewOnce = false; 
                    manager.GetComponent<AlloEyeManager>().viewingDone = false;
                    vD2 = false;

                    //reset array
                    table = GameObject.Find("Table");
                    table.transform.eulerAngles = Vector3.zero;
                    //reset sphere
                }
            }
            else
            {
                //show canvas instructing participants that trial has ended and they can now take off the headset
            }

        }
        public override void Act(GameObject manager)
        {
           //do nothing
        }
    }
}
