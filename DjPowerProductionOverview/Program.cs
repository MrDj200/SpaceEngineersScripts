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
			// Name of the Group containing the LCD Panels
			DjConfig.LcdGroupName = "PPO Group";

			// Max Output of Solar panels on a big Grid in kW
			DjConfig.solarMaxOutBig = 120;

			// Max Output of Solar panels on a small Grid in kW
			DjConfig.solarMaxOutSmall = 30;

		}

		public static class DjConfig
		{
			public static String LcdGroupName = "PPO Group";
			public static int solarMaxOutBig = 120, solarMaxOutSmall = 30;
			public static StringBuilder message = new StringBuilder();
			public static float solarMaxOutOptimal, solarCurOut, solarCurMaxOut;
			public static int solarCountSmall = 0, solarCountBig = 0, solarCountTotal = 0, solarCountBroken = 0, solarCountTurnedOff = 0;

			public static int reactorCountSmall = 0, reactorCountBig = 0, reactorCountTotal = 0;
			public static float reactorMaxOut = 0f, reactorCurOut = 0f;

			public static float totalUraniumLeft = 0f, totalPowerGenerated = 0f;
			

		}

		public Program()
		{
			Configuration();
			Runtime.UpdateFrequency = UpdateFrequency.Update100;
		}

		public void Main(string argument, UpdateType updateSource)
		{		
			var lcdGroup = GridTerminalSystem.GetBlockGroupWithName(DjConfig.LcdGroupName);
			List<IMyTextPanel> myLcdPanels = new List<IMyTextPanel>();		
			lcdGroup.GetBlocksOfType(myLcdPanels);

			if (myLcdPanels.Count <= 0)
			{
				Echo("Group \"" + DjConfig.LcdGroupName + "\" is empty!");
				return;
			}

			SolarStuff();
			ReactorStuff();

			foreach (IMyTextPanel curPanel in myLcdPanels)
			{
				curPanel.ShowPublicTextOnScreen();
				curPanel.WritePublicText(DjConfig.message, false);
			}
			
		}

		void SolarStuff()
		{
			DjConfig.message.Clear();
			DjConfig.solarMaxOutOptimal = 0f;
			DjConfig.solarCurOut = 0f;
			DjConfig.solarCountBig = 0;
			DjConfig.solarCountSmall = 0;
			DjConfig.solarCurMaxOut = 0;
			DjConfig.solarCountBroken = 0;
			DjConfig.solarCountTurnedOff = 0;

			List<IMySolarPanel> mySolarPanels = new List<IMySolarPanel>();
			GridTerminalSystem.GetBlocksOfType(mySolarPanels);

			DjConfig.solarCountTotal = mySolarPanels.Count;
			foreach (IMySolarPanel mySolar in mySolarPanels)
			{
				if (!mySolar.IsFunctional)
				{
					DjConfig.solarCountBroken++;
				}
				if (!mySolar.IsWorking)
				{
					DjConfig.solarCountTurnedOff++;
				}

				if (mySolar.CubeGrid.GridSizeEnum == MyCubeSize.Large)
				{
					DjConfig.solarMaxOutOptimal += DjConfig.solarMaxOutBig;
					DjConfig.solarCountBig++;
				}
				else
				{
					DjConfig.solarMaxOutOptimal += DjConfig.solarMaxOutSmall;
					DjConfig.solarCountSmall++;
				}
				DjConfig.solarCurMaxOut += mySolar.MaxOutput * 1000; // Current max output in kW

				DjConfig.solarCurOut += mySolar.CurrentOutput * 1000; // Current output in kW
			}

			DjConfig.message.
				Append("Solar:\n").
				Append(GetPercentage(DjConfig.solarCurOut, DjConfig.solarCurOut + DjConfig.reactorCurOut) + "% of our power is Green" + "\n").
				Append("Current Max Output: " + Math.Round(DjConfig.solarCurMaxOut, 2) + " kW\n").
				Append(Math.Round(DjConfig.solarCurOut, 2) + "/" + Math.Round(DjConfig.solarMaxOutOptimal, 2) + " kW\n").
				Append(GetPercentage(DjConfig.solarCurOut, DjConfig.solarMaxOutOptimal) + "% of optimal solar Output\n").
				Append("Total Panels: " + DjConfig.solarCountTotal);

			if (DjConfig.solarCountBroken != 0)
			{
				DjConfig.message.Append("\n" + DjConfig.solarCountBroken + " Broken Panels!");
			}
			if (DjConfig.solarCountTurnedOff != 0)
			{
				DjConfig.message.Append("\n" + DjConfig.solarCountTurnedOff + " Offline Panels");
			}

		}

		void ReactorStuff()
		{

			DjConfig.reactorMaxOut = 0f;
			DjConfig.reactorCurOut = 0f;
			DjConfig.reactorCountBig = 0;
			DjConfig.reactorCountSmall = 0;

			DjConfig.totalUraniumLeft = 0f;

			List<IMyReactor> myReactorList = new List<IMyReactor>();
			GridTerminalSystem.GetBlocksOfType(myReactorList);

			DjConfig.reactorCountTotal = myReactorList.Count;
			foreach (IMyReactor myReactor in myReactorList)
			{
				if (myReactor.CubeGrid.GridSizeEnum == MyCubeSize.Large)
				{
					DjConfig.reactorCountBig++;
				}
				else
				{
					DjConfig.reactorCountSmall++;
				}
				//myReactor.GetInventory().item
				DjConfig.reactorMaxOut += myReactor.MaxOutput; // Current max output in MW
				DjConfig.reactorCurOut += myReactor.CurrentOutput; // Current output in MW
			}

			DjConfig.message.
				Append("\n\nReactors:\n").
				Append("Uranium Left: " + TotalUranium(myReactorList) + "kg\n").
				Append(Math.Round(DjConfig.reactorCurOut, 2) + "/" + Math.Round(DjConfig.reactorMaxOut, 2) + " MW\n").
				Append(GetPercentage(DjConfig.reactorCurOut, DjConfig.reactorMaxOut) + "% of possible power Output (Reactor)\n").
				Append("Total Reactors: " + DjConfig.reactorCountTotal).
				Append("\n Big Reactors: " + DjConfig.reactorCountBig).
				Append("\n Small Reactors: " + DjConfig.reactorCountSmall);

		}

		// Determine Total Uranium 
		string TotalUranium(List<IMyReactor> reactors)
		{
			// Setup 
			float totalUranium = 0.0f;

            //IMyInventoryOwner owner;
			IMyInventory inventory;
			List<IMyInventoryItem> totalitems = new List<IMyInventoryItem>();

            // Acquire All Reactor Inventories 

            foreach (IMyReactor reactor in reactors)
            {
                inventory = reactor.GetInventory(0);
                var items = inventory.GetItems();

                totalitems.AddRange(items);
            }

            foreach (IMyInventoryItem item in totalitems)
            {
                if (item.Content.SubtypeId.ToString() == "Uranium")
                {
                    totalUranium += (float)item.Amount;
                }
            }

			return totalUranium.ToString("n2");
		}

		float GetPercentage(float myCurrent, float myMax)
		{
			int tempVal = (int)((myCurrent / myMax) * 100) * 100;
			return tempVal / 100;
		}

	}

}