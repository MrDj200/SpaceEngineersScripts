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

		public Program()
		{
			// The constructor, called only once every session and
			// always before any other method is called. Use it to
			// initialize your script. 
			//     
			// The constructor is optional and can be removed if not
			// needed.
			// 
			// It's recommended to set RuntimeInfo.UpdateFrequency 
			// here, which will allow your script to run itself without a 
			// timer block.
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

		void Main(string argument, UpdateType updateSource)
		{
			if (argument == "OpenHangar1")
			{
				var hangarGroup = GridTerminalSystem.GetBlockGroupWithName("Hangar 1");
				List<IMyAirtightHangarDoor> doors = new List<IMyAirtightHangarDoor>();
				hangarGroup.GetBlocksOfType(doors);

				foreach (var door in doors)
				{
					door.SetValue("Open", true);
				}
			}
			if (argument == "CloseHangar1")
			{
				var hangarGroup = GridTerminalSystem.GetBlockGroupWithName("Hangar 1");
				List<IMyAirtightHangarDoor> doors = new List<IMyAirtightHangarDoor>();
				hangarGroup.GetBlocksOfType(doors);

				foreach (var door in doors)
				{
					door.SetValue("Open", false);
				}

			}
		}
	}
}