﻿using SettlementSimulation.Engine.Helpers;

namespace SettlementSimulation.Engine.Models.Buildings.FirstType
{
    [Epoch(Epoch.First)]
    public class Tavern : Building
    {
        public override double Probability => 0.1;
    }
}