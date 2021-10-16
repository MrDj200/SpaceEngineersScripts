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
        public Program()
        {
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            List<IMyThrust> thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(thrusters);
            StringBuilder msg = new StringBuilder();
            IMyTextPanel lcd = (IMyTextPanel) GridTerminalSystem.GetBlockWithName("shitLCD");

            thrusters.ForEach( thruster => {
                msg
                .Append($"{thruster.DisplayNameText}:\n")
                .Append($"\tMaxThrust: {thruster.MaxThrust.ToString("n2")}N\n")
                .Append($"\tMaxEffectiveThrust: {thruster.MaxEffectiveThrust.ToString("n2")}N\n\n");
            });
            //Echo(msg.ToString());
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;
            lcd.WriteText(msg, false);
        }
    }
}
