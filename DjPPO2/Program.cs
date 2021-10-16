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
        public const String LcdKeyWord = "[PPO]";


        // No Touchie from here!

        #region mdk macros
        // This script was deployed at $MDK_DATETIME$
        const string Deployment = "$MDK_DATE$, $MDK_TIME$";
        #endregion
        

        public Program()
        {
            Me.GetSurface(1).ContentType = ContentType.TEXT_AND_IMAGE;
            Me.GetSurface(1).FontSize = 8.5f;
            Me.GetSurface(1).WriteText("Dj's PPO2");
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            float CurrentPower = 0;
            float MaxCurrentPower = 0;
            float MaxTotalPower = 0;
            StringBuilder msg = new StringBuilder();
            List<IMyPowerProducer> producers = new List<IMyPowerProducer>();
            GridTerminalSystem.GetBlocksOfType(producers);
            producers.RemoveAll(producer => (producer.GetType().Name == "MyBatteryBlock") || !(producer.IsWorking));
            
            List<IMyTextPanel> lcds = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(lcds);
            lcds.RemoveAll(lcd => !lcd.CustomName.Contains(LcdKeyWord)); // Dropping all panels that dont have the KeyWord in their name
            Echo($"Found {lcds.Count} Panels.");

            producers.ForEach(producer => {
                MaxCurrentPower += producer.MaxOutput;
                CurrentPower += producer.CurrentOutput;
                MaxTotalPower += producer.OptimalMaxOutput();
                //msg.Append($"{producer.GetType().Name}\n");
            });

            lcds.ForEach(lcd => {
                lcd.ContentType = ContentType.TEXT_AND_IMAGE;
                msg
                    .Append($"Current power production:{CurrentPower.ToString("n2")}MW\n")
                    .Append($"Current potential power: {MaxCurrentPower.ToString("n2")}MW\n")
                    .Append($"Current MAX potential: {MaxTotalPower.ToString("n2")}MW");
                lcd.WriteText(msg);
            });
        }
    }
    public static class ExtensionStuff
    {
        public static float OptimalMaxOutput(this IMyPowerProducer powerBlock) => powerBlock.Components.Get<MyResourceSourceComponent>().DefinedOutput;
    }
}
