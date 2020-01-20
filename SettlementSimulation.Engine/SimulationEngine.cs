﻿using System;
using System.Collections.Generic;
using System.Linq;
using SettlementSimulation.AreaGenerator.Models;
using SettlementSimulation.Engine.Interfaces;
using SettlementSimulation.Engine.Enumerators;
using SettlementSimulation.Engine.Helpers;
using SettlementSimulation.Engine.Models;

namespace SettlementSimulation.Engine
{
    public class SimulationEngine
    {

        #region properties
        public Epoch CurrentEpoch { get; set; }
        public int Generation { get; set; }
        public Field[,] Fields { get; }
        public List<Point> MainRoad { get; }
        public SettlementUpdate LastSettlementUpdate { get; set; }
        public Settlement Settlement { get; set; }
        #endregion

        public SimulationEngine(
            Field[,] fields,
            List<Point> mainRoad)
        {
            Fields = fields;
            MainRoad = mainRoad;
            Generation = 1;
            Settlement = new Settlement(fields, mainRoad);
            CurrentEpoch = Epoch.First;
        }

        public void NewGeneration()
        {
            var structures = Enumerable.Range(1, 100).ToList()
                .Select(s => Settlement.CreateNewSettlementUpdate(CurrentEpoch))
                .ToList();

            var settlementUpdate = GetBestStructures(structures);

            LastSettlementUpdate = settlementUpdate;

            if (settlementUpdate.NewRoads.Any())
            {
                var roadTypeSetUp = new RoadTypeSetUp()
                {
                    Epoch = CurrentEpoch,
                    SettlementCenter = Settlement.SettlementCenter,
                    AvgDistanceToSettlementCenter =
                        (int) Settlement.Genes.Average(r => r.Center.DistanceTo(Settlement.SettlementCenter))
                };
                settlementUpdate.NewRoads
                    .ForEach(r =>
                    {
                        r.SetUpRoadType(roadTypeSetUp);
                        Settlement.AddRoad(r);
                    });
            }

            if (settlementUpdate.NewBuildingsAttachedToRoad.Any())
            {
                settlementUpdate.BuildingRemovedFromRoad.ForEach(b =>
                    {
                        Settlement.RemoveBuildingFromRoad(b.road, b.building);
                    });

                settlementUpdate.NewBuildingsAttachedToRoad
                    .ForEach(b =>
                    {
                        Settlement.AddBuildingToRoad(b.road, b.building);
                    });
            }

            this.Settlement.Buildings.ForEach(b => b.Age++);

            if (RandomProvider.NextDouble() < 0.001)
            {
                settlementUpdate.FloodMutationResult = Settlement.InvokeFloodMutation();
            }

            if (RandomProvider.NextDouble() < 0.01)
            {
                settlementUpdate.EarthquakeMutationResult = Settlement.InvokeEarthquakeMutation();
            }

            if (EpochSpecific.CanEnterNextEpoch(Settlement, CurrentEpoch))
            {
                switch (CurrentEpoch)
                {
                    case Epoch.First:
                        CurrentEpoch = Epoch.Second;
                        break;
                    case Epoch.Second:
                        CurrentEpoch = Epoch.Third;
                        break;
                    case Epoch.Third:
                        //TODO
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            Generation++;
        }

        private SettlementUpdate GetBestStructures(List<SettlementUpdate> structures)
        {
            var structuresFitness = structures.ToDictionary(s => s, s => 0);
            foreach (var structure in structures)
            {
                var fitness = CalculateFitness(structure);
                structuresFitness[structure] = fitness;
            }

            var bests = structuresFitness.OrderByDescending(s => s.Value).Take(2).Select(u => u.Key).ToArray();
            var settlementUpdate = bests[0].Crossover(bests[1]);

            return settlementUpdate;
        }

        private int CalculateFitness(SettlementUpdate model)
        {
            var fitness = 0;
            foreach (var road in model.NewRoads)
            {
                var roads = new List<IRoad>(Settlement.Genes) { road };
                road.Buildings.ForEach(b => b.SetFitness(new BuildingRule()
                {
                    Fields = Settlement.Fields,
                    Roads = roads,
                    BuildingRoad = road,
                    SettlementCenter = Settlement.SettlementCenter
                }));
                fitness += road.Buildings.Sum(b => b.Fitness);
            }

            foreach (var (building, road) in model.NewBuildingsAttachedToRoad)
            {
                var roads = new List<IRoad>(Settlement.Genes);
                building.SetFitness(new BuildingRule()
                {
                    Fields = Settlement.Fields,
                    Roads = roads,
                    BuildingRoad = road,
                    SettlementCenter = Settlement.SettlementCenter
                });
                fitness += building.Fitness;
            }

            return fitness;
        }
    }
}