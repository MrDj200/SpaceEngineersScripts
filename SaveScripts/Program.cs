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

        IMyInteriorLight light;
        TimeSpan time = new TimeSpan();
        int seconds = 0;
            
        public Program()
        {
            light = GridTerminalSystem.GetBlockWithName("DebugLight") as IMyInteriorLight;
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

        }

        public void Save()
        {            
            if(light == null)
            {
                throw new Exception("No light with the name \"DebugLight\" found");
            }
            light.Color = Color.DarkRed;
            light.Enabled = true;
            seconds = 0;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            seconds += Runtime.TimeSinceLastRun.Seconds; 
            time.Add(Runtime.TimeSinceLastRun);
            Echo($"Time Seconds: {time.Seconds.ToString()}\nSince Last Run: {Runtime.TimeSinceLastRun.Seconds}\n Seconds: {seconds}");
            if (time.Seconds >= 10 || seconds >= 10)
            {
                light.Enabled = false;
                seconds = 0;
                time = new TimeSpan();
            }
            
        }
    }
}