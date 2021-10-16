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
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

        public static class DjConfig
        {
            public static string keyWord = "[PPO]";
            public static float solarCurOut, solarCurMaxOut;
            public static int solarCountSmall = 0, solarCountBig = 0, solarCountTotal = 0, solarCountBroken = 0, solarCountTurnedOff = 0;
        }

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100; // Update all 100 game ticks
        }

        void SolarStuff()
        {
            DjConfig.solarCountBig = 0;
            DjConfig.solarCountBroken = 0;
            DjConfig.solarCountSmall = 0;
            DjConfig.solarCountTotal = 0;
            DjConfig.solarCountTurnedOff = 0;
            DjConfig.solarCurMaxOut = 0;
            DjConfig.solarCurOut = 0;


            List<IMySolarPanel> mySolarPanels = new List<IMySolarPanel>();
            GridTerminalSystem.GetBlocksOfType(mySolarPanels);

            DjConfig.solarCountTotal = mySolarPanels.Count;
            foreach (IMySolarPanel mySolar in mySolarPanels)
            {
                if (!mySolar.IsFunctional)
                {
                    DjConfig.solarCountBroken++;
                    continue;
                }
                if (!mySolar.IsWorking)
                {
                    DjConfig.solarCountTurnedOff++;
                    continue;
                }

                if (mySolar.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                {
                    DjConfig.solarCountBig++;
                }
                else
                {
                    DjConfig.solarCountSmall++;
                }
                DjConfig.solarCurMaxOut += mySolar.MaxOutput * 1000; // Current max output in kW

                DjConfig.solarCurOut += mySolar.CurrentOutput * 1000; // Current output in kW
            }
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
            SolarStuff();

            StringBuilder message = new StringBuilder();
            int maxOutput = DjConfig.solarCountBig * 160;
            float efficiency = (DjConfig.solarCurOut / maxOutput) * 100;

            var unused = message.
                Append($"Solar Count: {DjConfig.solarCountBig}\n").
                Append($"Solar MaxOut: {string.Format("{0:0,0.##}", maxOutput)} kW\n").
                Append($"Solar CurOut: {string.Format("{0:0,0.##}", DjConfig.solarCurOut)} kW\n\n").
                Append($"Solar Efficiency: {string.Format("{0:0}", efficiency)}%");

            List<IMyTextPanel> myLcds = new List<IMyTextPanel>();
            List<IMyTextPanel> ppoLcds = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(myLcds);

            myLcds.ForEach(lcd =>
            {
                if (lcd.CustomName.Contains(DjConfig.keyWord))
                {
                    ppoLcds.Add(lcd);
                }
            });

            if(ppoLcds.Count >= 1)
            {
                ppoLcds.ForEach(lcd =>
                {
                    lcd.ContentType = ContentType.TEXT_AND_IMAGE;
                    lcd.WriteText(message);
                });
            }
            else
            {
                Echo("No LCD's with Keyword found");
            }
        }
    }
}
