using BCIEssentials.StimulusObjects;
using System.Collections;
using UnityEngine;
using System;
using System.Linq;
using BCIEssentials.Controllers;
using BCIEssentials.Utilities;
using Random = System.Random;

namespace BCIEssentials.ControllerBehaviors
{
    public class P300ControllerBehavior : BCIControllerBehavior
    {
        public override BCIBehaviorType BehaviorType => BCIBehaviorType.P300;
        
        public int numFlashesLowerLimit = 9;
        public int numFlashesUpperLimit = 12;
        public Random randNumFlashes = new Random();
        private int numFlashesPerObjectPerSelection = 3;

        public float onTime = 0.2f;
        public float offTime = 0.3f;

        public bool singleFlash = true;
        public bool multiFlash = false;

        public bool rowColumn = false;
        public bool checkerboard = true;
        public int checkerBoardRows = 5;
        public int checkerBoardCols = 6;

        public enum multiFlashMethod
        {
            Random
        };

        private float timeOfFlash = 0;
        private float timeOfWrite = 0;
        private float oldTimeOfWrite = 0;
        private float timeLag = 0;

        public bool timeDebug = false;

        private bool blockOutGoingLSL = false;

        public float trainBufferTime = 0f;


        protected override IEnumerator WhileDoAutomatedTraining()
        {
            numFlashesPerObjectPerSelection = randNumFlashes.Next(numFlashesLowerLimit, numFlashesUpperLimit);
            Debug.Log("Number of flashes is " + numFlashesPerObjectPerSelection.ToString());

            // Generate the target list
            PopulateObjectList();

            // Get number of selectable objects by counting the objects in the objectList
            int numOptions = _selectableSPOs.Count;

            // Create a random non repeating array 
            int[] trainArray = ArrayUtilities.GenerateRNRA(numTrainingSelections, 0, numOptions);
            LogArrayValues(trainArray);

            yield return null;

            //System.Random randNumFlashes = new System.Random();

            // Loop for each training target
            for (int i = 0; i < numTrainingSelections; i++)
            {
                numFlashesPerObjectPerSelection = randNumFlashes.Next(numFlashesLowerLimit, numFlashesUpperLimit);
                Debug.Log("Number of flashes is " + numFlashesPerObjectPerSelection.ToString());

                // Get the target from the array
                trainTarget = trainArray[i];

                // 
                Debug.Log("Running training selection " + i.ToString() + " on option " +
                          trainTarget.ToString());

                // Turn on train target

                _selectableSPOs[trainTarget].GetComponent<SPO>().OnTrainTarget();

                // Go through the training sequence
                yield return new WaitForSecondsRealtime(trainTargetPresentationTime);

                if (trainTargetPersistent == false)
                {
                    _selectableSPOs[trainTarget].GetComponent<SPO>().OffTrainTarget();
                }

                yield return new WaitForSecondsRealtime(0.5f);

                // Calculate the length of the trial
                float trialTime = (onTime + offTime) * (1f + (10f / Application.targetFrameRate)) *
                                  (float)numFlashesPerObjectPerSelection * (float)_selectableSPOs.Count;

                Debug.Log("This trial will take ~" + trialTime.ToString() + " seconds");

                StartStimulusRun(false);
                yield return new WaitForSecondsRealtime(trialTime);
                yield return new WaitForSecondsRealtime(trainBufferTime);
                //stimulusOff();

                // If sham feedback is true, then show it
                if (shamFeedback)
                {
                    _selectableSPOs[trainTarget].GetComponent<SPO>().Select();
                }

                // Turn off train target
                yield return new WaitForSecondsRealtime(0.5f);

                if (trainTargetPersistent == true)
                {
                    _selectableSPOs[trainTarget].GetComponent<SPO>().OffTrainTarget();
                }

                // Take a break
                yield return new WaitForSecondsRealtime(trainBreak);

                trainTarget = 99;
            }

            marker.Write("Training Complete");

        }

