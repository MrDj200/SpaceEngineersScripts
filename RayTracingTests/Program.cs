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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        static class DjConfig
        {
            public static double lastVal;
            public static bool isRip;
        }

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            IMyCameraBlock cam = GridTerminalSystem.GetBlockWithName("TestCam") as IMyCameraBlock;

            if( cam != null)
            {
                cam.EnableRaycast = true;
                MyDetectedEntityInfo shit = cam.Raycast(new Vector3D(1, 1, 1));
                if (shit.HitPosition.HasValue)
                {
                    double distance = Vector3D.Distance(shit.HitPosition.Value, cam.GetPosition());
                    DjConfig.lastVal = distance;
                    DjConfig.isRip = !shit.HitPosition.HasValue;
                }
                StringBuilder msg = new StringBuilder();

                msg
                    .Append($"Distance: {String.Format("{0:0,0.##}", DjConfig.lastVal)}m\n")
                    .Append($"IsRip: {DjConfig.isRip}");

                Echo(msg.ToString());

            }
        }

        public double test(IMyCameraBlock cam, MyDetectedEntityInfo shit)
        {            
            double distance = Vector3D.Distance(shit.HitPosition.Value, cam.GetPosition());
            return distance;
        }
    }
}