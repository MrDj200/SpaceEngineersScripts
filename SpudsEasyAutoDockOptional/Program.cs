using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        //



        /////////////////////////////////////////////////////
        //    Auto Docking
        //    
        //    Author:  Spug
        //    Debugged by gruntblender
        //    Please leave credit if the ship is
        //    used on the workshop.
        /////////////////////////////////////////////////////



        // Changeable variables:

        double maxSpeed = 8;                                //Limits the speed while moving to the connector to roughly this value.
        double acceleration = 4;                             // Acceleration of the approach to the connector. (Meters per second)
        double decceleration = 6;                           // Opposite to above, increase this if overshooting. (overshooting is caused by too much mass for the thruster power) (Meters per second)
        double verticalAcceleration = 1;                  // Acceleration downwards towards the docking port
        double verticalDecceleration = 1.2;             // Deceleration when moving towards the docking port
        double verticalApproachSpeed = 1;             // The relative speed at which the ship will attempt to approach at (no particular unit)
        double connectorClearance = 5;                 // The height the ship will rise to above the connector (Eg if there are trees of height 10, it could rise above them) (Minimum of 1m)
        double hangarHeight = 9999;                     // MUST BE at least 2m larger than connectorClearance. This is the height the ship will make sure it's below.
        double hangarCorrectionSpeed = 6;           // Speed at which it rises or lowers to get into the hangar.

        bool spinsWhenConnected = true;             // If true, the ship will rotate to match the connector's direction once it has docked.
        bool spinsBeforeConnecting = true;           // If true, the ship will rotate to match the connector's direction when it's moving down to dock in true Star Wars fashion.
        bool largeShipsSpinToo = false;                // If false, large ships won't spin no matter what the settings are above.
        bool onlySpinsClockwise = false;             // If true, while docked, the ship will only rotate clockwise instead of taking the shortest rotation adding extra coolness.
        double spinSpeedOnConnector = 2;          // Maximum speed the ship will rotate at on the connector.
        double maximumSuccessAngle = 0.025;  // The angle in radians at which the program determines it's successfully docked.
        double spinStartDistance = 4;                   // Distance away the ship will be when it begins to spin around.
        double runwaySuccessDistance = 4;       // The distance away the ship will be from the runway marker (excluding vertical distance) when it then goes to the next one.
        double maxRunwaySpeed = 7;                 // Rough value, might be slightly faster than this.

        string connectorTag = "[Dock]";                 // The tag on the name of the connector - must be unique for that ship. - can't be the same as the tag on the optional script.
        string antennaTag = "[Home]";                    // (Optional script) The tag on the name of the antenna - only relevent for using with the optional script. - can't be the same as the tag on the optional script.
        string timerblockTag = "[Dock]";                // Completely optional. A timer with this tag gets activated once docking is complete.
        string cockpitblockTag = "[Dock]";                // Completely optional. A cockpit/remote/flight seat with this tag gets prioritised as your main one if you're using a mod.
        string runwayArgumentTag = "Runway";  // Tag required in the argument for the ship to use the runway

        //////TECHNICAL Changeable variables:

        double spinAngle = 0.1;                              //Don't change unless you don't want your ship to spin round. Can cause issues.
        double maxConnectorDistanceX = 2;        // Maximum distance from the home connector the ship will be before it docks. (Measured by how lined up it is, eg 0.01 is almost perfectly in line)
        double gravityMultiplier = 1.05;                    // Gravity is artificially increased by this multiplier to make atmospheric landings more energetic and less like I'm hauling the ship up by it's toes.
        double retryDistance = 3;                          // If the ship is further from this distance it'll fly above connectorClearence and have another go.

        bool gridCheck = true;                               //Set to false if you want this to operate with multiple grids.
        float gyroSpeed = 10;

        bool multiplayerFix = false;                // If your ship is bobbing above the connector and not lowering, set this to true.
        double multiplayerFixPower = 0.1;              // How strong the fix is applied. (The maximum angle at which it deems it's at a correct angle)

        bool overrideAtmosphericSpinning = false;     //The script automatically turns off spinning before connector if the script detects it could be dangerous for the ship to spin. This overrides that.








        ////////////////////////////////////////////////////
        ////////////////////////////////////////////////////
        //                       
        //  DO NOT CHANGE BELOW v v v

        Vector3D homePos = new Vector3D(0, 0, 0);
        Vector3D homeDirection = new Vector3D(0, 0, 0);
        Vector3D homeUp = new Vector3D(0, 0, 0);

        List<Vector3D> homePositions = new List<Vector3D>();
        List<Vector3D> homeDirections = new List<Vector3D>();
        List<Vector3D> homeUps = new List<Vector3D>();
        List<string> homeNames = new List<string>();
        List<string> connectorNames = new List<string>();

        List<Vector3D> runwayPositions = new List<Vector3D>();
        List<Vector3D> runwayDirections = new List<Vector3D>();
        List<Vector3D> runwayForwards = new List<Vector3D>();
        string runwayHome = "";
        int currentRunwayMarker = 0;

        string homeConnectorName = "";
        string currentArg = "";

        IMyShipConnector myConnector;
        List<IMyShipConnector> myConnectors = new List<IMyShipConnector>();


        List<IMyThrust> thrusters = new List<IMyThrust>();
        List<IMyGyro> gyros = new List<IMyGyro>();

        Vector3 velocityVector = new Vector3();
        Vector3 lastPosition = new Vector3();
        Vector3 lastHomePosition = new Vector3(0, 0, 0);
        Vector3 additionalVelocityVector = new Vector3(0, 0, 0);

        DateTime lastCheck = DateTime.MinValue;
        DateTime lastCheckHome = DateTime.MinValue;
        IMyShipController cockpit = null;
        IMyRadioAntenna antenna = null;
        IMyBroadcastListener Listener;
        IMyTimerBlock timer = null;
        bool isLargeShip = false;

        double maxRotation = 0;

        bool docking = false;

        DateTime lastAntennaArg = DateTime.MinValue;

        public Program()
        {
            instVariables();
            Runtime.UpdateFrequency = UpdateFrequency.Once;
            stopThrust();
            maxRotation = 0;
            maxSpeed = maxSpeed * 0.77;
            maxRunwaySpeed = maxRunwaySpeed * 0.77;
        }

        void instVariables()
        {
            bool error = false;
            Listener = IGC.RegisterBroadcastListener(antennaTag);
            Listener.SetMessageCallback(antennaTag);
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);
            String Definition = Me.BlockDefinition.ToString();
            String[] DefinitionFragments = Definition.Split('/');
            //String BlockType = DefinitionFragments[0].Substring(DefinitionFragments[0].IndexOf("_") + 1);
            int BlockStrPos = DefinitionFragments[1].IndexOf("Block");
            String BlockSize = DefinitionFragments[1].Substring(0, BlockStrPos);
            if (BlockSize.Contains("Large"))
            {
                isLargeShip = true;
            }

            foreach (var block in blocks)
            {
                if (block is IMyShipConnector)
                {
                    if (block.CustomName.ToLower().Contains(connectorTag.ToLower()) && (Me.CubeGrid.CustomName == block.CubeGrid.CustomName || gridCheck == false))
                    {
                        myConnector = (IMyShipConnector)block;
                        if (gridCheck == false)
                        {
                            if (myConnector.Status == MyShipConnectorStatus.Connected)
                            {
                                Echo("GridCheck is off and you are connected to home, please disconnect and recompile\n");
                                myConnector = null;
                            }
                        }
                        if (myConnector != null)
                        {
                            myConnectors.Add(myConnector);
                        }

                    }
                    IMyShipConnector tempblock = (IMyShipConnector)block;
                }
                else if (block is IMyRadioAntenna)
                {
                    {
                        antenna = (IMyRadioAntenna)block;
                    }
                }
            }
            if (myConnector == null)
            {
                throw new Exception("\nNo connector found with\n" + connectorTag + " in the name.");
            }
            if (antenna != null)
            {
                Echo("Found antenna with tag " + antennaTag);
            }
            bool hasFoundCockpit = false;
            foreach (var block in blocks)
            {
                if (block is IMyShipController)
                {
                    if ((block.CubeGrid.ToString() == myConnector.CubeGrid.ToString() || gridCheck == false) && hasFoundCockpit == false)
                    {
                        cockpit = (IMyShipController)block;
                        if (block.CustomName.ToLower().Contains(cockpitblockTag.ToLower()))
                        {
                            hasFoundCockpit = true;
                            Echo("Found controller/remote with tag " + cockpitblockTag);
                        }
                    }
                }
                if (block is IMyTimerBlock)
                {
                    if ((block.CubeGrid.ToString() == Me.CubeGrid.ToString() || gridCheck == false) && block.CustomName.ToLower().Contains(timerblockTag.ToLower()))
                    {
                        timer = (IMyTimerBlock)block;
                        Echo("Found " + timer.CustomName + ".");
                    }
                }
            }
            if (cockpit == null)
            {
                Echo("No cockpit/remote found on board, could cause weight issues.");
            }


            runwayPositions = new List<Vector3D>();
            runwayDirections = new List<Vector3D>();
            runwayForwards = new List<Vector3D>();
            runwayHome = "";


            if (error == false)
            {
                if (Storage.Length > 1)
                {
                    int runwayCount = 0;
                    char mainDelimiter = '!';
                    string[] allMemories = Storage.Split(mainDelimiter);

                    foreach (var memory in allMemories)
                    {
                        char delimiter = ',';
                        string[] partsToMemory = memory.Split(delimiter);
                        if (partsToMemory[0] == "Runway" || partsToMemory[0] == "RunwayHome")
                        {
                            if (partsToMemory[0] == "Runway")
                            {
                                runwayCount += 1;
                                Vector3D runPos = new Vector3D();
                                Vector3D.TryParse(partsToMemory[1], out runPos);
                                runwayPositions.Add(runPos);
                                Vector3D runDi = new Vector3D();
                                Vector3D.TryParse(partsToMemory[2], out runDi);
                                runwayDirections.Add(runDi);
                                Vector3D runFor = new Vector3D(0, 0, 0);
                                if (partsToMemory.Length >= 4)
                                {
                                    Vector3D.TryParse(partsToMemory[3], out runFor);
                                }
                                runwayForwards.Add(runFor);
                            }
                            else
                            {
                                runwayHome = partsToMemory[1];
                            }
                        }
                        else
                        {
                            if (partsToMemory.Length == 4 | partsToMemory.Length == 5)
                            {
                                homePos = new Vector3D();
                                Vector3D.TryParse(partsToMemory[0], out homePos);
                                homePositions.Add(homePos);

                                homeDirection = new Vector3D();
                                Vector3D.TryParse(partsToMemory[1], out homeDirection);
                                homeDirections.Add(homeDirection);

                                homeUp = new Vector3D();
                                Vector3D.TryParse(partsToMemory[2], out homeUp);
                                homeUps.Add(homeUp);

                                homeConnectorName = partsToMemory[3];
                                connectorNames.Add(homeConnectorName);

                                if (partsToMemory.Length == 4)
                                {
                                    homeNames.Add("NOARG"); // MAY NOT ADD, COULD CAUSE INDEXING ISSUES
                                    Echo("\nAssociated: '" + homeConnectorName + "' with no argument");
                                }
                                else
                                {
                                    if (partsToMemory[4] == "")
                                    {
                                        partsToMemory[4] = "NOARG";
                                    }
                                    homeNames.Add(partsToMemory[4]);
                                    if (partsToMemory[4] == "NOARG")
                                    {
                                        Echo("\nAssociated: '" + homeConnectorName + "' with no argument");
                                    }
                                    else
                                    {
                                        Echo("\nAssociated: '" + homeConnectorName + "' with argument: " + partsToMemory[4]);
                                    }
                                }
                            }
                            else
                            {
                                Echo("Corrupted memory, maybe a comma used in a name");
                                homePos = new Vector3D(0, 0, 0);
                                homeDirection = new Vector3D(0, 0, 0);
                                homeUp = new Vector3D(0, 0, 0);
                                error = true;
                            }
                        }

                    }
                    if (error == false)
                    {
                        GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);
                        //disableGyroOverride();
                        foreach (var thruster in thrusters)
                        {
                            if ((thruster.CubeGrid.CustomName == Me.CubeGrid.CustomName || gridCheck == false))
                            {
                                thruster.SetValueFloat("Override", 0f);
                            }
                        }
                        if (runwayCount > 0)
                        {
                            Echo("\nFound " + runwayCount + " runway markers.");
                        }

                        Echo("\nReady to dock.");

                    }
                }
                else
                {
                    Echo("[WARNING] No Home Found.\nDock with a connector then run script");
                }

            }
        }

        public void reset()
        {
            homePos = new Vector3D(0, 0, 0);
            homeDirection = new Vector3D(0, 0, 0);
            homeUp = new Vector3D(0, 0, 0);
            homeConnectorName = "";
            stopThrust();

            homePositions.Clear();
            homeDirections.Clear();
            homeUps.Clear();
            homeNames.Clear();
            connectorNames.Clear();

            runwayPositions.Clear();
            runwayDirections.Clear();
            runwayForwards.Clear();
            runwayHome = "";

            maxRotation = 0;
            docking = false;
            Echo("Reset to have no home");
            Storage = "";
        }





        Vector3D GetConnectorApproach(IMyShipConnector connector, int distanceBlocks)
        {
            Vector3D coord = connector.CubeGrid.GridIntegerToWorld(connector.Position +
            Base6Directions.GetIntVector(connector.Orientation.Forward) * distanceBlocks);
            return coord;
        }
        Vector3D GetConnectorDirection(IMyShipConnector connector)
        {
            Vector3D v_con = connector.CubeGrid.GridIntegerToWorld(connector.Position);
            Vector3D v_app = GetConnectorApproach(connector, 1);
            Vector3D v_offset = v_app - v_con;
            Vector3D v_normalized = v_offset / Math.Sqrt(v_offset.X * v_offset.X + v_offset.Y * v_offset.Y + v_offset.Z * v_offset.Z);
            return v_normalized;
        }
        Vector3D GetConnectorApproach(IMyShipConnector connector, float distanceMeters)
        {
            Vector3D v_con = connector.CubeGrid.GridIntegerToWorld(connector.Position);
            Vector3D v_app = GetConnectorApproach(connector, 1);
            Vector3D v_offset = v_app - v_con;
            Vector3D v_normalized = v_offset / Math.Sqrt(v_offset.X * v_offset.X + v_offset.Y * v_offset.Y + v_offset.Z * v_offset.Z);
            Vector3D coord = v_con + v_normalized;
            return coord;
        }


        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update10)
            {
                if (docking == false)
                {
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    stopThrust();
                    Echo("Stopped Docking.\n\nAwaiting orders.");


                    maxRotation = 0;
                    if (myConnector.Status == MyShipConnectorStatus.Connectable && (homePos - myConnector.GetPosition()).Length() < 5)
                    {
                        myConnector.Connect();
                        sendMessage("LANDED");
                        if (timer != null)
                        {
                            timer.GetActionWithName("Start").Apply(timer);
                        }
                    }
                    else
                    {
                        sendMessage("ABORTED");
                    }
                    if (myConnector.Status == MyShipConnectorStatus.Connected)
                    {
                        if (timer != null)
                        {
                            timer.GetActionWithName("Start").Apply(timer);
                        }
                    }

                }
                else
                {

                    manageMovement();



                }
            }
            else if (updateSource == UpdateType.Trigger | updateSource == UpdateType.Terminal | updateSource == UpdateType.IGC | updateSource == UpdateType.Script)
            {
                if (updateSource == UpdateType.IGC & Listener.HasPendingMessage)
                {
                    MyIGCMessage Message = Listener.AcceptMessage();
                    if (Message.Tag == antennaTag & Message.Data is string) argument = Message.Data.ToString();
                }
                if ((argument == "reset" | argument == "Reset") && updateSource != UpdateType.IGC)
                {
                    reset();
                }
                if (docking == true)
                {
                    if (updateSource != UpdateType.IGC)
                    {
                        docking = false;
                    }
                }
                else if (argument != "reset" && argument != "Reset")
                {
                    if (myConnector.Status == MyShipConnectorStatus.Connected)
                    {
                        if (argument.ToLower() == "nearest")
                        {
                            Echo("WARNING:\nThe argument nearest\nis used to dock to the\nnearest connector, please\nuse a different home name.");
                        }
                        else
                        {
                            if (argument == "")
                            {
                                findAndSetConnector("NOARG");
                            }
                            else
                            {
                                findAndSetConnector(argument);
                            }
                        }

                        stopThrust();
                    }
                    else// if (tooSoonCatch == false)
                    {
                        // Start Dock Sequence
                        int indexOfIntendedDock = -1;
                        bool errored = false;
                        if (homePositions.Count > 0)
                        {
                            if (argument.ToLower() == "nearest")
                            {
                                Vector3 myPos = myConnector.GetPosition();
                                double closestDist = 9999999999;
                                for (int i = 0; i < homePositions.Count; i++)
                                {
                                    double dist = Vector3D.Distance(homePositions[i], myPos);
                                    if (dist < closestDist)
                                    {
                                        indexOfIntendedDock = i;
                                        closestDist = dist;
                                    }
                                }
                                if (indexOfIntendedDock == -1)
                                {
                                    Echo("WARNING:\nFinding nearest connector\nfailed.");
                                }
                                else
                                {
                                    Echo("Docking to nearest connector.");
                                }
                            }
                            else
                            {
                                for (int i = 0; i < homePositions.Count; i++)
                                {
                                    if (homeNames[i] == argument || (argument == "" && homeNames[i] == "") || (argument == "" && homeNames[i] == "NOARG"))
                                    {
                                        if (homePositions[i] != new Vector3D(0, 0, 0) && homeDirections[i] != new Vector3D(0, 0, 0) && homeUps[i] != new Vector3D(0, 0, 0))
                                        {
                                            if (indexOfIntendedDock != -1)
                                            {
                                                Echo("Multiple docks found for that argument.\nPicking the latest one.");
                                            }
                                            indexOfIntendedDock = i;
                                        }
                                    }

                                }
                            }
                        }
                        else
                        {
                            errored = true;
                            Echo("[WARNING] No Home Found\nDock with a connector then run script");
                        }

                        if (indexOfIntendedDock != -1)
                        {
                            Echo("Docking to connector: '" + connectorNames[indexOfIntendedDock] + "'");
                            homePos = homePositions[indexOfIntendedDock];
                            homeDirection = homeDirections[indexOfIntendedDock];
                            homeConnectorName = connectorNames[indexOfIntendedDock];
                            homeUp = homeUps[indexOfIntendedDock];
                            currentRunwayMarker = 0;

                            currentArg = homeNames[indexOfIntendedDock];
                            docking = true;

                            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);
                            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);
                            lastHomePosition = new Vector3(0, 0, 0);
                            checkGrav();
                            Runtime.UpdateFrequency = UpdateFrequency.Update10;
                            maxRotation = 0;
                        }
                        else
                        {
                            if (errored == false)
                            {
                                Echo("[WARNING] No Home Found Associated with that argument.\nDock with a connector then run script");
                            }
                        }
                    }
                }
            }
            if (argument.Contains("Pos,"))
            {
                Char delimiter = ',';
                String[] argArr = argument.Split(delimiter);
                if (argArr.Length == 5 && argArr[1] == Me.CubeGrid.CustomName + currentArg)
                {
                    Vector3D.TryParse(argArr[2], out homePos);
                    Vector3D.TryParse(argArr[3], out homeDirection);
                    Vector3D.TryParse(argArr[4], out homeUp);

                    var stamp2 = DateTime.Now;
                    if (lastHomePosition != new Vector3(0, 0, 0) && (lastCheckHome != DateTime.MinValue) && (stamp2 - lastCheckHome).TotalMilliseconds > 50)
                    {
                        var elapsedTime = (stamp2 - lastCheckHome).TotalMilliseconds;
                        additionalVelocityVector = (homePos - lastHomePosition) / (float)elapsedTime;

                        lastHomePosition = homePos;
                    }

                    lastHomePosition = homePos;
                    lastCheckHome = stamp2;
                }
                else if (argArr.Length != 5)
                {
                    Echo("Warning, couldn't find home, ensure both scripts are up to date");
                }



            }
            else if (argument == "Connector not found")
            {
                Echo("WARNING,\n the home connector hasn't been found,\n check to make sure it's name hasn't\n been changed or the whole thing removed");
            }
        }

        public void sendMessage(string message)
        {
            if (antenna != null)
            {
                string oString = message + "," + homeConnectorName + "," + Me.CubeGrid.CustomName + "," + currentArg;
            }
        }

        public void requestDockPosition()
        {
            string oString = "DockRequest," + homeConnectorName + "," + Me.CubeGrid.CustomName + currentArg;
            //Echo(Me.CubeGrid.CustomName);
            IGC.SendBroadcastMessage(antennaTag, oString);
        }

        public void findAndSetConnector(string arg)
        {
            List<IMyShipConnector> Connectors = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(Connectors);
            double closestDist = 999999;
            IMyShipConnector closestConnector = null;
            foreach (var connector in Connectors)
            {
                if (myConnector.CubeGrid.ToString() != connector.CubeGrid.ToString())
                {
                    Vector3 myConnectorPos = myConnector.GetPosition();
                    Vector3 otherConnectorPos = connector.GetPosition();

                    double dist = Vector3.Distance(myConnectorPos, otherConnectorPos);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestConnector = connector;
                    }

                }

            }
            if (closestConnector == null)
            {
                throw new Exception("\nSomething went wrong when finding the connector.\nReconnect and try again.\n(Check you've attatched with the " + connectorTag + " connector)");

            }
            else
            {
                if (arg == "")
                {
                    arg = "NOARG";
                }

                if (arg.ToLower().Contains(runwayArgumentTag.ToLower()))
                {
                    checkRunway();
                }


                homePos = closestConnector.GetPosition();
                homeDirection = GetConnectorDirection(closestConnector);
                homeConnectorName = closestConnector.CustomName;
                homeUp = closestConnector.CubeGrid.GridIntegerToWorld(closestConnector.Position + Base6Directions.GetIntVector(closestConnector.Orientation.Up));
                homeUp = connectorDirectionOut(homeDirection, homeUp, homePos, homeConnectorName);
                // fwd = 0 + directional
                // up = pos + directional
                // pos = pos
                // OUTPUT = pos + directional
                if (homeNames.Count > 0)
                {
                    bool updated = false;
                    for (int i = 0; i < homeNames.Count; i++)
                    {
                        if (homeNames[i] == arg)
                        {
                            homePositions[i] = homePos;
                            homeDirections[i] = homeDirection;
                            homeUps[i] = homeUp;
                            homeNames[i] = arg;
                            connectorNames[i] = homeConnectorName;
                            Echo("Updated existing association.\n");
                            updated = true;
                        }

                    }
                    if (updated == false)
                    {
                        homePositions.Add(homePos);
                        homeDirections.Add(homeDirection);
                        homeUps.Add(homeUp);
                        homeNames.Add(arg);
                        connectorNames.Add(homeConnectorName);
                    }

                }
                else
                {
                    homePositions.Add(homePos);
                    homeDirections.Add(homeDirection);
                    homeUps.Add(homeUp);
                    homeNames.Add(arg);
                    connectorNames.Add(homeConnectorName);
                }



                //List<Vector3D> homeDirections = new List<Vector3D>();
                //List<Vector3D> homeUps = new List<Vector3D>();
                //List<string> homeNames = new List<string>();
                //List<string> connectorNames = new List<string>();
                if (arg == "" || arg == "NOARG")
                {
                    Echo("Associated connector\n'" + closestConnector.CustomName + "'\nwith no argument.");
                }
                else
                {
                    Echo("Associated connector\n'" + closestConnector.CustomName + "'\nwith the argument:\n" + arg);
                }

                //if (homePositions.Count == 0)
                //{
                //    Storage = homePos.ToString() + "," + homeDirection.ToString() + "," + homeUp.ToString() + "," + homeConnectorName + "," + arg;
                //}
                string currentString = "";
                for (int i = 0; i < homeNames.Count; i++)
                {
                    if (i > 0)
                    {
                        currentString = currentString + "!" + homePositions[i].ToString() + "," + homeDirections[i].ToString() + "," + homeUps[i].ToString() + "," + connectorNames[i] + "," + homeNames[i];
                    }
                    else
                    {
                        currentString = homePositions[0].ToString() + "," + homeDirections[0].ToString() + "," + homeUps[0].ToString() + "," + connectorNames[0] + "," + homeNames[0];
                    }
                }
                if (runwayPositions.Count > 0)
                {
                    for (int i = 0; i < runwayPositions.Count; i++)
                    {
                        currentString = currentString + "!Runway," + runwayPositions[i].ToString() + "," + runwayDirections[i].ToString() + "," + runwayForwards[i].ToString();
                    }
                    if (runwayHome != "")
                    {
                        currentString = currentString + "!RunwayHome," + runwayHome;
                    }
                }
                //runwayPositions.Clear();
                //runwayDirections.Clear();
                //runwayHome = "";

                Storage = currentString;

                //Echo(homeNames.Count().ToString());
                //Echo(Storage);
                //Echo(myConnector.CubeGrid.ToString());
            }

        }

        Vector3 connectorDirectionOut(Vector3 fwd, Vector3 up, Vector3 pos, string connName)
        {
            // fwd = 0 + directional
            // up = pos + directional
            // pos = pos
            // OUTPUT = pos + directional
            connName = connName.ToLower();
            if (connName.Contains("[") && connName.Contains("]"))
            {
                int fbracketIndex = connName.LastIndexOf("[");
                int lbracketIndex = connName.LastIndexOf("]");
                if (lbracketIndex > fbracketIndex)
                {
                    string centralText = connName.Substring(fbracketIndex + 1, lbracketIndex - fbracketIndex - 1);
                    double num;
                    if (double.TryParse(centralText, out num) && "[" + centralText + "]" != antennaTag && "[" + centralText + "]" != connectorTag && "[" + centralText + "]" != timerblockTag)
                    {
                        Vector3 upNorm = up - pos;

                        Echo("\nFound a " + num + " degree rotation");

                        num = (num / 360) * Math.PI * -2;

                        // Rotate 
                        VRageMath.Matrix rotationMatrix = VRageMath.Matrix.CreateFromAxisAngle(fwd, (float)num);
                        Vector3D outUp = VRageMath.Vector3D.Transform((Vector3D)upNorm, rotationMatrix);
                        return pos + (Vector3)outUp;
                    }
                    else
                    {
                        return up;
                    }
                }
                else
                {
                    return up;
                }
            }
            else
            {
                return up;
            }
        }



        public void checkRunway()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);

            List<IMyTerminalBlock> successBlocks = new List<IMyTerminalBlock>();
            List<double> successBlockPositions = new List<double>();
            int count = 0;

            runwayPositions.Clear();
            runwayDirections.Clear();
            runwayForwards.Clear();
            runwayHome = "";

            foreach (var block in blocks)
            {
                if (block.CustomName.ToLower().Contains("[") && block.CustomName.ToLower().Contains("]") && block is IMyTextPanel)
                {
                    string name = block.CustomName.ToLower();
                    int fbracketIndex = name.LastIndexOf("[");
                    int lbracketIndex = name.LastIndexOf("]");
                    if (lbracketIndex > fbracketIndex)
                    {
                        string centralText = name.Substring(fbracketIndex + 1, lbracketIndex - fbracketIndex - 1);
                        double num;
                        if (double.TryParse(centralText, out num) && "[" + centralText + "]" != antennaTag && "[" + centralText + "]" != connectorTag && "[" + centralText + "]" != timerblockTag)
                        {
                            successBlocks.Add(block);
                            successBlockPositions.Add(num);
                            count += 1;
                        }
                    }
                    //runwayPositions.Clear();
                    //runwayDirections.Clear();
                    //runwayHome = "";
                }
            }

            List<IMyTerminalBlock> orderedBlocks = new List<IMyTerminalBlock>();
            List<double> orderedPositions = new List<double>();
            int total = successBlocks.Count;
            for (int i1 = 0; i1 < total; i1++)
            {
                if (successBlocks.Count > 0)
                {
                    double lowestAmount = 999999;
                    int lowestIndex = 0;
                    for (int i = 0; i < successBlockPositions.Count; i++)
                    {
                        if (successBlockPositions[i] < lowestAmount)
                        {
                            lowestIndex = i;
                            lowestAmount = successBlockPositions[i];
                        }
                    }
                    orderedPositions.Add(successBlockPositions[lowestIndex]);
                    orderedBlocks.Add(successBlocks[lowestIndex]);
                    successBlocks.RemoveAt(lowestIndex);
                    successBlockPositions.RemoveAt(lowestIndex);
                }
            }
            for (int i = 0; i < orderedPositions.Count; i++)
            {
                runwayPositions.Add(orderedBlocks[i].GetPosition());

                Vector3 direction = orderedBlocks[i].CubeGrid.GridIntegerToWorld(orderedBlocks[i].Position + (Base6Directions.GetIntVector(orderedBlocks[i].Orientation.Forward) * -1));
                Vector3 normDirection = normalize(direction - orderedBlocks[i].GetPosition());
                runwayDirections.Add(normDirection);


                Vector3 direction2 = orderedBlocks[i].CubeGrid.GridIntegerToWorld(orderedBlocks[i].Position + (Base6Directions.GetIntVector(orderedBlocks[i].Orientation.Up)));
                //Vector3 normDirection2 = normalize(direction2 - orderedBlocks[i].GetPosition());
                runwayForwards.Add(direction2);
            }
            runwayHome = orderedBlocks[0].CubeGrid.ToString();

            if (count > 0)
            {
                Echo("Set runway. Found " + count + " markers.");
            }


        }


        public void manageMovement()
        {
            double distanceToAdj = 9999;
            bool isFinalConnector = true;
            Vector3 posOut = homePos;
            Vector3 dirOut = homeDirection;
            if (currentArg.ToLower().Contains(runwayArgumentTag.ToLower()))
            {
                int totalCount = runwayPositions.Count;


                if (currentRunwayMarker < totalCount)
                {
                    posOut = runwayPositions[currentRunwayMarker];
                    dirOut = runwayDirections[currentRunwayMarker];
                    isFinalConnector = false;
                }

                double distance = dock(posOut, dirOut, isFinalConnector);
                align(posOut, dirOut);

                if (isFinalConnector)
                {
                    distanceToAdj = distance;
                }
                else
                {
                    if (distance < runwaySuccessDistance)
                    {
                        currentRunwayMarker += 1;
                    }
                }
            }
            else
            {
                distanceToAdj = dock(homePos, homeDirection, true);
                align(homePos, homeDirection);
            }


            if (isLargeShip && largeShipsSpinToo == false)
            {
                spinsBeforeConnecting = false;
                spinsWhenConnected = false;
            }

            if (antenna != null)
            {
                requestDockPosition();
            }


            bool hasSpun = false;

            if (spinsBeforeConnecting)
            {
                if (currentRunwayMarker > 0 && isFinalConnector == false)
                {
                    Vector3 targetDirection = runwayForwards[currentRunwayMarker - 1];
                    if (targetDirection != new Vector3(0, 0, 0))
                    {
                        hasSpun = true;
                        spin(targetDirection, posOut, distanceToAdj);//targetDirection
                    }
                }
            }



            if (spinsBeforeConnecting)
            {


                if (distanceToAdj < spinStartDistance && (myConnector.Status != MyShipConnectorStatus.Connectable || distanceToAdj > maxConnectorDistanceX))
                {
                    spin(homeUp, homePos, distanceToAdj);
                    hasSpun = true;
                }

                if (myConnector.Status == MyShipConnectorStatus.Connectable && distanceToAdj < maxConnectorDistanceX)
                {

                    if (spinsWhenConnected == false)
                    {
                        docking = false;
                        myConnector.Connect();
                    }
                    else if (hasSpun == false)
                    {
                        spin(homeUp, homePos, distanceToAdj);
                    }
                }
            }
            else
            {
                if (myConnector.Status == MyShipConnectorStatus.Connectable && distanceToAdj < maxConnectorDistanceX)
                {

                    if (spinsWhenConnected == false)
                    {
                        docking = false;
                        myConnector.Connect();
                    }
                    else
                    {

                        spin(homeUp, homePos, distanceToAdj);
                    }

                }
            }
        }




        public double dock(Vector3 targetPos, Vector3 targetDi, bool isFinalDock)
        {

            Vector3 myDir = myConnector.GetPosition();
            Vector3 myPos = myConnector.CubeGrid.GridIntegerToWorld(myConnector.Position - Base6Directions.GetIntVector(myConnector.Orientation.Forward));
            Vector3 myUp = myConnector.CubeGrid.GridIntegerToWorld(myConnector.Position - Base6Directions.GetIntVector(myConnector.Orientation.Forward) + Base6Directions.GetIntVector(myConnector.Orientation.Up));
            Vector3 myRight = myConnector.CubeGrid.GridIntegerToWorld(myConnector.Position - Base6Directions.GetIntVector(myConnector.Orientation.Forward) + (Base6Directions.GetIntVector(myConnector.Orientation.Left) * -1));


            Vector3 adjustmentPoint = new Vector3();


            double dist = Vector3.Distance(myPos, targetPos);
            Vector3 directionToMe = (myPos - targetPos);

            Vector3 currentDirection = normalize(myDir - myPos);
            Vector3 targetDirection = normalize(targetDi);
            double rotationAngle = angleBetween(currentDirection, (targetDirection * -1));

            double angle = angleBetween(directionToMe, targetDi);
            double distToAdjustmentPoint = dist * Math.Cos(angle);
            adjustmentPoint = targetPos + ((Vector3D)targetDi * distToAdjustmentPoint);






            Vector3 ToAdj = adjustmentPoint - myPos;


            LocalCoords myLocal = new LocalCoords(myPos, myUp, myDir);

            Vector3 localVector = myLocal.GetLocalVector(adjustmentPoint);


            var stamp = DateTime.Now;
            if (lastCheck != DateTime.MinValue && (stamp - lastCheck).TotalMilliseconds > 100)
            {
                var elapsedTime = (stamp - lastCheck).TotalMilliseconds;
                velocityVector.X = (float)(myPos.X - lastPosition.X) / (float)elapsedTime;
                velocityVector.Y = (float)(myPos.Y - lastPosition.Y) / (float)elapsedTime;
                velocityVector.Z = (float)(myPos.Z - lastPosition.Z) / (float)elapsedTime;
                velocityVector = velocityVector * 1000;
            }
            lastPosition = myPos;
            lastCheck = stamp;
            Vector3 localVelocity = myLocal.GetLocalVector(myPos + velocityVector);
            Vector3 localTarget = myLocal.GetLocalVector(targetPos);
            double distanceToAdj = Vector3.Distance(myPos, adjustmentPoint);


            float mass = 850000;
            if (cockpit != null)
            {
                var Masses = cockpit.CalculateShipMass();
                //mass = Masses.TotalMass;
                //mass = Masses.BaseMass;
                mass = Masses.PhysicalMass;
                //Echo(Masses.ToString());
                //Echo("tot: " + mass.ToString());
            }
            //mass = mass - 25000;//(float)manualMassIncrease;

            double force = acceleration * (double)mass;
            double massConstant = (double)mass * decceleration;

            //if (localVector.X > maxSpeed)
            //{
            //    localVector.X = (float)maxSpeed;
            //}
            //else if (localVector.X < -maxSpeed)
            //{
            //    localVector.X = -(float)maxSpeed;
            //}
            //if (localVector.Y > maxSpeed)
            //{
            //    localVector.Y = (float)maxSpeed;
            //}
            //else if (localVector.Y < -maxSpeed)
            //{
            //    localVector.Y = -(float)maxSpeed;
            //}
            double len = localVector.Length();

            if (len > (float)maxSpeed * 2)
            {
                localVector = normalize(localVector) * (float)maxSpeed * 2;
            }
            //Echo(localVector.Length().ToString());

            if (isFinalDock == false)
            {
                localVector = normalize(localVector) * (float)maxRunwaySpeed * 2;
            }
            double finalThrustX = (localVector.X * force) - (localVelocity.X * massConstant); //+ (additionalVelocityVector.X * mass * 15);
            double finalThrustY = (localVector.Y * force) - (localVelocity.Y * massConstant);// + (additionalVelocityVector.Y * mass * 15);


            //if (isFinalDock == false)
            //{
            //    finalThrustX = (localVector.X * force) - (localVelocity.X * massConstant * 0.2);
            //    finalThrustY = (localVector.Y * force) - (localVelocity.Y * massConstant * 0.2);
            //}



            foreach (var thruster in thrusters)
            {
                thruster.SetValueFloat("Override", 0f);
            }


            bool countION = true;

            Vector3 resultantGravity = new Vector3(0, 0, 0);
            if (cockpit != null)
            {
                Vector3 gravity = cockpit.GetNaturalGravity() * gravityMultiplier;
                float gravStrength = gravity.Length();
                if (gravStrength > 0.1)
                {
                    Vector3 localGravity = myLocal.GetLocalVector(myPos + gravity);
                    //Vector3 localGravity = myLocal.GetLocalVector(gravity);
                    resultantGravity = localGravity;
                }
                if (gravStrength > 3)
                {
                    countION = false;
                }
            }






            if ((rotationAngle < spinAngle) && (localTarget.Z > connectorClearance || distanceToAdj < 3) && localTarget.Z < hangarHeight)
            {
                setThrust(new Vector3(0, 0, 1), (float)finalThrustX + (resultantGravity.X * mass * 1), countION);
                setThrust(new Vector3(0, -1, 0), (float)finalThrustY + (resultantGravity.Y * mass * -1), countION);
            }


            double downMaxSpeed = verticalApproachSpeed * 3;

            double moveDistance = 10;
            if (localTarget.Z < 11)
            {
                moveDistance = 10;
            }
            if (localTarget.Z < 7)
            {
                moveDistance = 1;
                downMaxSpeed = verticalApproachSpeed * 2;
            }
            if (localTarget.Z < 5)
            {
                moveDistance = 0.5;
                downMaxSpeed = verticalApproachSpeed * 1;
            }
            if (localTarget.Z < 3)
            {
                moveDistance = 0.3;
            }

            if (localTarget.Z > 17)
            {
                moveDistance = 35;
                //downMaxSpeed = verticalApproachSpeed * 3;
            }
            if (localTarget.Z < 10 && isFinalDock == false)
            {
                moveDistance = 0;
                downMaxSpeed = 1;
            }
            if (downMaxSpeed > maxSpeed)
            {
                downMaxSpeed = maxSpeed;
            }

            bool tooLow = false;
            bool tooHigh = false;
            if (localTarget.Z < connectorClearance && distanceToAdj > retryDistance)
            {
                tooLow = true;
            }
            if (localTarget.Z > hangarHeight)
            {
                tooHigh = true;
            }
            if (hangarHeight < connectorClearance)
            {
                Echo("WARNING:\nhangarHeight < connectorClearance");
                tooHigh = false;
            }



            // If Close to the finishing connector
            if (distanceToAdj < moveDistance && rotationAngle < spinAngle && tooLow == false && tooHigh == false)
            {
                double targetDownSpeed = downMaxSpeed - ((distanceToAdj / moveDistance) * downMaxSpeed);
                double currentDownSpeed = localVelocity.Z;
                if (currentDownSpeed > targetDownSpeed)
                {
                    setThrust(new Vector3(1, 0, 0), ((float)(currentDownSpeed - targetDownSpeed) * (float)targetDownSpeed * mass * -(float)verticalDecceleration) + (resultantGravity.Z * mass * -1), countION);
                }
                else
                {
                    setThrust(new Vector3(1, 0, 0), ((float)(targetDownSpeed - currentDownSpeed) * (float)targetDownSpeed * mass * (float)verticalAcceleration) + (resultantGravity.Z * mass * -1), countION);
                }
            }
            else if (rotationAngle < spinAngle && tooLow == false && tooHigh == false) // If away from the finishing connector
            {
                if (localVelocity.Z > 0)
                {
                    setThrust(new Vector3(1, 0, 0), (5 * mass * -1) + (resultantGravity.Z * mass * -1), countION);
                }
            }
            double hangarBuffer = 0.1;

            if ((tooLow && distanceToAdj > retryDistance) || localTarget.Z > hangarHeight - hangarBuffer) // If below connector clearance
            {
                double maxSpeed = hangarCorrectionSpeed;
                double currentDownSpeed = -localVelocity.Z;
                if (localTarget.Z > hangarHeight - hangarBuffer)
                {
                    maxSpeed = -maxSpeed;
                }
                if (tooLow && distanceToAdj > retryDistance)
                {
                    if (currentDownSpeed > maxSpeed)
                    {
                        setThrust(new Vector3(1, 0, 0), ((float)(currentDownSpeed - maxSpeed) * mass * (float)verticalDecceleration) + (resultantGravity.Z * mass * -1), countION);
                    }
                    else
                    {
                        setThrust(new Vector3(1, 0, 0), ((float)(maxSpeed - currentDownSpeed) * -(float)verticalAcceleration * mass) + (resultantGravity.Z * mass * -1), countION);
                    }
                }
                else
                {
                    if (currentDownSpeed > maxSpeed)
                    {
                        setThrust(new Vector3(1, 0, 0), ((float)(currentDownSpeed - maxSpeed) * mass * (float)verticalDecceleration) + (resultantGravity.Z * mass * -1), countION);
                    }
                    else
                    {
                        setThrust(new Vector3(1, 0, 0), ((float)(maxSpeed - currentDownSpeed) * mass * -(float)verticalAcceleration) + (resultantGravity.Z * mass * -1), countION);
                    }
                }


            }
            return distanceToAdj;

        }

        public void align(Vector3 targetPos, Vector3 targetDi)
        {

            Vector3 myDir = myConnector.GetPosition();
            Vector3 myPos = myConnector.CubeGrid.GridIntegerToWorld(myConnector.Position - Base6Directions.GetIntVector(myConnector.Orientation.Forward));
            Vector3 myUp = myConnector.CubeGrid.GridIntegerToWorld(myConnector.Position - Base6Directions.GetIntVector(myConnector.Orientation.Forward) + Base6Directions.GetIntVector(myConnector.Orientation.Up));
            Vector3 myRight = myConnector.CubeGrid.GridIntegerToWorld(myConnector.Position - Base6Directions.GetIntVector(myConnector.Orientation.Forward) + (Base6Directions.GetIntVector(myConnector.Orientation.Left) * -1));

            LocalCoords myLocal = new LocalCoords(myPos, myUp, myDir);

            Vector3 currentDirection = normalize(myDir - myPos);
            Vector3 targetDirection = normalize(targetDi);

            Vector3 crossCurrentTarget = crossProduct(currentDirection, targetDirection);
            crossCurrentTarget = normalize(crossCurrentTarget);

            Vector3 upDirection = normalize(myUp - myPos);
            double rollAngle = angleBetween(upDirection, crossCurrentTarget);
            int rollDirection = AngleDir(upDirection, crossCurrentTarget, currentDirection);



            double mainAngle = angleBetween((currentDirection * -1), targetDirection);
            float directionOfRoll = AngleDir((currentDirection * -1), targetDirection, normalize(myRight - myPos)) * -1;

            Vector3 rightDirection = normalize(myRight - myPos);
            Vector3 cross2 = crossProduct(normalize(myUp - myPos), targetDirection);
            double yawAngle = angleBetween(rightDirection, cross2);
            float directionOfYaw = AngleDir(rightDirection, cross2, upDirection);

            mainAngle = (mainAngle - yawAngle) * directionOfRoll; //

            float speed = gyroSpeed;

            if (multiplayerFix == false || (multiplayerFix == true && mainAngle < multiplayerFixPower))
            {
                foreach (var gyro in gyros)
                {
                    gyro.SetValueFloat("Yaw", 0);
                    gyro.SetValueFloat("Pitch", 0);
                    gyro.SetValueFloat("Roll", 0);
                    gyro.GyroOverride = true;
                }

                if (Math.Abs(yawAngle) > 0.01)
                {
                    setYaw(directionOfYaw * (float)yawAngle * speed);
                }

                setPitch((float)mainAngle * speed);
            }
            else
            {
                foreach (var gyro in gyros)
                {
                    gyro.SetValueFloat("Yaw", 0);
                    gyro.SetValueFloat("Pitch", 0);
                    gyro.SetValueFloat("Roll", 0);
                    gyro.GyroOverride = true;
                }
            }
        }

        public void spin(Vector3 targetUp, Vector3 targetHome, double distanceToAdj)
        {
            Vector3 myDir = myConnector.GetPosition();
            Vector3 myPos = myConnector.CubeGrid.GridIntegerToWorld(myConnector.Position - Base6Directions.GetIntVector(myConnector.Orientation.Forward));
            Vector3 myUp = myConnector.CubeGrid.GridIntegerToWorld(myConnector.Position - Base6Directions.GetIntVector(myConnector.Orientation.Forward) + Base6Directions.GetIntVector(myConnector.Orientation.Up));

            myUp = connectorDirectionOut(myDir - myPos, myUp, myPos, myConnector.CustomName);

            LocalCoords myLocal = new LocalCoords(myPos, myUp, myDir);

            Vector3 forwardDirection = normalize(myDir - myPos);
            Vector3 currentDirection = normalize(myUp - myPos);
            Vector3 targetDirection = normalize(targetUp - targetHome);
            double rotationAngle = angleBetween(currentDirection, targetDirection);

            if (rotationAngle > maxRotation)
            {
                maxRotation = rotationAngle;
            }

            int angleDirection = AngleDir(currentDirection, targetDirection, forwardDirection);
            double finalRotation = rotationAngle * angleDirection;
            bool hasRotated = (maxRotation > Math.PI * 0.4 && rotationAngle < 0.2);
            if (onlySpinsClockwise == true && hasRotated == false)
            {
                finalRotation = Math.Abs(finalRotation);
                if (angleDirection * rotationAngle > -0.1 && angleDirection * rotationAngle < 0)
                {
                    finalRotation = finalRotation * -1;
                }
            }
            double maxSpin = spinSpeedOnConnector;
            if (finalRotation > maxSpin)
            {
                finalRotation = maxSpin;

            }
            if (finalRotation < -maxSpin)
            {
                finalRotation = -maxSpin;
            }

            Vector3 resultantGravity = new Vector3(0, 0, 0);
            if (cockpit != null)
            {
                Vector3 gravity = cockpit.GetNaturalGravity();
                float gravStrength = gravity.Length();
                if (gravStrength > 0.1)
                {
                    Vector3 localGravity = myLocal.GetLocalVector(myPos + gravity);
                    resultantGravity = localGravity;
                }

            }
            if ((Math.Abs(resultantGravity.X) > 3 | Math.Abs(resultantGravity.Y) > 3) && myConnector.Status == MyShipConnectorStatus.Connectable)
            {
                Echo("Disabling spin as natural gravity is found while docking sideways");
                docking = false;
            }
            else
            {
                setRoll((float)(finalRotation * 8));
                if (Math.Abs(rotationAngle) < maximumSuccessAngle & myConnector.Status == MyShipConnectorStatus.Connectable && distanceToAdj < maxConnectorDistanceX)
                {
                    docking = false;
                }
            }




        }

        //homePos
        //homeDirection
        //homeConnectorName
        //homeUp
        public void checkGrav()
        {
            if (cockpit != null)
            {
                var Masses = cockpit.CalculateShipMass();
                double mass = Masses.BaseMass;

                Vector3 gravity = cockpit.GetNaturalGravity() * gravityMultiplier;
                float gravStrength = gravity.Length();
                if (gravStrength > 3)
                {
                    Vector3 myDir = homePos + homeDirection;
                    Vector3 myPos = homePos; //myConnector.CubeGrid.GridIntegerToWorld(myConnector.Position - Base6Directions.GetIntVector(myConnector.Orientation.Forward));
                    Vector3 myUp = homeUp; // myConnector.CubeGrid.GridIntegerToWorld(myConnector.Position - Base6Directions.GetIntVector(myConnector.Orientation.Forward) + Base6Directions.GetIntVector(myConnector.Orientation.Up));
                                           ////Vector3 myRight = myConnector.CubeGrid.GridIntegerToWorld(myConnector.Position - Base6Directions.GetIntVector(myConnector.Orientation.Forward) + (Base6Directions.GetIntVector(myConnector.Orientation.Left) * -1));

                    LocalCoords gridLocal = new LocalCoords(myPos, myUp, myDir);

                    Vector3 localGrav = gridLocal.GetLocalVector(gravity + myPos);

                    if (localGrav.X > 3 || localGrav.Y > 3)
                    {
                        if (overrideAtmosphericSpinning == true)
                        {
                            Echo("\nOverriden spin before connector.");
                        }
                        else
                        {
                            spinsBeforeConnecting = false;
                            Echo("\nWARNING:\nSpin before connecting has been auto disabled.\nThis is due to an atmosphere being detected and\na horizontal connector setup.\nOverride is in settings.");
                        }

                    }
                }
            }
        }


        public void disableGyroOverride()
        {
            foreach (var gyro in gyros)
            {
                gyro.SetValueFloat("Yaw", 0);
                gyro.SetValueFloat("Pitch", 0);
                gyro.SetValueFloat("Roll", 0);
                gyro.GyroOverride = false;
            }
        }

        public void setRoll(float roll)
        {
            Vector3 forwardVector = Base6Directions.GetIntVector(myConnector.Orientation.Forward);
            foreach (var gyro in gyros)
            {
                gyro.GyroOverride = true;
                Vector3 gyroForward = Base6Directions.GetIntVector(gyro.Orientation.Forward);
                Vector3 gyroUpward = Base6Directions.GetIntVector(gyro.Orientation.Up);
                Vector3 gyroRight = Base6Directions.GetIntVector(gyro.Orientation.Left) * -1;

                if (gyroForward == forwardVector)
                {
                    gyro.SetValueFloat("Roll", roll);
                }
                else if (gyroForward == (forwardVector * -1))
                {
                    gyro.SetValueFloat("Roll", roll * -1);
                }
                else if (gyroUpward == (forwardVector * -1))
                {
                    gyro.SetValueFloat("Yaw", roll);
                }
                else if (gyroUpward == forwardVector)
                {
                    gyro.SetValueFloat("Yaw", roll * -1);
                }
                else if (gyroRight == forwardVector)
                {
                    gyro.SetValueFloat("Pitch", roll);
                }
                else if (gyroRight == (forwardVector * -1))
                {
                    gyro.SetValueFloat("Pitch", roll * -1);
                }

            }
        }

        public void setPitch(float pitch)
        {

            Vector3 rightVector = Base6Directions.GetIntVector(myConnector.Orientation.Left) * -1;

            foreach (var gyro in gyros)
            {
                gyro.GyroOverride = true;
                Vector3 gyroForward = Base6Directions.GetIntVector(gyro.Orientation.Forward);
                Vector3 gyroUp = Base6Directions.GetIntVector(gyro.Orientation.Up);
                Vector3 gyroRight = Base6Directions.GetIntVector(gyro.Orientation.Left) * -1;

                if (gyroRight == (rightVector * -1))
                {
                    gyro.SetValueFloat("Pitch", pitch);
                }
                else if (gyroRight == rightVector)
                {
                    gyro.SetValueFloat("Pitch", pitch * -1);
                }
                else if (gyroUp == rightVector)
                {
                    gyro.SetValueFloat("Yaw", pitch);
                }
                else if (gyroUp == (rightVector * -1))
                {
                    gyro.SetValueFloat("Yaw", pitch * -1);
                }
                else if (gyroForward == (rightVector * -1))
                {
                    gyro.SetValueFloat("Roll", pitch);
                }
                else if (gyroForward == rightVector)
                {
                    gyro.SetValueFloat("Roll", pitch * -1);
                }

            }
        }

        public void setYaw(float yaw)
        {

            Vector3 upVector = Base6Directions.GetIntVector(myConnector.Orientation.Up);

            foreach (var gyro in gyros)
            {
                gyro.GyroOverride = true;
                Vector3 gyroForward = Base6Directions.GetIntVector(gyro.Orientation.Forward);
                Vector3 gyroUp = Base6Directions.GetIntVector(gyro.Orientation.Up);
                Vector3 gyroRight = Base6Directions.GetIntVector(gyro.Orientation.Left) * -1;

                if (gyroUp == (upVector * -1))
                {
                    gyro.SetValueFloat("Yaw", yaw);
                }
                else if (gyroUp == upVector)
                {
                    gyro.SetValueFloat("Yaw", yaw * -1);
                }
                else if (gyroRight == upVector)
                {
                    gyro.SetValueFloat("Pitch", yaw);
                }
                else if (gyroRight == (upVector * -1))
                {
                    gyro.SetValueFloat("Pitch", yaw * -1);
                }
                else if (gyroForward == upVector)
                {
                    gyro.SetValueFloat("Roll", yaw);
                }
                else if (gyroForward == (upVector * -1))
                {
                    gyro.SetValueFloat("Roll", yaw * -1);
                }

            }
        }



        string GPSString(Vector3 vectorIn, string name)
        {
            //GPS:New:-49.79:44.94:-104.12:
            string output = "GPS:" + name + ":" + vectorIn.X + ":" + vectorIn.Y + ":" + vectorIn.Z + ":";
            return output;
        }

        Vector3 normalize(Vector3 vectorIn)
        {
            Vector3 vectorOut = new Vector3();
            double dist = Math.Sqrt((vectorIn.X * vectorIn.X) + (vectorIn.Y * vectorIn.Y) + (vectorIn.Z * vectorIn.Z));
            vectorOut.X = (float)(vectorIn.X / dist);
            vectorOut.Y = (float)(vectorIn.Y / dist);
            vectorOut.Z = (float)(vectorIn.Z / dist);
            return vectorOut;
        }

        int AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
        {
            Vector3 perp = crossProduct(fwd, targetDir);
            float dir = Vector3.Dot(perp, up);

            if (dir > 0.0)
            {
                return 1;
            }
            else if (dir < 0.0)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        double angleBetween(Vector3 vector1, Vector3 vector2)
        {
            Vector3 nvector1 = normalize(vector1);
            Vector3 nvector2 = normalize(vector2);
            double dot = (nvector1.X * nvector2.X) + (nvector1.Y * nvector2.Y) + (nvector1.Z * nvector2.Z);
            double angOut = Math.Acos(dot);

            return angOut;
        }
        Vector3 crossProduct(Vector3 a, Vector3 b)
        {

            double x = (a.Y * b.Z) - (b.Y * a.Z);
            double y = (b.X * a.Z) - (a.X * b.Z);
            double z = (a.X * b.Y) - (b.X * a.Y);

            return new Vector3(x, y, z);
        }

        public void stopThrust()
        {
            disableGyroOverride();
            foreach (var thruster in thrusters)
            {
                thruster.SetValueFloat("Override", 0f);
            }
        }

        public void setThrust(Vector3 direction, float value, bool IONon)
        {
            Vector3 thrustDirection = new Vector3();

            if (direction == new Vector3(-1, 0, 0))
                thrustDirection = Base6Directions.GetIntVector(myConnector.Orientation.Forward);
            else if (direction == new Vector3(0, 1, 0))
                thrustDirection = Base6Directions.GetIntVector(myConnector.Orientation.Up);
            else if (direction == new Vector3(0, -1, 0))
                thrustDirection = Base6Directions.GetIntVector(myConnector.Orientation.Up) * -1;
            else if (direction == new Vector3(0, 0, 1))
                thrustDirection = Base6Directions.GetIntVector(myConnector.Orientation.Left) * -1;
            else if (direction == new Vector3(0, 0, -1))
                thrustDirection = Base6Directions.GetIntVector(myConnector.Orientation.Left);
            else
                thrustDirection = Base6Directions.GetIntVector(myConnector.Orientation.Forward) * -1;
            int count = 0;
            float totalThrust = 0;


            if (value < 0)
            {
                thrustDirection = thrustDirection * -1;
                value = Math.Abs(value);
            }

            foreach (var thruster in thrusters)
            {
                Vector3 actualThrustDirection = Base6Directions.GetIntVector(thruster.Orientation.Forward);
                if (thrustDirection == actualThrustDirection)
                {
                    String Definition = thruster.BlockDefinition.ToString();
                    String[] DefinitionFragments = Definition.Split('/');
                    int BlockStrPos = DefinitionFragments[1].IndexOf("Block");
                    String SubType = DefinitionFragments[1].Substring(BlockStrPos + 5);
                    if (SubType != "SmallThrust" && SubType != "LargeThrust" && IONon == false && thruster.Enabled) // && thruster.Enabled
                    {
                        count += 1;
                        totalThrust += thruster.MaxThrust; //GetValueFloat("MaxThrust");
                    }
                    else if (IONon == true)
                    {
                        if (thruster.Enabled)
                        {
                            count += 1;
                            totalThrust += thruster.MaxThrust;
                        }
                    }
                }

            }

            foreach (var thruster in thrusters)
            {
                Vector3 actualThrustDirection = Base6Directions.GetIntVector(thruster.Orientation.Forward);
                if (thrustDirection == actualThrustDirection)
                {
                    //thruster.SetValueFloat("Override", value / count);
                    if (IONon == false)
                    {
                        String Definition = thruster.BlockDefinition.ToString();
                        String[] DefinitionFragments = Definition.Split('/');
                        int BlockStrPos = DefinitionFragments[1].IndexOf("Block");
                        String SubType = DefinitionFragments[1].Substring(BlockStrPos + 5);
                        if (SubType != "SmallThrust" && SubType != "LargeThrust" && thruster.Enabled)
                        {
                            thruster.SetValueFloat("Override", value * (thruster.MaxThrust / totalThrust));
                        }
                    }
                    else
                    {
                        if (thruster.Enabled)
                        {
                            thruster.SetValueFloat("Override", value / count);//* (thruster.MaxThrust / totalThrust));// * (thruster.MaxThrust / totalThrust));
                        }
                    }
                }
            }

            //totalThrust += thruster.MaxThrust;

            //    else if((thrustDirection * -1) == actualThrustDirection)
            //{
            //    thruster.SetValueFloat("Override", 0);
            //}

        }



        //https://www.reddit.com/r/spaceengineers/comments/30vcxr/programming_script_to_translate_gps_points/

        ////////////////////////////////////////////////
        //Matrix, TransformMatrix, LocalCoords classes
        //version 1.0 3-30-2015
        //author: YenRaven
        //You can use/modify/distribute however you like,
        //just please give credit.
        ////////////////////////////////////////////////


        public class Matrix
        {
            private uint MyRows;
            private uint MyCols;
            private double[][] MyData;
            public uint Rows
            {
                get
                {
                    return MyRows;
                }
            }
            public uint Cols
            {
                get
                {
                    return MyCols;
                }
            }
            public Matrix(uint dimensions)
            {
                //Start with idenity matrix
                dimensions++;
                MyRows = dimensions;
                MyCols = dimensions;
                MyData = new double[MyCols][];
                for (var i = 0; i < MyCols; i++)
                {
                    MyData[i] = new double[MyRows];
                }
                SetToIdenity();
            }
            public Matrix(uint Cols, uint Rows)
            {
                // Start with identity matrix;
                MyRows = Rows;
                MyCols = Cols;
                MyData = new double[MyCols][];
                for (var i = 0; i < MyCols; i++)
                {
                    MyData[i] = new double[MyRows];
                }
                SetToIdenity();
            }
            public Matrix(double[][] data)
            {
                MyData = data;
            }
            public void SetToIdenity()
            {
                for (int x = 0; x < MyCols; x++)
                {
                    for (int y = 0; y < MyRows; y++)
                    {
                        if (y == x + (MyRows - MyCols))
                        {
                            MyData[x][y] = 1;
                        }
                        else
                        {
                            MyData[x][y] = 0;
                        }
                    }
                }
            }
            public double GetValue(uint x, uint y)
            {
                return MyData[x][y];
            }
            public void SetValue(uint x, uint y, double val)
            {
                MyData[x][y] = val;
            }
            public Matrix Multiply(Matrix ToMultiply)
            {
                Matrix Result = new Matrix(ToMultiply.Cols, MyRows);
                for (uint x = 0; x < Result.Cols; x++)
                {
                    for (uint y = 0; y < Result.Rows; y++)
                    {
                        double val = 0;
                        for (uint c = 0; c < MyCols; c++)
                        {
                            val += MyData[c][y] * ToMultiply.GetValue(x, c);
                        }
                        Result.SetValue(x, y, val);
                    }
                }
                return Result;
            }
        }
        public class TransformMatrix3D
        {
            private double RotX;
            private double RotY;
            private double RotZ;
            private double TranslateX;
            private double TranslateY;
            private double TranslateZ;
            private Matrix RotXMatrix;
            private Matrix RotYMatrix;
            private Matrix RotZMatrix;
            private Matrix RotMatrix;
            private Matrix TranslateMatrix;
            private Matrix TransformMatrix;
            private Matrix InverseTransform;
            private bool UpdateTransform;
            private bool UpdateRotation;
            public TransformMatrix3D()
            {
                RotXMatrix = new Matrix(3);
                RotYMatrix = new Matrix(3);
                RotZMatrix = new Matrix(3);
                RotMatrix = new Matrix(3);
                TranslateMatrix = new Matrix(3);
                TransformMatrix = new Matrix(3);
                InverseTransform = new Matrix(3);
                UpdateTransform = false;
                UpdateRotation = false;
            }
            public void RotateX(double Rads)
            {
                RotX = Rads;
                RotXMatrix.SetValue(1, 1, Math.Cos(Rads));
                RotXMatrix.SetValue(2, 1, -Math.Sin(Rads));
                RotXMatrix.SetValue(1, 2, Math.Sin(Rads));
                RotXMatrix.SetValue(2, 2, Math.Cos(Rads));
                UpdateTransform = true;
                UpdateRotation = true;
            }
            public void RotateY(double Rads)
            {
                RotY = Rads;
                RotYMatrix.SetValue(0, 0, Math.Cos(Rads));
                RotYMatrix.SetValue(2, 1, Math.Sin(Rads));
                RotYMatrix.SetValue(0, 2, -Math.Sin(Rads));
                RotYMatrix.SetValue(2, 2, Math.Cos(Rads));
                UpdateTransform = true;
                UpdateRotation = true;
            }
            public void RotateZ(double Rads)
            {
                RotZ = Rads;
                RotZMatrix.SetValue(0, 0, Math.Cos(Rads));
                RotZMatrix.SetValue(1, 0, -Math.Sin(Rads));
                RotZMatrix.SetValue(0, 1, Math.Sin(Rads));
                RotZMatrix.SetValue(1, 1, Math.Cos(Rads));
                UpdateTransform = true;
                UpdateRotation = true;
            }
            public void Orientate(Vector3 XAxis, Vector3 YAxis, Vector3 ZAxis)
            {
                RotMatrix.SetValue(0, 0, XAxis.GetDim(0));
                RotMatrix.SetValue(0, 1, XAxis.GetDim(1));
                RotMatrix.SetValue(0, 2, XAxis.GetDim(2));
                RotMatrix.SetValue(1, 0, YAxis.GetDim(0));
                RotMatrix.SetValue(1, 1, YAxis.GetDim(1));
                RotMatrix.SetValue(1, 2, YAxis.GetDim(2));
                RotMatrix.SetValue(2, 0, ZAxis.GetDim(0));
                RotMatrix.SetValue(2, 1, ZAxis.GetDim(1));
                RotMatrix.SetValue(2, 2, ZAxis.GetDim(2));
                UpdateRotation = false;
            }
            private Matrix InverseRotate()
            {
                Matrix inv = new Matrix(3);
                Matrix rot = GetRotationMatrix();
                for (uint x = 0; x < rot.Cols; x++)
                {
                    for (uint y = 0; y < rot.Rows; y++)
                    {
                        inv.SetValue(y, x, rot.GetValue(x, y));
                    }
                }
                return inv;
            }
            public void Translate(double X, double Y, double Z)
            {
                TranslateX = X;
                TranslateY = Y;
                TranslateZ = Z;
                TranslateMatrix.SetValue(3, 0, X);
                TranslateMatrix.SetValue(3, 1, Y);
                TranslateMatrix.SetValue(3, 2, Z);
                UpdateTransform = true;
            }
            private Matrix InverseTranslate()
            {
                Matrix inv = new Matrix(3);
                inv.SetValue(3, 0, -TranslateMatrix.GetValue(3, 0));
                inv.SetValue(3, 1, -TranslateMatrix.GetValue(3, 1));
                inv.SetValue(3, 2, -TranslateMatrix.GetValue(3, 2));
                return inv;
            }
            public Matrix GetRotationMatrix()
            {
                if (UpdateRotation)
                {
                    RotMatrix.SetToIdenity();
                    RotMatrix = RotXMatrix.Multiply(RotMatrix);
                    RotMatrix = RotYMatrix.Multiply(RotMatrix);
                    RotMatrix = RotZMatrix.Multiply(RotMatrix);
                    UpdateRotation = false;
                }
                return RotMatrix;
            }
            public Matrix GetTransformMatrix(String ver)
            {
                if (UpdateTransform)
                {
                    UpdateMatrix();
                }
                switch (ver)
                {
                    case "Inverse":
                        return InverseTransform;
                    default:
                        return TransformMatrix;
                }
            }
            public Matrix GetTransformMatrix()
            {
                return GetTransformMatrix("");
            }
            private void UpdateMatrix()
            {
                //Model to World
                TransformMatrix.SetToIdenity();
                TransformMatrix = GetRotationMatrix().Multiply(TransformMatrix);
                TransformMatrix = TranslateMatrix.Multiply(TransformMatrix);
                //World to Model
                InverseTransform.SetToIdenity();
                InverseTransform = InverseTranslate().Multiply(InverseTransform);
                InverseTransform = InverseRotate().Multiply(InverseTransform);
                UpdateTransform = false;
            }
            public Matrix Transform(Matrix ToTransform)
            {
                return GetTransformMatrix().Multiply(ToTransform);
            }
            public Vector3 Transform(Vector3 ToTransform)
            {
                Matrix result = GetTransformMatrix().Multiply(VecToMatrix(ToTransform));
                return MatrixToVec(result);
            }
            public Matrix TransformInverse(Matrix ToTransform)
            {
                return GetTransformMatrix("Inverse").Multiply(ToTransform);
            }
            public Vector3 TransformInverse(Vector3 ToTransform)
            {
                Matrix result = GetTransformMatrix("Inverse").Multiply(VecToMatrix(ToTransform));
                return MatrixToVec(result);
            }
            public Vector3 ModelToWorld(Vector3 ToTransform)
            {
                Matrix result = GetTransformMatrix().Multiply(VecToMatrix(ToTransform));
                return MatrixToVec(result);
            }
            public Vector3 WorldToModel(Vector3 ToTransform)
            {
                Matrix result = GetTransformMatrix("Inverse").Multiply(VecToMatrix(ToTransform));
                return MatrixToVec(result);
            }
            private Matrix VecToMatrix(Vector3 vec)
            {
                Matrix result = new Matrix(1, 4);
                result.SetValue(0, 0, vec.GetDim(0));
                result.SetValue(0, 1, vec.GetDim(1));
                result.SetValue(0, 2, vec.GetDim(2));
                return result;
            }
            private Vector3 MatrixToVec(Matrix m)
            {
                return new Vector3(m.GetValue(0, 0), m.GetValue(0, 1), m.GetValue(0, 2));
            }
        }
        public class LocalCoords
        {
            private Vector3 Radix;
            private Vector3 XAxis;
            private Vector3 YAxis;
            private Vector3 ZAxis;
            private TransformMatrix3D TransformToGlobal;
            public LocalCoords(Vector3 radix, Vector3 y, Vector3 z)
            {
                Radix = radix;
                YAxis = Normalize(GetVectorRelative(y, Radix));
                ZAxis = Normalize(GetVectorRelative(z, Radix));
                XAxis = CrossProduct(YAxis, ZAxis);
                TransformToGlobal = new TransformMatrix3D();
                TransformToGlobal.Translate(Radix.GetDim(0), Radix.GetDim(1), Radix.GetDim(2));
                TransformToGlobal.Orientate(XAxis, YAxis, ZAxis);
            }
            public Vector3 GetLocalVector(Vector3 point)
            {
                return TransformToGlobal.WorldToModel(point);
            }
            public Vector3 GetGlobalVector(Vector3 point)
            {
                return TransformToGlobal.ModelToWorld(point);
            }
            private Vector3 GetVectorRelative(Vector3 vec, Vector3 rel)
            {
                double X = vec.GetDim(0) - rel.GetDim(0);
                double Y = vec.GetDim(1) - rel.GetDim(1);
                double Z = vec.GetDim(2) - rel.GetDim(2);
                return new Vector3(X, Y, Z);
            }
            private Vector3 CrossProduct(Vector3 a, Vector3 b)
            {
                return Vector3.Cross(a, b);
            }
            private Vector3 Normalize(Vector3 v)
            {
                double n = Math.Sqrt(v.GetDim(0) * v.GetDim(0) + v.GetDim(1) * v.GetDim(1) + v.GetDim(2) * v.GetDim(2));
                double x = v.GetDim(0) / n;
                double y = v.GetDim(1) / n;
                double z = v.GetDim(2) / n;
                return new Vector3(x, y, z);
            }
            private Vector3 Reverse(Vector3 v)
            {
                double x = -v.GetDim(0);
                double y = -v.GetDim(1);
                double z = -v.GetDim(2);
                return new Vector3(x, y, z);
            }
        }

    }
}
