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
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
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
			StringBuilder message = new StringBuilder();

			IMyTextPanel testLcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName("TestDisplay");
			testLcd.Font = "Monospace";

			IMyCargoContainer cargoContainer = (IMyCargoContainer)GridTerminalSystem.GetBlockWithName("AssContainer");
			List<IMyInventoryItem> test = cargoContainer.GetInventory().GetItems();
			message.Clear();

			foreach (IMyInventoryItem curItem in test)
			{
				message.Append(curItem.Content.SubtypeName + " " + curItem.Amount + "\n");
			}

			List<IMySensorBlock> sensorList = new List<IMySensorBlock>();
			List<MyDetectedEntityInfo> testList = new List<MyDetectedEntityInfo>();
			GridTerminalSystem.GetBlocksOfType(sensorList);

			foreach (IMySensorBlock mySensor in sensorList)
			{
				mySensor.DetectedEntities(testList);
				message.Append("\n" + mySensor.CustomName + ": \n");
				foreach (MyDetectedEntityInfo info in testList)
				{
					message.Append( info.Name + " | " + info.Relationship. + "\n");
				}
			}

			Echo(message + "");
			testLcd.ShowPublicTextOnScreen();
			testLcd.WritePublicText(message);
		}
	}
}