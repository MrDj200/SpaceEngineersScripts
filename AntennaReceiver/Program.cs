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
		void Configuration()
		{

		}


		///////////////////////////////
		//Do not modify the following//
		///////////////////////////////

		public static class DjConfig
		{
		}

		public Program()
		{
			Configuration();
		}

		public void Main(string argument, UpdateType updateSource)
		{
			if (String.IsNullOrEmpty(argument))
			{
				Echo("No argument!");
				return;
			}

			IMyBlockGroup myGroup = GridTerminalSystem.GetBlockGroupWithName(argument);
			List<IMyAirtightHangarDoor> myDoors = new List<IMyAirtightHangarDoor>();
			myGroup.GetBlocksOfType(myDoors);

			if (myDoors.Count == 0)
			{
				Echo("No doors on group");
				return;
			}

			foreach (IMyAirtightHangarDoor door in myDoors)
			{
				door.ToggleDoor();
			}
		}
	}
}