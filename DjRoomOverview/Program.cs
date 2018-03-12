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
			// Keyword all related blocks must have in front of their names:
			DjConfig.roomKeyword = "Room";

			// Play sound when someone/thing enters a sensor field: 
			DjConfig.playSound = false; //( true or false )
		}

		public static class DjConfig
		{
			public static List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
			public static List<IMyTerminalBlock> filteredBlocks = new List<IMyTerminalBlock>();
			public static List<IMySensorBlock> sensorBlocks = new List<IMySensorBlock>();
			public static List<IMyTextPanel> textBlocks = new List<IMyTextPanel>();
			public static List<MyDetectedEntityInfo> entities = new List<MyDetectedEntityInfo>();

			public static String roomKeyword = "Room";
			public static bool playSound = false;
			public static bool detectPlayers = true, detectFriendly = true, detectEnemy = true, detectLargeGrids = true, detectSmallGrids = true;

			public static StringBuilder message = new StringBuilder(), echoMessage = new StringBuilder();			
			public static char seperatorSymbol = '|';
			public static int timer = 0, refreshCycle = 10, panelCount = 0, sensorCount = 0;
			
		}

		public Program()
		{
			Configuration();
			GetBlockLists();
			Runtime.UpdateFrequency = UpdateFrequency.Update100;
		}

		public void Main(string argument, UpdateType updateSource)
		{
			DjConfig.timer++;
			DjConfig.message.Clear();
			if (DjConfig.timer >= DjConfig.refreshCycle)
			{
				GetBlockLists();
				DjConfig.timer = 0;
			}
			DjConfig.sensorCount = 0;
			foreach (IMySensorBlock mySensor in DjConfig.sensorBlocks)
			{
				mySensor.PlayProximitySound = DjConfig.playSound;
				mySensor.DetectPlayers = DjConfig.detectPlayers;
				mySensor.DetectFriendly = DjConfig.detectFriendly;
				mySensor.DetectEnemy = DjConfig.detectEnemy;
				mySensor.DetectLargeShips = DjConfig.detectLargeGrids;
				mySensor.DetectSmallShips = DjConfig.detectSmallGrids;

				mySensor.DetectFloatingObjects = true;

				DjConfig.entities.Clear();
				mySensor.DetectedEntities(DjConfig.entities);
				DjConfig.message.
					Append(mySensor.CustomName.Split(DjConfig.seperatorSymbol)[1]);

				if (DjConfig.entities.Count <= 0)
				{
					DjConfig.message.
						Append("\n\\\\ \n");
					continue;
				}
				
				foreach (MyDetectedEntityInfo myEnt in DjConfig.entities)
				{
					char listChar = '?';
					String relation = myEnt.Relationship.ToString();
					if (myEnt.Type == MyDetectedEntityType.FloatingObject)
					{
						listChar = 'I';
						relation = "Item";
					}
					else if(myEnt.Type == MyDetectedEntityType.CharacterHuman)
					{
						listChar = 'P';
						if (myEnt.Relationship == MyRelationsBetweenPlayerAndBlock.FactionShare || myEnt.Relationship == MyRelationsBetweenPlayerAndBlock.Owner)
						{
							relation = "Friendly";
						}
					}
					else if (myEnt.Type == MyDetectedEntityType.CharacterOther)
					{
						listChar = 'A';
					}
					else if (myEnt.Type == MyDetectedEntityType.SmallGrid)
					{
						listChar = 'S';
						relation = "S-Ship";
					}
					else if (myEnt.Type == MyDetectedEntityType.LargeGrid)
					{
						listChar = 'L';
					}

					DjConfig.message.
						Append("\n" + listChar + " - ").
						//Append(myEnt.Name).
						Append(String.Format("{0,-15} | {1,5}", myEnt.Name, relation));
						//Append(myEnt.Relationship);
						
				}
				DjConfig.message.Append("\n");

				DjConfig.sensorCount++;
				
			}
			DjConfig.panelCount = 0;
			foreach (IMyTextPanel textPanel in DjConfig.textBlocks)
			{
				textPanel.ShowPublicTextOnScreen();
				textPanel.Font = "Monospace";
				textPanel.FontSize = .8f;
				textPanel.WritePublicText(DjConfig.message, false);
				DjConfig.panelCount++;
			}

			DjConfig.echoMessage.Clear();
			DjConfig.echoMessage.
				Append("\nText Panels: " + DjConfig.panelCount).
				Append("\nSensorCount: " + DjConfig.sensorCount);

			Echo(DjConfig.echoMessage.ToString());

		}

		void GetBlockLists()
		{
			DjConfig.allBlocks.Clear();
			DjConfig.filteredBlocks.Clear();
			DjConfig.sensorBlocks.Clear();
			DjConfig.textBlocks.Clear();
			DjConfig.filteredBlocks.Clear();

			GridTerminalSystem.GetBlocks(DjConfig.allBlocks);


			DjConfig.filteredBlocks.AddRange(DjConfig.allBlocks.Where(s => s.CustomName.StartsWith(DjConfig.roomKeyword)));

			DjConfig.sensorBlocks.AddRange(DjConfig.filteredBlocks.OfType<IMySensorBlock>());
			DjConfig.textBlocks.AddRange(DjConfig.filteredBlocks.OfType<IMyTextPanel>());

		}
	}
}