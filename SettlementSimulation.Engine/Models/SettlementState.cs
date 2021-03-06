﻿using SettlementSimulation.Engine.Interfaces;
using System.Collections.Generic;
using SettlementSimulation.AreaGenerator.Models;
using SettlementSimulation.Engine.Enumerators;

namespace SettlementSimulation.Engine.Models
{
    public class SettlementState
    { 
        public Epoch CurrentEpoch { get; set; }
        public int CurrentIteration { get; set; }
        public IRoad MainRoad { get; set; }
        public List<IRoad> Roads { get; set; }
        public Point SettlementCenter { get; set; }
        public SettlementUpdate LastSettlementUpdate { get; set; }
        public int Time { get; set; }
    }
}