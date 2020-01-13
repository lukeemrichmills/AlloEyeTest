using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ViveSR.anipal.Eye;
using System.Runtime.Serialization;
using UnityEngine.UI;
using System.IO;

namespace EyeTracking_lEC
{
    public class AlloEyeManager : MonoBehaviour
    {
        public readonly int ppt = 1; //ppt ID
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
        public int trialCounter;
        public int trialLimit = 3; //change this for when to stop
        public int moveCode = 1; //1 = move (walk); 2 = move (teleport); 3 = don't move (stick). Change to enum?
        public bool tableRotation = false; //false = no table rotation; true = table rotates.
        public bool objectShift = true; //true = an object moves; false = no objects move. 
        //public enum MoveCode { Walk, Teleport, Stick};   

        //define objects in array including table
        public GameObject arrayObject1;
        public GameObject arrayObject2;
        public GameObject arrayObject3;
        public GameObject arrayObject4;
        public GameObject arrayObject5;
        public float hamburgerY = 0.6026f;
        public List<GameObject> arrayObjectsPrePlace = new List<GameObject>();
        public List<GameObject> arrayObjectsPostPlace = new List<GameObject>();
        public List<GameObject> arrayObjectsFull = new List<GameObject>();
        public GameObject table;

        
        public readonly float minDistance = 0.23f; // separation distance between objects, tested by eye
        public float theta;

        public string dataFile;

        public SRanipal_EyeFocusSample_LEM sranipal;

