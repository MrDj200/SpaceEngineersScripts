﻿using Sandbox.Game.EntityComponents;
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
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
	partial class Program : MyGridProgram
	{


		void Configuration()
		{
			//Name of the Target Container where the Assemblers Items will be move to:
			DjConfig.targetContainerName = "AssContainer";

			//Name of the Main Storage the Items will be moved when the above is to be cleared:
			DjConfig.mainCargoName = "Large Cargo Container (Master)";
			
		}


		///////////////////////////////
		//Do not modify the following//
		///////////////////////////////

		public static class DjConfig
		{
			public static String targetContainerName = "AssCargo";
			public static String mainCargoName = "MainCargo";
			public static IMyCargoContainer targetContainer, mainCargo;
			public static IMyInventory targetInv, mainInv;
		}

		public Program()
		{
			Configuration();
			Runtime.UpdateFrequency = UpdateFrequency.Update100;

			DjConfig.targetContainer = (IMyCargoContainer)GridTerminalSystem.GetBlockWithName(DjConfig.targetContainerName);
			DjConfig.targetInv = DjConfig.targetContainer.GetInventory();

			DjConfig.mainCargo = (IMyCargoContainer)GridTerminalSystem.GetBlockWithName(DjConfig.mainCargoName);
			DjConfig.mainInv = DjConfig.mainCargo.GetInventory();

		}

		public void Main(string argument, UpdateType updateSource)
		{
			if (updateSource == UpdateType.Update1 || updateSource == UpdateType.Update10 || updateSource == UpdateType.Update100)
			{
				RunStuff();
			}
			else if (updateSource == UpdateType.Trigger)
			{
				if (argument.StartsWith("clear"))
				{
					ClearContainer(DjConfig.targetInv, DjConfig.mainInv);
					return;
				}
			}
					
		}

		void RunStuff()
		{

			List<IMyAssembler> allAsses = new List<IMyAssembler>();
			GridTerminalSystem.GetBlocksOfType(allAsses);
			foreach (IMyAssembler ass in allAsses)
			{
				if (!ass.UseConveyorSystem || ass.Mode == MyAssemblerMode.Disassembly)
				{
					continue;
				}
				ass.GetInventory(1).TransferItemTo(DjConfig.targetInv, 0);
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
				Echo("The Source Container is not Connected to the Target Container! (Check the Cargo Lines)");
				return;
			}
			List<IMyInventoryItem> myItems = sourceContainer.GetItems();
			int count = myItems.Count;
			for (int i = 0; i <= count; i++)
			{
				sourceContainer.TransferItemTo(targetContainer, 0);
			}
		}

	}
}