        protected override IEnumerator WhileDoUserTraining()
        {
            numFlashesPerObjectPerSelection = randNumFlashes.Next(numFlashesLowerLimit, numFlashesUpperLimit);
            Debug.Log("Number of flashes is " + numFlashesPerObjectPerSelection.ToString());

            blockOutGoingLSL = true;

            // Generate the target list
            PopulateObjectList();
            Debug.Log("User Training");

            // Get a random training target
            trainTarget = randNumFlashes.Next(0, _selectableSPOs.Count - 1);

            // Turn on train target

            _selectableSPOs[trainTarget].GetComponent<SPO>().OnTrainTarget();

            // Go through the training sequence
            yield return new WaitForSecondsRealtime(trainTargetPresentationTime);

            if (trainTargetPersistent == false)
            {
                _selectableSPOs[trainTarget].GetComponent<SPO>().OffTrainTarget();
            }

            yield return new WaitForSecondsRealtime(0.5f);

            // Calculate the length of the trial

            float trialTime = (onTime + offTime) * (1f + (10f / Application.targetFrameRate)) *
                              (float)numFlashesPerObjectPerSelection * (float)_selectableSPOs.Count;

            Debug.Log("This trial will take ~" + trialTime.ToString() + " seconds");

            StartStimulusRun(false);

            yield return new WaitForSecondsRealtime(trialTime);
            yield return new WaitForSecondsRealtime(trainBufferTime);
            //stimulusOff();

            // If sham feedback is true, then show it
            if (shamFeedback)
            {
                _selectableSPOs[trainTarget].GetComponent<SPO>().Select();
            }

            // Turn off train target
            yield return new WaitForSecondsRealtime(0.5f);

            if (trainTargetPersistent == true)
            {
                _selectableSPOs[trainTarget].GetComponent<SPO>().OffTrainTarget();
            }

            // Take a break
            yield return new WaitForSecondsRealtime(trainBreak);

            trainTarget = 99;

            Debug.Log("User Training Complete");

            blockOutGoingLSL = false;

            yield return null;
        }

