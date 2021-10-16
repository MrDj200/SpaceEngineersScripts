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
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            int percentage = CalcCargoSpace();            

            List<IMyCockpit> cocks = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType(cocks, cock => cock.CubeGrid == Me.CubeGrid);

            //Echo($"{msg}");
        }

        int CalcCargoSpace()
        {
            float curVol = 0;
            float maxVol = 0;
            bool drillError = false;
            IMyShipDrill faultyDrill = null;

            List<IMyShipDrill> drillList = new List<IMyShipDrill>();
            GridTerminalSystem.GetBlocksOfType(drillList, drill => drill.IsFunctional && drill.CubeGrid == Me.CubeGrid);

            drillList.ForEach(drill =>
            {
                if (!drill.GetInventory().IsConnectedTo(drillList[0].GetInventory()))
                {
                    Echo($"Not all drills are Connected!\nSpecifically: {drill.CustomName}");
                    faultyDrill = drill;
                    drillError = true;
                }
                curVol += drill.GetInventory().CurrentVolume.RawValue / 1000;
                maxVol += (drill.GetInventory().MaxVolume.RawValue / 1000) / 2;
            });

            if (drillError && faultyDrill != null)
            {
                throw new Exception($"Drill {faultyDrill.CustomName} is not connected!");
            }


            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks, block =>
                block.CubeGrid == Me.CubeGrid && // Check for same grid as prog block
                block.HasInventory && // Check if block has inventory
                block.InventoryCount == 1 &&// Check if block has only 1 inventory
                block.IsWorking && // Check if block is working
                block.GetInventory().IsConnectedTo(drillList[0].GetInventory()) && // Only inventories connected to the drills
                !drillList.Contains(block) // Don't add drills to the list
            );                      

            blocks.ForEach(block =>
            {
                curVol += block.GetInventory().CurrentVolume.RawValue / 1000;
                maxVol += block.GetInventory().MaxVolume.RawValue / 1000;
            });

            return Convert.ToInt32(curVol / maxVol * 100);
        }
    }
}
