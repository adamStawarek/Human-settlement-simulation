﻿using System;
using System.Linq;
using SettlementSimulation.Engine.Enumerators;
using SettlementSimulation.Engine.Helpers;
using SettlementSimulation.Engine.Models.Buildings.FirstType;

namespace SettlementSimulation.Engine.Models.Buildings.SecondType
{
    [Epoch(Epoch.Second)]
    public class Administration : Building
    {
        public override double Probability => 0.01;
        public override int Space => 1;

        public override double CalculateFitness(BuildingRule model)
        {
            var maxDistanceToCenter = 10;
            if (Position.DistanceTo(model.SettlementCenter) > maxDistanceToCenter)
            {
                //market is close enough to the settlement center
                return 0;
            }

            var buildings = model.Roads.SelectMany(b => b.Buildings).Count();
            var administrations = model.Roads.SelectMany(b => b.Buildings).Count(b => b is Administration);
            if (buildings / (administrations + 1) < 2000)
            {
                //("No more than one administration per 2000 buildings");
                return 0;
            }

            return 10;
        }
    }
}