        void Start()
        {
            trialCounter = 1;
            GameObject efa = GameObject.Find("EyeFocusArray");
            sranipal = efa.GetComponent<SRanipal_EyeFocusSample_LEM>();

            //create CSV file and headers
            dataFile = Application.dataPath + "/CSV/" + "Gaze_Data" + ppt + ".csv";
            StreamWriter writer = new StreamWriter(dataFile);
            writer.WriteLine("Time, Object, PosX, PosY, PosZ, ScaleX, ScaleY, ScaleZ, ColX, ColY, ColZ, " +
                                "Trial, ViewNo, ViewPos, Rot?, ObjShift?, ObjShifted, Q1, Q2 ");
            writer.Flush();
            Debug.Log("CSV file created");

            //get table
            table = GameObject.Find("Table");
            
            //get array objects and add to lists
            arrayObject1 = GameObject.Find("Donut");
            arrayObject2 = GameObject.Find("IceCream");
            arrayObject3 = GameObject.Find("Cake");
            arrayObject4 = GameObject.Find("Milk");
            arrayObject5 = GameObject.Find("Hamburger");

            arrayObjectsPrePlace.Add(arrayObject1);
            arrayObjectsPrePlace.Add(arrayObject2);
            arrayObjectsPrePlace.Add(arrayObject3);
            arrayObjectsPrePlace.Add(arrayObject4);
            arrayObjectsPrePlace.Add(arrayObject5);

            arrayObjectsFull.Add(arrayObject1);
            arrayObjectsFull.Add(arrayObject2);
            arrayObjectsFull.Add(arrayObject3);
            arrayObjectsFull.Add(arrayObject4);
            arrayObjectsFull.Add(arrayObject5);

            //calculations to find angle of movement
            GameObject A = GameObject.Find("TeleportPointA");
            GameObject B = GameObject.Find("TeleportPointB");
            float commonY = A.transform.position.y;
            Vector3 tableFloor = new Vector3(table.transform.position.x, commonY, table.transform.position.z);
            Vector3 a = tableFloor - A.transform.position;
            Vector3 b = tableFloor - B.transform.position;
            theta = Vector3.Angle(a, b);
            Debug.Log("angle: " + theta);

            ArrayRandomisation(arrayObjectsPrePlace, arrayObjectsPostPlace, table);
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
            q1.AddTransition(Transition.Question1AnsweredYes, StateID.Question2Showing);
            q1.AddTransition(Transition.Question1AnsweredNo, StateID.TrialEndSwitch);
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
        public void ArrayRandomisation(List<GameObject> prePlacedObjects, List<GameObject> placedObjects, GameObject table)
        {
            //put in object dump
            foreach (GameObject go in prePlacedObjects)
            {
                go.transform.position = objectDumpPosition;
            }

            GenerateRandomPositionOnTable(table, out Vector3 randPos);

            while (prePlacedObjects.Count > 0) //while still objects left to place
            {
                int randSelect = UnityEngine.Random.Range(0, prePlacedObjects.Count); //select a random object to place
                GameObject objectToPlace = prePlacedObjects[randSelect]; //save to another object
                prePlacedObjects.Remove(prePlacedObjects[randSelect]); //remove this from pre placement array

                //Placing Objects on Table
                int farEnoughCount = 0; //counter for how many objects are too close to the object being placed, default set to 0
                while (farEnoughCount < placedObjects.Count) //while the number of objects that are far enough away from the object being placed is less than the total number of objects on the table...
                {
                    farEnoughCount = 0; //reset counter with every new random position
                    GenerateRandomPositionOnTable(table, out randPos);

                    foreach (GameObject go in placedObjects) //for each object on the table
                    {
                        float d = Vector3.Distance(randPos, go.transform.position); //for debugging
                        if (Vector3.Distance(randPos, go.transform.position) > minDistance) //if the distance between random position and objects on table is greater than min distance
                        {
                            farEnoughCount++;
                        }
                    }
                }
                objectToPlace.transform.position = randPos; // place object
                placedObjects.Add(objectToPlace); //add to list of objects on table
            }
        }
        public void RandomObjectShift(List<GameObject> objectList, GameObject table)
        {
            int randIndex = UnityEngine.Random.Range(0, objectList.Count); //select a random object to shift
            GameObject objectToShift = objectList[randIndex]; //save to another object
            List<GameObject> objectsNotMoving = objectList; //create new list for objects not moving
            objectsNotMoving.Remove(objectsNotMoving[randIndex]);//remove shifting object from objects not moving (because it will move!)

            GenerateRandomPositionOnTable(table, out Vector3 randPos);

            int farEnoughCount = 0; //count for far enough away from each other object on the table
            while (farEnoughCount < objectsNotMoving.Count)
            {
                farEnoughCount = 0;
                //Vector2 randVector = (UnityEngine.Random.insideUnitCircle * 0.25f); //generate random 2D vector within 0.25 radius
                //Vector3 shiftVector = objectToShift.transform.position + new Vector3(randVector.x, 0f, randVector.y); //convert to 3D relative to object to move

                GenerateRandomPositionOnTable(table, out randPos);

                float d = Vector3.Distance(objectToShift.transform.position, randPos); //record this
                if (d >= 0.15f & d < 0.5) //range of distances 
                {
                    foreach (GameObject go in objectsNotMoving)
                    {
                        if (Vector3.Distance(randPos, go.transform.position) > minDistance)
                        {
                            farEnoughCount++;
                        }
                    }
                }
            }
            objectToShift.transform.position = randPos;
        }
        public void GenerateRandomPositionOnTable(GameObject table, out Vector3 randPos)
        {
            Vector3 tableTop = table.transform.position + (table.transform.up * table.transform.localScale.y);
            Debug.Log("table top : " + tableTop.ToString("F4"));
            //generate random position on table
            //Vector3 tableTop = new Vector3(table.transform.position.x, 0.6392f, table.transform.position.z);
            randPos = tableTop + (UnityEngine.Random.insideUnitSphere * (table.transform.localScale.x / 2 - 0.05f));
            randPos = new Vector3(randPos.x, tableTop.y, randPos.z); //fixed to top of table
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
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            calibrationButtonObject = GameObject.Find("CalibrationButton");

           if (calibrationButtonObject.GetComponent<CalibrationLaunch>().calibrated == true)
           {
                aem.SetTransition(Transition.CalibrationButtonPressed);
           }
        }
        public override void Act(GameObject manager)
        {
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            calibrationCanvas = GameObject.Find("CalibrationCanvas");
            calibrationCanvas.GetComponent<RectTransform>().localPosition = aem.positionACanvasPosition;
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
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            startButton = GameObject.Find("StartButton");
            if(startButton.GetComponent<RegisterPress>().pressBool == true)
            {
                aem.SetTransition(Transition.Instructions1OKPressed);
                startButton.GetComponent<RegisterPress>().pressBool = false; //reset button for next trial
            }       
        }
        public override void Act(GameObject manager)
        {
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            calibrationCanvas = GameObject.Find("CalibrationCanvas");
            firstInstructionsCanvas = GameObject.Find("FirstInstructionsCanvas");
            firstInstructionsCanvas.GetComponent<RectTransform>().localPosition = aem.positionACanvasPosition; 
            calibrationCanvas.GetComponent<RectTransform>().localPosition = aem.objectDumpPosition;
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
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            screenA = GameObject.Find("ScreenA");
            if(screenA.transform.position == aem.viewerAUpPosition)
            {
                aem.SetTransition(Transition.ViewerALifted);
            }
        }
        public override void Act(GameObject manager)
        {
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            firstInstructionsCanvas = GameObject.Find("FirstInstructionsCanvas");
            firstInstructionsCanvas.GetComponent<RectTransform>().localPosition = aem.objectDumpPosition;

            screenA = GameObject.Find("ScreenA");
            Vector3 upPosition = aem.viewerAUpPosition;
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
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            bool vD = aem.viewingDone;
            if (vD2 == false)
            {
                aem.WaitTime(aem.viewingTimeSeconds);
                vD2 = true;
            }
            if (vD == true)
            {
                aem.SetTransition(Transition.PositionAViewingComplete);
                aem.viewingDone = false;
                vD2 = false;
                
            }
        }
        public override void Act(GameObject manager)
        {
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            //collect data
            //get SRanipal_EyeFocusSample_LEM
            List<GazeData> dataList = aem.sranipal.gazeDataCollection;
            //get output of RegisterGaze

            //add to CSV alongside other data
            //StreamWriter writer = new StreamWriter(aem.dataFile);
            //writer.WriteLine("Time, Object, PosX, PosY, PosZ, ScaleX, ScaleY, ScaleZ, ColX, ColY, ColZ, " +
            //                    "Trial, ViewNo, ViewPos, Rot?, ObjShift?, ObjShifted, Q1, Q2 ");
            for (int i = 0; i < dataList.Count; i++)
            {
                string newLine = (dataList[i].time.ToString() + "," + dataList[i].objectID.ToString() + ","
                   + dataList[i].objPosX.ToString() + "," + dataList[i].objPosY.ToString() + "," + dataList[i].objPosZ.ToString() + ","
                   + dataList[i].objScaleX.ToString() + "," + dataList[i].objScaleY.ToString() + "," + dataList[i].objScaleZ.ToString() + ","
                   + dataList[i].colX.ToString() + "," + dataList[i].colY.ToString() + "," + dataList[i].colZ.ToString() + "," 
                   + aem.trialCounter.ToString() + "," + 1 + "," + "A" + "," + aem.tableRotation.ToString() + "," + 
                   aem.objectShift.ToString() + "," + objectThatShift + blank + blank);
                File.AppendAllText(aem.dataFile, newLine); // NEED TO WORK THIS OUT! 
            }
           
            //writer.Flush();
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
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            screenA = GameObject.Find("ScreenA");
            if (screenA.transform.position == aem.viewerADownPosition
                & aem.firstViewOnce == false) //viewer down AND haven't viewed before
            {
                aem.SetTransition(Transition.ViewerADropped); //transition to array adjustments
                aem.firstViewOnce = true; //mark first viewing as done
            }
            else if (screenA.transform.position == aem.viewerADownPosition) //viewer down (AND have viewed before)
            {
                aem.SetTransition(Transition.ViewerADropped2); //transition to questions
            }
        }
        public override void Act(GameObject manager)
        {
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            screenA = GameObject.Find("ScreenA");
            Vector3 downPosition = aem.viewerADownPosition;
            screenA.GetComponent<UpDown>().Dropping(downPosition);
        }
    }
    public class ArrayAdjustments : FSMState
    {
        public GameObject movingObject;
        public Vector3 shiftVector;
        public bool adjustmentsMade = false;
        public GameObject table;
        public List<GameObject> objectsNotMoving = new List<GameObject>();