        protected override IEnumerator OnStimulusRunBehavior()
        {
            numFlashesPerObjectPerSelection = randNumFlashes.Next(numFlashesLowerLimit, numFlashesUpperLimit);
            Debug.Log("Number of flashes is " + numFlashesPerObjectPerSelection.ToString());
            // numFlashesPerObjectPerSelection = randNumFlashes.Next(numFlashesLowerLimit, numFlashesUpperLimit);
            // UnityEngine.Debug.Log("Number of flashes is " + numFlashesPerObjectPerSelection.ToString());

            if (singleFlash)
            {
                int totalFlashes = numFlashesPerObjectPerSelection * _selectableSPOs.Count;
                int[] stimOrder = ArrayUtilities.GenerateRNRA(totalFlashes, 0, _selectableSPOs.Count);

                for (int i = 0; i < stimOrder.Length; i++)
                {
                    // 
                    GameObject currentObject = _selectableSPOs[stimOrder[i]]?.gameObject;

                    /////
                    //This block keeps taking longer and longer... maybe.... try timing it?
                    string markerString = "p300,s," + _selectableSPOs.Count.ToString();

                    if (trainTarget <= _selectableSPOs.Count)
                    {
                        markerString = markerString + "," + trainTarget.ToString();
                    }
                    else
                    {
                        markerString = markerString + "," + "-1";
                    }

                    markerString = markerString + "," + stimOrder[i].ToString();

                    ///
                    ////create and start a Stopwatch instance
                    //Stopwatch stopwatch = Stopwatch.StartNew();

                    //Turn on

                    timeOfFlash = currentObject.GetComponent<SPO>().StartStimulus();


                    //Send marker
                    if (blockOutGoingLSL == false)
                    {
                        marker.Write(markerString);
                    }

                    oldTimeOfWrite = timeOfWrite;
                    timeOfWrite = Time.time;
                    timeLag = timeOfWrite - oldTimeOfWrite;

                    if (timeDebug)
                    {
                        Debug.Log("write - write lag:" + timeLag.ToString());
                    }

                    //stopwatch.Stop();
                    //UnityEngine.Debug.Log(stopwatch.ElapsedMilliseconds.ToString());

                    //Wait
                    yield return new WaitForSecondsRealtime(onTime);

                    //Turn off
                    currentObject.GetComponent<SPO>().StopStimulus();

                    //Wait
                    yield return new WaitForSecondsRealtime(offTime);
                }
            }

            if (multiFlash)
            {
                // For multi flash selection, create virtual rows and columns
                int numSelections = _selectableSPOs.Count;
                int numColumns = (int)Math.Ceiling(Math.Sqrt((float)numSelections));
                int numRows = (int)Math.Ceiling((float)numSelections / (float)numColumns);

                int[,] rcMatrix = new int[numColumns, numRows];

                // Assign object indices to places in the virtual row/column matrix
                //if (rcMethod.ToString() == "Ordered")
                //{
                if (rowColumn)
                {
                    int count = 0;
                    for (int i = 0; i < numColumns; i++)
                    {
                        for (int j = 0; j < numRows; j++)
                        {
                            if (count <= numSelections)
                                rcMatrix[i, j] = count;
                            //print(i.ToString() + j.ToString() + count.ToString());
                            count++;
                        }
                    }

                    // Number of flashes per row/column
                    int totalColumnFlashes = numFlashesPerObjectPerSelection * numColumns;
                    int totalRowFlashes = numFlashesPerObjectPerSelection * numRows;

                    // Create a random order to flash rows and columns
                    int[] columnStimOrder = ArrayUtilities.GenerateRNRA(totalColumnFlashes, 0, numColumns);
                    int[] rowStimOrder = ArrayUtilities.GenerateRNRA(totalRowFlashes, 0, numRows);

                    for (int i = 0; i < totalColumnFlashes; i++)
                    {
                        //Initialize marker string
                        string markerString = "p300,m," + _selectableSPOs.Count.ToString();

                        //Add training target
                        if (trainTarget <= _selectableSPOs.Count)
                        {
                            markerString = markerString + "," + trainTarget.ToString();
                        }
                        else
                        {
                            markerString = markerString + "," + "-1";
                        }

                        // Turn on column 
                        int columnIndex = columnStimOrder[i];
                        for (int n = 0; n < numRows; n++)
                        {
                            _selectableSPOs[rcMatrix[n, columnIndex]]?.StartStimulus();
                            markerString = markerString + "," + rcMatrix[n, columnIndex];
                        }

                        //// Add train target to marker
                        //if (trainTarget <= objectList.Count)
                        //{
                        //    markerString = markerString + "," + trainTarget.ToString();
                        //}

                        // Send marker
                        if (blockOutGoingLSL == false)
                        {
                            marker.Write(markerString);
                        }

                        //Wait
                        yield return new WaitForSecondsRealtime(onTime);

                        //Turn off column
                        for (int n = 0; n < numRows; n++)
                        {
                            _selectableSPOs[rcMatrix[n, columnIndex]]?.StopStimulus();
                        }

                        //Wait
                        yield return new WaitForSecondsRealtime(offTime);

                        // Flash row if available
                        if (i <= totalRowFlashes)
                        {
                            //Initialize marker string
                            string markerString1 = "p300,m," + _selectableSPOs.Count.ToString();


                            // Add training target
                            if (trainTarget <= _selectableSPOs.Count)
                            {
                                markerString1 = markerString1 + "," + trainTarget.ToString();
                            }
                            else
                            {
                                markerString1 = markerString1 + "," + "-1";
                            }

                            // Turn on row
                            int rowIndex = rowStimOrder[i];
                            for (int m = 0; m < numColumns; m++)
                            {
                                //Turn on row
                                _selectableSPOs[rcMatrix[rowIndex, m]]?.StartStimulus();

                                //Add to marker
                                markerString1 = markerString1 + "," + rcMatrix[rowIndex, m];
                            }

                            ////Add train target to marker
                            //if (trainTarget <= objectList.Count)
                            //{
                            //    markerString1 = markerString1 + "," + trainTarget.ToString();
                            //}

                            //Send Marker
                            if (blockOutGoingLSL == false)
                            {
                                marker.Write(markerString1);
                            }

                            //Wait
                            yield return new WaitForSecondsRealtime(onTime);

                            //Turn off Row
                            for (int m = 0; m < numColumns; m++)
                            {
                                _selectableSPOs[rcMatrix[rowIndex, m]].StopStimulus();
                            }


                            //Wait
                            yield return new WaitForSecondsRealtime(offTime);
                        }
                    }
                }

                if (checkerboard)
                {
                    // get the size of the black/white matrices
                    double maxBWsize = Math.Ceiling(((float)checkerBoardRows * (float)checkerBoardCols) / 2f);

                    // get the number of rows and columns
                    int bwCols = (int)Math.Ceiling(Math.Sqrt(maxBWsize));
                    int bwRows = (int)Math.Ceiling(Math.Sqrt(maxBWsize));
                    // check if cbRows needs to match cbCols or if we can remove a row
                    if (maxBWsize < ((bwRows * bwCols) - bwRows))
                    {
                        bwRows = bwRows - 1;
                    }

                    int realBWSize = bwCols * bwRows;

                    int[] blackList = new int[realBWSize];
                    int[] whiteList = new int[realBWSize];

                    int blackCount = 0;
                    int whiteCount = 0;

                    Debug.Log("There are " + bwRows.ToString() + " rows and " + bwCols.ToString() +
                              " columns in the BW matrices");

                    Random rnd = new Random();
                    int[] shuffledArray = Enumerable.Range(0, _selectableSPOs.Count).OrderBy(c => rnd.Next()).ToArray();

                    // assign from CB to BW
                    for (int i = 0; i < _selectableSPOs.Count; i++)
                    {

                        // if there is an odd number of columns
                        if (checkerBoardCols % 2 == 1)
                        {
                            //evens assigned to black
                            if (shuffledArray[i] % 2 == 0)
                            {
                                blackList[blackCount] = shuffledArray[i];
                                blackCount++;
                            }
                            //odds assigned to white
                            else
                            {
                                whiteList[whiteCount] = shuffledArray[i];
                                whiteCount++;
                            }
                        }

                        // if there is an even number of columns
                        if (checkerBoardCols % 2 == 0)
                        {
                            //assigned to black
                            int numR = shuffledArray[i] / checkerBoardCols;
                            // print("to place" + shuffledArray[i].ToString());
                            // print("row number" + numR.ToString());

                            if (((shuffledArray[i] - (numR % 2)) % 2) == 0)
                            {
                                blackList[blackCount] = shuffledArray[i];
                                // print(shuffledArray[i] + " is black");
                                blackCount++;
                            }
                            //assigned to white
                            else
                            {
                                whiteList[whiteCount] = shuffledArray[i];
                                // print(shuffledArray[i] + " is white");
                                whiteCount++;
                            }
                        }

                    }

                    // set the remaining values to 99
                    while (whiteCount < realBWSize)
                    {
                        whiteList[whiteCount] = 99;
                        whiteCount++;
                    }

                    // set the remaining values to 99
                    while (blackCount < realBWSize)
                    {
                        blackList[blackCount] = 99;
                        blackCount++;
                    }

                    // Print the white and black indices
                    Debug.Log("blacks");
                    LogArrayValues(blackList);
                    Debug.Log("whites");
                    LogArrayValues(whiteList);

                    // reshape the black and white arrays to 2D
                    int[,] blackMat = new int[bwRows, bwCols];
                    int[,] whiteMat = new int[bwRows, bwCols];

                    int count = 0;
                    for (int i = 0; i < bwRows; i++)
                    {
                        for (int j = 0; j < bwCols; j++)
                        {
                            print(count.ToString());
                            blackMat[i, j] = blackList[count];
                            whiteMat[i, j] = whiteList[count];

                            count++;
                        }
                    }

                    int[] objectsToFlash = new int[bwCols];

                    // for flash count
                    for (int f = 0; f < numFlashesPerObjectPerSelection; f++)
                    {
                        // for black rows
                        for (int br = 0; br < bwRows; br++)
                        {
                            for (int c = 0; c < bwCols; c++)
                            {
                                objectsToFlash[c] = blackMat[br, c];
                            }

                            LogArrayValues(objectsToFlash);

                            //Initialize marker string
                            string markerString1 = "p300,m," + _selectableSPOs.Count.ToString();

                            // Add training target
                            if (trainTarget <= _selectableSPOs.Count)
                            {
                                markerString1 = markerString1 + "," + trainTarget.ToString();
                            }
                            else
                            {
                                markerString1 = markerString1 + "," + "-1";
                            }

                            // Turn on objects to flash
                            for (int fi = 0; fi < bwCols; fi++)
                            {
                                if (objectsToFlash[fi] != 99)
                                {
                                    //Turn on row
                                    _selectableSPOs[objectsToFlash[fi]].StartStimulus();

                                    //Add to marker
                                    markerString1 = markerString1 + "," + objectsToFlash[fi].ToString();
                                }
                            }

                            //Send Marker
                            if (blockOutGoingLSL == false)
                            {
                                marker.Write(markerString1);
                            }

                            //Wait
                            yield return new WaitForSecondsRealtime(onTime);

                            //Turn off objects to flash
                            for (int fi = 0; fi < bwCols; fi++)
                            {
                                if (objectsToFlash[fi] != 99)
                                {
                                    //Turn on row
                                    _selectableSPOs[objectsToFlash[fi]].StopStimulus();
                                }
                            }

                            //Wait
                            yield return new WaitForSecondsRealtime(offTime);

                        }

                        // for white rows
                        for (int wr = 0; wr < bwRows; wr++)
                        {
                            for (int c = 0; c < bwCols; c++)
                            {
                                objectsToFlash[c] = whiteMat[wr, c];
                            }

                            LogArrayValues(objectsToFlash);

                            //Initialize marker string
                            string markerString1 = "p300,m," + _selectableSPOs.Count.ToString();

                            // Add training target
                            if (trainTarget <= _selectableSPOs.Count)
                            {
                                markerString1 = markerString1 + "," + trainTarget.ToString();
                            }
                            else
                            {
                                markerString1 = markerString1 + "," + "-1";
                            }

                            // Turn on objects to flash
                            for (int fi = 0; fi < bwCols; fi++)
                            {
                                if (objectsToFlash[fi] != 99)
                                {
                                    //Turn on row
                                    _selectableSPOs[objectsToFlash[fi]].StartStimulus();

                                    //Add to marker
                                    markerString1 = markerString1 + "," + objectsToFlash[fi].ToString();
                                }
                            }

                            //Send Marker
                            if (blockOutGoingLSL == false)
                            {
                                marker.Write(markerString1);
                            }

                            //Wait
                            yield return new WaitForSecondsRealtime(onTime);

                            //Turn off objects to flash
                            for (int fi = 0; fi < bwCols; fi++)
                            {
                                if (objectsToFlash[fi] != 99)
                                {
                                    //Turn on row
                                    _selectableSPOs[objectsToFlash[fi]].StopStimulus();
                                }
                            }

                            //Wait
                            yield return new WaitForSecondsRealtime(offTime);
                        }

                        // for black columns
                        for (int bc = 0; bc < bwCols; bc++)
                        {
                            for (int r = 0; r < bwRows; r++)
                            {
                                objectsToFlash[r] = blackMat[r, bc];
                            }

                            LogArrayValues(objectsToFlash);

                            //Initialize marker string
                            string markerString1 = "p300,m," + _selectableSPOs.Count.ToString();

                            // Add training target
                            if (trainTarget <= _selectableSPOs.Count)
                            {
                                markerString1 = markerString1 + "," + trainTarget.ToString();
                            }
                            else
                            {
                                markerString1 = markerString1 + "," + "-1";
                            }

                            // Turn on objects to flash
                            for (int fi = 0; fi < bwRows; fi++)
                            {
                                if (objectsToFlash[fi] != 99)
                                {
                                    //Turn on row
                                    _selectableSPOs[objectsToFlash[fi]].StartStimulus();

                                    //Add to marker
                                    markerString1 = markerString1 + "," + objectsToFlash[fi].ToString();
                                }
                            }

                            //Send Marker
                            if (blockOutGoingLSL == false)
                            {
                                marker.Write(markerString1);
                            }

                            //Wait
                            yield return new WaitForSecondsRealtime(onTime);

                            //Turn off objects to flash
                            for (int fi = 0; fi < bwRows; fi++)
                            {
                                if (objectsToFlash[fi] != 99)
                                {
                                    //Turn on row
                                    _selectableSPOs[objectsToFlash[fi]].StopStimulus();
                                }
                            }

                            //Wait
                            yield return new WaitForSecondsRealtime(offTime);
                        }

                        // for white columns
                        for (int wc = 0; wc < bwCols; wc++)
                        {
                            for (int r = 0; r < bwRows; r++)
                            {
                                objectsToFlash[r] = whiteMat[r, wc];
                            }

                            LogArrayValues(objectsToFlash);

                            //Initialize marker string
                            string markerString1 = "p300,m," + _selectableSPOs.Count.ToString();

                            // Add training target
                            if (trainTarget <= _selectableSPOs.Count)
                            {
                                markerString1 = markerString1 + "," + trainTarget.ToString();
                            }
                            else
                            {
                                markerString1 = markerString1 + "," + "-1";
                            }

                            // Turn on objects to flash
                            for (int fi = 0; fi < bwRows; fi++)
                            {
                                if (objectsToFlash[fi] != 99)
                                {
                                    //Turn on row
                                    _selectableSPOs[objectsToFlash[fi]].StartStimulus();

                                    //Add to marker
                                    markerString1 = markerString1 + "," + objectsToFlash[fi].ToString();
                                }
                            }

                            //Send Marker
                            if (blockOutGoingLSL == false)
                            {
                                marker.Write(markerString1);
                            }

                            //Wait
                            yield return new WaitForSecondsRealtime(onTime);

                            //Turn off objects to flash
                            for (int fi = 0; fi < bwRows; fi++)
                            {
                                if (objectsToFlash[fi] != 99)
                                {
                                    //Turn on row
                                    _selectableSPOs[objectsToFlash[fi]].StopStimulus();
                                }
                            }

                            //Wait
                            yield return new WaitForSecondsRealtime(offTime);
                        }
                    }

                    //
                }
                //}


            }

            StopStimulusRun();
        }

        protected override IEnumerator SendMarkers(int trainingIndex = 99)
        {
            // Do nothing, markers are are temporally bound to stimulus and are therefore sent from stimulus coroutine
            yield return null;
        }

        // Turn the stimulus on
        public override void StartStimulusRun(bool sendConstantMarkers = true)
        {
            StimulusRunning = true;
            
            StimulusRunning = true;
            LastSelectedSPO = null;
            
            // Send the marker to start
            if (blockOutGoingLSL == false)
            {
                marker.Write("Trial Started");
            }

            ReceiveMarkers();
            PopulateObjectList();
            StopStartCoroutine(ref _runStimulus, RunStimulus());

            // Not required for P300
            if (sendConstantMarkers)
            {
                StopStartCoroutine(ref _sendMarkers, SendMarkers(trainTarget));
            }
        }

        public override void StopStimulusRun()
        {
            // End thhe stimulus Coroutine
            StimulusRunning = false;

            // Send the marker to end
            if (blockOutGoingLSL == false)
            {
                marker.Write("Trial Ends");
            }
        }
    }
}
