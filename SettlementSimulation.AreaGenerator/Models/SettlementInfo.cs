﻿using System.Collections.Generic;

namespace SettlementSimulation.AreaGenerator.Models
{
    public class SettlementInfo
    {
        public Field[,] Fields { get; set; }
        public List<Point> MainRoad { get; set; }
        public Pixel[,] PreviewBitmap { get; set; }
    }
}