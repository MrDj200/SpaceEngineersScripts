﻿using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {


        void Configuration()
        {
            //Name of the Target Container where the Assemblers Items will be moved to:
            DjConfig.targetContainerName = "AssContainer";

            //Name of the Main Storage the Items will be moved when the above is to be cleared:
            DjConfig.mainCargoName = "Large Cargo Container (Master)";

            //Name of raw ressource storage
            DjConfig.rawCargoName = "Raw Ressource Cargo";

            DjConfig.disassemblerNameTag = "[DjDis]";

        }


        ///////////////////////////////
        //Do not modify the following//
        ///////////////////////////////

        public static class DjConfig
        {
            public static String targetContainerName = "AssCargo";
            public static String mainCargoName = "MainCargo";
            public static String rawCargoName = "RawCargo";
            public static String disassemblerNameTag = "[DjDis]";
            public static IMyCargoContainer targetContainer, mainCargo, rawCargo;
            public static IMyInventory targetInv, mainInv, rawInv;
        }

        List<IMyAssembler> allAsses = new List<IMyAssembler>();
        List<IMyAssembler> disassemblers = new List<IMyAssembler>();
        List<IMyRefinery> allRefineries = new List<IMyRefinery>();

        public Program()
        {
            Configuration();
            Runtime.UpdateFrequency = UpdateFrequency.Update100;


            DjConfig.targetContainer = (IMyCargoContainer)GridTerminalSystem.GetBlockWithName(DjConfig.targetContainerName);
            if (DjConfig.targetContainer == null)
            {
                Echo($"Target container with name \"{DjConfig.targetContainerName}\" not found!");
                return;
            }
            DjConfig.targetInv = DjConfig.targetContainer.GetInventory();

            DjConfig.mainCargo = (IMyCargoContainer)GridTerminalSystem.GetBlockWithName(DjConfig.mainCargoName);
            if (DjConfig.mainCargo == null)
            {
                Echo($"Main storage with name \"{DjConfig.mainCargoName}\" not found!");
                return;
            }
            DjConfig.mainInv = DjConfig.mainCargo.GetInventory();

            DjConfig.rawCargo = (IMyCargoContainer)GridTerminalSystem.GetBlockWithName(DjConfig.rawCargoName);
            if (DjConfig.rawCargo != null)
            {
                DjConfig.rawInv = DjConfig.rawCargo.GetInventory();
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo($"Got Argument: '{argument}'");
            RunStuff();

            GridTerminalSystem.GetBlocksOfType(disassemblers, block =>
                block.CustomName.Contains(DjConfig.disassemblerNameTag) &&
                block.IsWorking &&
                block.CubeGrid == Me.CubeGrid
            );

            if (disassemblers.Count > 0)
            {
                RunDisassemblerStuff();
            }

            Echo($"Found {allAsses.Count} Assemblers\nFound {disassemblers.Count} Disassemblers");

            if (updateSource == UpdateType.Trigger)
            {
                if (argument.StartsWith("clear"))
                {
                    Echo("Trying to clear shit!");
                    ClearContainer(DjConfig.targetInv, DjConfig.mainInv);
                    return;
                }
            }

        }

        void RunDisassemblerStuff()
        {
            disassemblers.ForEach(disAss =>
            {
                disAss.Mode = MyAssemblerMode.Disassembly;
                disAss.Repeating = true;
            });

            IMyInventory testInv = disassemblers[0].GetInventory(1);

            List<IMyTerminalBlock> connectedBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(connectedBlocks, block =>
                block.HasInventory &&
                block.IsWorking &&
                block.GetInventory(0).IsConnectedTo(testInv)
            );

            StringBuilder msg = new StringBuilder();

            connectedBlocks.ForEach(block =>
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                block.GetInventory(0).GetItems(items, item =>
                    item.Type == MyItemType.MakeTool("WelderItem") ||
                    item.Type == MyItemType.MakeTool("AngleGrinderItem") ||
                    item.Type == MyItemType.MakeTool("HandDrillItem") ||
                    item.Type == MyItemType.MakeTool("AutomaticRifleItem")
                );

                //block.GetInventory(0).GetItems(items);

                if (items.Count > 0)
                {
                    items.ForEach(item =>
                    {
                        block.GetInventory(0).TransferItemTo(testInv, item);
                    });
                }

            });

        }

        void RunStuff()
        {
            allAsses.Clear();
            GridTerminalSystem.GetBlocksOfType(allAsses, ass =>
                ass.UseConveyorSystem &&
                ass.Mode == MyAssemblerMode.Assembly &&
                ass.IsWorking &&
                ass.GetInventory(1).IsConnectedTo(DjConfig.targetInv) &&
                ass.CubeGrid == Me.CubeGrid
            );
            foreach (IMyAssembler ass in allAsses)
            {
                ass.GetInventory(1).TransferItemTo(DjConfig.targetInv, 0);
                if (DjConfig.rawInv != null)
                {
                    ass.GetInventory(0).TransferItemTo(DjConfig.rawInv, 0);
                }
            }

            if (DjConfig.rawInv != null)
            {
                allRefineries.Clear();
                GridTerminalSystem.GetBlocksOfType(allRefineries, block =>
                    block.CubeGrid == Me.CubeGrid &&
                    block.GetInventory(1).IsConnectedTo(DjConfig.rawInv)
                );

                foreach (var refinery in allRefineries)
                {
                    refinery.GetInventory(1).TransferItemTo(DjConfig.rawInv, 0);
                }
            }

        }

        void ClearContainer(IMyInventory sourceContainer, IMyInventory targetContainer)
        {
            if (sourceContainer == null || targetContainer == null)
            {
                Echo("One of the Containers could not be found!");
                return;
            }
            if (!sourceContainer.IsConnectedTo(targetContainer))
            {
                Echo("The Source Container is not Connected to the Target Container! (Check the Conveyor Lines)");
                return;
            }
            List<MyInventoryItem> myItems = new List<MyInventoryItem>();
            sourceContainer.GetItems(myItems);

            int count = myItems.Count;
            for (int i = 0; i <= count; i++)
            {
                sourceContainer.TransferItemTo(targetContainer, 0);
            }
        }

    }
}