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
		}

		public void Main(string argument, UpdateType updateSource)
		{
			if (String.IsNullOrEmpty(argument))
			{
				Echo("No argument!");
				return;
			}
			List<IMyRadioAntenna> myAntennas = new List<IMyRadioAntenna>();
			GridTerminalSystem.GetBlocksOfType(myAntennas);

			foreach (IMyRadioAntenna ant in myAntennas)
			{
				if (ant.IsFunctional && ant.IsWorking)
				{
					bool result = ant.TransmitMessage(argument, MyTransmitTarget.Ally);
                    Echo($"Transmitted \"{argument}\" on Antenna \"{ant.CustomName}\" Status: {result}");
					return;
				}
			}

			//var ant = GridTerminalSystem.GetBlockWithName("Antenna") as IMyRadioAntenna;
			//ant.TransmitMessage(argument, MyTransmitTarget.Everyone);
		}
	}
}