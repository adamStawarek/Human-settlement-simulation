﻿using System;
using System.Linq;
using SettlementSimulation.Engine.Enumerators;
using SettlementSimulation.Engine.Helpers;

namespace SettlementSimulation.Engine.Models.Buildings.ThirdType
{
    [Epoch(Epoch.Third)]
    public class Port : Building
    {
        public override double Probability => 0.005;
        public override int Space => 3;

        public override double CalculateFitness(BuildingRule model)
        {
            var maxPortDistanceToWater = 5;
            var field = model.Fields[this.Position.X, this.Position.Y];
            if (field.DistanceToWater > maxPortDistanceToWater)
            {
                return 0;
            }

            var ports = model.Roads.SelectMany(b => b.Buildings).Where(b => b is Port);
            if (ports.Any())
            {
                //("There can be only one port");
                return 0;
            }

            return 15;
        }
    }
}