        public ArrayAdjustments()
        {
            stateID = StateID.ArrayAdjustments;
        }
        public override void Reason(GameObject manager)
        {
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            if (adjustmentsMade == true)
            {
                aem.SetTransition(Transition.ArrayAdjusted);
            }
        }
        public override void Act(GameObject manager)
        {
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            //whether object shifts or not
            if (aem.objectShift == true)
            {
                table = GameObject.Find("Table");
                aem.RandomObjectShift(aem.arrayObjectsPostPlace, table);
                //collect data - could either put in RandomObjectShift or output data from the function and collect data here (or in separate function)
            }
            //whether table rotates or not
            if (aem.tableRotation == true)
            {
                //rotate table
                table = GameObject.Find("Table");
                table.transform.Rotate(0f, -aem.theta, 0f);
                //collect data
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
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            startButton = GameObject.Find("StartButton2");
            if (startButton.GetComponent<RegisterPress>().pressBool == true &
                aem.moveCode != 3)
            {
                aem.SetTransition(Transition.Instructions2BPressed);
                startButton.GetComponent<RegisterPress>().pressBool = false; //reset button for next trial
                secondInstructionsCanvas = GameObject.Find("SecondInstructionsCanvas");
                secondInstructionsCanvas.GetComponent<RectTransform>().localPosition = aem.objectDumpPosition;
            }
            else if(startButton.GetComponent<RegisterPress>().pressBool == true)
            {
                aem.SetTransition(Transition.Instructions2APressed);
                startButton.GetComponent<RegisterPress>().pressBool = false; //reset button for next trial
                secondInstructionsCanvas = GameObject.Find("SecondInstructionsCanvas");
                secondInstructionsCanvas.GetComponent<RectTransform>().localPosition = aem.objectDumpPosition;
            }
        }
        public override void Act(GameObject manager)
        {
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            secondInstructionsCanvas = GameObject.Find("SecondInstructionsCanvas");
            if (aem.moveCode != 3)
            {
                secondInstructionsCanvas.GetComponent<RectTransform>().localPosition = aem.positionBCanvasPosition;

                if (aem.moveCode == 1)
                {
                    walkInstructionsCanvas = GameObject.Find("WalkInstructionsCanvas");
                    walkInstructionsCanvas.GetComponent<RectTransform>().localPosition = aem.positionACanvasPosition;
                }
                else
                {
                    teleportInstructionsCanvas = GameObject.Find("TeleportInstructionsCanvas");
                    teleportInstructionsCanvas.GetComponent<RectTransform>().localPosition = aem.positionACanvasPosition;
                }
            }
            else
            {
                secondInstructionsCanvas.GetComponent<RectTransform>().localPosition = aem.positionACanvasPosition;
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
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            screenB = GameObject.Find("ScreenB");
            if (screenB.transform.position == aem.viewerBUpPosition)
            {
                aem.SetTransition(Transition.ViewerBLifted);
            }
        }
        public override void Act(GameObject manager)
        {
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            secondInstructionsCanvas = GameObject.Find("SecondInstructionsCanvas");
            secondInstructionsCanvas.GetComponent<RectTransform>().localPosition = aem.objectDumpPosition;
            walkInstructionsCanvas = GameObject.Find("WalkInstructionsCanvas");
            walkInstructionsCanvas.GetComponent<RectTransform>().localPosition = aem.objectDumpPosition;
            teleportInstructionsCanvas = GameObject.Find("TeleportInstructionsCanvas");
            teleportInstructionsCanvas.GetComponent<RectTransform>().localPosition = aem.objectDumpPosition;
            screenB = GameObject.Find("ScreenB");
            Vector3 upPosition = aem.viewerBUpPosition;
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
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            bool vD = aem.viewingDone;
            if (vD2 == false)
            {
                aem.WaitTime(aem.viewingTimeSeconds);
                vD2 = true;
            }
            if (vD == true)
            {
                aem.SetTransition(Transition.PositionBViewingComplete);
                aem.viewingDone = false;
                vD2 = false;
            }
        }
        public override void Act(GameObject manager)
        {
            //collect data
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
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            screenB = GameObject.Find("ScreenB");
            if (screenB.transform.position == aem.viewerBDownPosition
                & aem.moveCode == 1)
            {
                walkBackCanvas = GameObject.Find("WalkBackInstructionsCanvas");
                walkBackCanvas.GetComponent<RectTransform>().localPosition = aem.positionBCanvasPosition;
                aem.SetTransition(Transition.ViewerBDropped);
            }
            else if (screenB.transform.position == aem.viewerBDownPosition
                    & aem.moveCode == 2)
                 {
                    teleportBackCanvas = GameObject.Find("TeleportBackInstructionsCanvas");
                    teleportBackCanvas.GetComponent<RectTransform>().localPosition = aem.positionBCanvasPosition;
                    aem.SetTransition(Transition.ViewerBDropped);
                 }
        }
        public override void Act(GameObject manager)
        {
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            screenB = GameObject.Find("ScreenB");
            Vector3 downPosition = aem.viewerBDownPosition;
            screenB.GetComponent<UpDown>().Dropping(downPosition);
        }
    }
    public class Question1Showing : FSMState
    {
        public GameObject q1Canvas;
        public GameObject yButton;
        public GameObject nButton;
        public GameObject walkBackCanvas;
        public GameObject teleportBackCanvas;

        public Question1Showing()
        {
            stateID = StateID.Question1Showing;
        }
        public override void Reason(GameObject manager)
        {
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            yButton = GameObject.Find("Yes1");
            nButton = GameObject.Find("No1");
            if (yButton.GetComponent<RegisterPress>().pressBool == true)
            {
                aem.SetTransition(Transition.Question1AnsweredYes);
                q1Canvas = GameObject.Find("Question1");
                q1Canvas.GetComponent<RectTransform>().localPosition = aem.objectDumpPosition;
                yButton.GetComponent<RegisterPress>().pressBool = false; //reset button for next trial

                //record button press here
            }
            else if (nButton.GetComponent<RegisterPress>().pressBool == true)
            {
                aem.SetTransition(Transition.Question1AnsweredNo);
                q1Canvas = GameObject.Find("Question1");
                q1Canvas.GetComponent<RectTransform>().localPosition = aem.objectDumpPosition;
                nButton.GetComponent<RegisterPress>().pressBool = false; //reset button for next trial
                walkBackCanvas = GameObject.Find("WalkBackInstructionsCanvas");
                walkBackCanvas.GetComponent<RectTransform>().localPosition = aem.objectDumpPosition;
                teleportBackCanvas = GameObject.Find("TeleportBackInstructionsCanvas");
                teleportBackCanvas.GetComponent<RectTransform>().localPosition = aem.objectDumpPosition;
            }
        }
        public override void Act(GameObject manager)
        {
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            q1Canvas = GameObject.Find("Question1");
            q1Canvas.GetComponent<RectTransform>().localPosition = aem.positionACanvasPosition;
        }
    }
    public class Question2Showing : FSMState
    {
        public GameObject q2Canvas;
        public GameObject walkBackCanvas;
        public GameObject teleportBackCanvas;
        public GameObject button1;
        public GameObject button2;
        public GameObject button3;
        public GameObject button4;
        public GameObject button5;
        public List<GameObject> buttons = new List<GameObject>();

        public Question2Showing()
        {
            stateID = StateID.Question2Showing;
        }
        public override void Reason(GameObject manager)
        {
            button1 = GameObject.Find("IceCreamButton");
            bool b1Press = button1.GetComponent<RegisterPress>().pressBool;
            buttons.Add(button1);
            button2 = GameObject.Find("CakeButton");
            bool b2Press = button2.GetComponent<RegisterPress>().pressBool;
            buttons.Add(button1);
            button3 = GameObject.Find("HamburgerButton");
            bool b3Press = button3.GetComponent<RegisterPress>().pressBool;
            buttons.Add(button1);
            button4 = GameObject.Find("DonutButton");
            bool b4Press = button4.GetComponent<RegisterPress>().pressBool;
            buttons.Add(button1);
            button5 = GameObject.Find("MilkButton");
            bool b5Press = button5.GetComponent<RegisterPress>().pressBool;
            buttons.Add(button1);
           
            if (b1Press == true | b2Press == true | b3Press == true | b4Press == true | b5Press == true)
            {
                AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();

                //transition
                aem.SetTransition(Transition.Question2Answered);

                //remove canvases
                q2Canvas = GameObject.Find("Question2");
                q2Canvas.GetComponent<RectTransform>().localPosition = aem.objectDumpPosition;
                walkBackCanvas = GameObject.Find("WalkBackInstructionsCanvas");
                walkBackCanvas.GetComponent<RectTransform>().localPosition = aem.objectDumpPosition;
                teleportBackCanvas = GameObject.Find("TeleportBackInstructionsCanvas");
                teleportBackCanvas.GetComponent<RectTransform>().localPosition = aem.objectDumpPosition;

                
                //record button press
                foreach (GameObject b in buttons)
                {
                    if (b.GetComponent<RegisterPress>().pressBool == true)
                    {
                        //record here
                        //trial variables
                        //button press
                    }
                }

                //reset buttons for next trial - foreach loop doesn't work for some reason, neither does b1Press etc.
                button1.GetComponent<RegisterPress>().pressBool = false;
                button2.GetComponent<RegisterPress>().pressBool = false;
                button3.GetComponent<RegisterPress>().pressBool = false;
                button4.GetComponent<RegisterPress>().pressBool = false;
                button5.GetComponent<RegisterPress>().pressBool = false;
            }
        }
        public override void Act(GameObject manager)
        {
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            q2Canvas = GameObject.Find("Question2");
            q2Canvas.GetComponent<RectTransform>().localPosition = aem.positionACanvasPosition;
        }
    }
    public class TrialEndSwitch : FSMState
    {
        bool vD;
        bool vD2 = false;
        public GameObject table;
        public GameObject endCanvas;

        public TrialEndSwitch()
        {
            stateID = StateID.TrialEndSwitch;
        }
        public override void Reason(GameObject manager)
        {
            AlloEyeManager aem = manager.GetComponent<AlloEyeManager>();
            bool vD = aem.viewingDone;
            if (aem.trialCounter < aem.trialLimit)
            {
                if (vD2 == false)
                {
                    aem.WaitTime(aem.trialEndWaitTime);
                    vD2 = true;
                }
                if (vD == true)
                {
                    //change independent variables - eventually need to find way to randomise with equal variation across conditions
                    //currently the variables simply switch in a set way
                    aem.moveCode++;
                    aem.objectShift = !aem.objectShift;
                    aem.tableRotation = !aem.tableRotation;
                    aem.trialCounter++;

                    //transition
                    aem.SetTransition(Transition.TrialSwitched);

                    //reset bools
                    aem.firstViewOnce = false; 
                    aem.viewingDone = false;
                    vD2 = false;

                    //reset and randomise array
                    table = GameObject.Find("Table");
                    table.transform.eulerAngles = Vector3.zero;

                    aem.arrayObjectsPrePlace.Clear();
                    foreach (GameObject go in aem.arrayObjectsFull)
                    {
                        aem.arrayObjectsPrePlace.Add(go);
                    }
                    aem.arrayObjectsPostPlace.Clear();
                    aem.ArrayRandomisation(aem.arrayObjectsPrePlace, aem.arrayObjectsPostPlace, table);

                }
            }
            else
            {
                
                endCanvas = GameObject.Find("EndCanvas");
                endCanvas.GetComponent<RectTransform>().localPosition = aem.positionACanvasPosition;
            }

        }
        public override void Act(GameObject manager)
        {
           //do nothing
        }
    }
}
