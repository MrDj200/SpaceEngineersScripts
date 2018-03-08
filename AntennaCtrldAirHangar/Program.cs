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
			//Name of the Hangar (Every Group has to start with this String):
			DjConfig.roomName = "Hangar 1";

			//Name of the Antenna:
			DjConfig.antennaName = "Antenna";

			//Passes:
			//	Passphrase to open/decompress the Hangar:
			DjConfig.passOpen = "OpenHangar1";
			// Passphrase to close/compress the Hangar:
			DjConfig.passClose = "CloseHangar1";

			//Subnames:
			//	Name of the Vents group:
			DjConfig.roomNameVents = "Vents";
			//	Name of the Doors Group:
			DjConfig.roomNameDoors = "Doors";
			//	Name of the Lights Group:
			DjConfig.roomNameLights = "Lights";
			// Name of the Other Doors Group:
			DjConfig.roomNameOtherDoors = "Other Doors";

		}


		///////////////////////////////
		//Do not modify the following//
		///////////////////////////////

		public static class DjConfig
		{
			public static String roomName, roomNameVents, roomNameDoors, roomNameLights, roomNameOtherDoors;
			public static String passOpen, passClose;
			public static String antennaName;
		}

		public Program()
		{
			Configuration();
		}

		public void Main(string argument, UpdateType updateSource)
		{
			if (argument != DjConfig.passClose && argument != DjConfig.passOpen)
			{
				Echo("Invalid Arguments: \"" + argument + "\"");
				return;
			}

			var hangarVentGroup = GridTerminalSystem.GetBlockGroupWithName(DjConfig.roomName + " " + DjConfig.roomNameVents);				
			var hangarLightGroup = GridTerminalSystem.GetBlockGroupWithName(DjConfig.roomName + " " + DjConfig.roomNameLights);
			var hangarDoorGroup = GridTerminalSystem.GetBlockGroupWithName(DjConfig.roomName + " " + DjConfig.roomNameDoors);
			var hangarOtherDoors = GridTerminalSystem.GetBlockGroupWithName(DjConfig.roomName + " " + DjConfig.roomNameOtherDoors);

			List<IMyAirVent> myAirVents = new List<IMyAirVent>();				
			List<IMyLightingBlock> myLights = new List<IMyLightingBlock>();
			List<IMyAirtightHangarDoor> myHangarDoors = new List<IMyAirtightHangarDoor>();
			List<IMyDoor> myOtherDoors = new List<IMyDoor>();

			hangarVentGroup.GetBlocksOfType(myAirVents);
			hangarLightGroup.GetBlocksOfType(myLights);
			hangarDoorGroup.GetBlocksOfType(myHangarDoors);				
			hangarOtherDoors.GetBlocksOfType(myOtherDoors);

			//IMyAirVent checkVent = myAirVents.First<IMyAirVent>();
			//checkVent.Set

			if (argument == DjConfig.passClose)
			{
				foreach (var door in myHangarDoors)
				{
					door.CloseDoor();
				}
				foreach (var light in myLights)
				{
					light.Color = new Color(255, 255, 255);
				}
				foreach (var vent in myAirVents)
				{
					vent.Depressurize = false;
				}
			}
			else if (argument == DjConfig.passOpen)
			{
				foreach (var door in myOtherDoors)
				{
					door.CloseDoor();
				}
				foreach (var vent in myAirVents)
				{
					vent.Depressurize = true;
				}
				foreach (var light in myLights)
				{
					light.Color = new Color(255, 0, 0);
				}
			}
		}
	}
}