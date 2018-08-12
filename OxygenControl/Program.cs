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
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        void Configuration()
        {
            // Name of the Vent that vents
            DjConfig.ventVent = "The vent Vent";

        }

        public static class DjConfig
        {
            public static String ventVent;

        }

        public Program()
        {
            Configuration();
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            List<IMyGasTank> allTanks = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType(allTanks);

            List<IMyGasGenerator> allOxyGenerators = new List<IMyGasGenerator>();
            GridTerminalSystem.GetBlocksOfType(allOxyGenerators);

            List<IMyGasTank> bigHydroTanks = new List<IMyGasTank>();
            List<IMyGasTank> bigOxygenTanks = new List<IMyGasTank>();            

            foreach (IMyGasTank tank in allTanks)
            {
                if (tank.CubeGrid.GridSizeEnum == MyCubeSize.Large)
                {
                    if (tank.BlockDefinition.SubtypeId.Contains("Hydro"))
                    {
                        bigHydroTanks.Add(tank);
                        continue;
                    }
                    else
                    {
                        bigOxygenTanks.Add(tank);
                        continue;
                    }
                }
            }

            float OxygenPercentage = GetPercentage(bigOxygenTanks);
            float HydrogenPercentage = GetPercentage(bigHydroTanks);

            if(OxygenPercentage >= 0.8)
            {
                allOxyGenerators.ForEach(g => g.Enabled = false);
            }
            if (OxygenPercentage <= 0.4 || HydrogenPercentage <= 0.6)
            {
                allOxyGenerators.ForEach(g => g.Enabled = true);
            }
            
        }

        public void VentStuff()
        {

        }

        public float GetPercentage(List<IMyGasTank> tanks)
        {
            return (float)tanks.Max(t => t.FilledRatio);
        }

    }
}