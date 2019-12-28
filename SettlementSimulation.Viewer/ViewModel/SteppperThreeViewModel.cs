﻿using FastBitmapLib;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using SettlementSimulation.AreaGenerator.Models;
using SettlementSimulation.Engine;
using SettlementSimulation.Engine.Models;
using SettlementSimulation.Engine.Models.Buildings;
using SettlementSimulation.Viewer.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows;
using Point = SettlementSimulation.AreaGenerator.Models.Point;

namespace SettlementSimulation.Viewer.ViewModel
{
    public class StepperThreeViewModel : ViewModelBase
    {
        #region fields
        private Bitmap _colorMap;
        private Bitmap _heightMap;
        private SettlementInfo _settlementInfo;
        private StructureGenerator _generator;
        private readonly ViewModelLocator _viewModelLocator;
        #endregion

        #region properties
        private List<string> _logs;
        public List<string> Logs
        {
            get => _logs;
            set
            {
                _logs = value;
                RaisePropertyChanged();
            }
        }

        public Dictionary<Type, Tuple<Color, string>> StructuresLegend { get; set; }

        private Bitmap _settlementBitmap;
        public Bitmap SettlementBitmap
        {
            get => _settlementBitmap;
            set
            {
                _settlementBitmap = value;
                RaisePropertyChanged();
            }
        }

        private Visibility _spinnerVisibility;
        public Visibility SpinnerVisibility
        {
            get => _spinnerVisibility;
            set
            {
                _spinnerVisibility = value;
                RaisePropertyChanged();
            }
        }

        private SettlementState _settlementState;
        public SettlementState SettlementState
        {
            get => _settlementState;
            set
            {
                _settlementState = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region commands
        public RelayCommand StartSimulationCommand { get; }
        public RelayCommand StopSimulationCommand { get; }
        #endregion

        public StepperThreeViewModel()
        {
            _viewModelLocator = new ViewModelLocator();

            Messenger.Default.Register<SetSettlementInfoCommand>(this, this.SetSettlementInfo);

            Logs = new List<string>();
            StructuresLegend = new Dictionary<Type, Tuple<Color, string>>();
            SpinnerVisibility = Visibility.Hidden;

            StartSimulationCommand = new RelayCommand(StartSimulation);
            StopSimulationCommand = new RelayCommand(StopSimulation);
            SetUpStructureLegend();
        }

        private void SetSettlementInfo(SetSettlementInfoCommand obj)
        {
            _settlementInfo = obj.SettlementInfo;
            _heightMap = obj.HeightMap;
            _colorMap = obj.ColorMap;
            SettlementBitmap = new Bitmap(_colorMap);

            foreach (var point in _settlementInfo.MainRoad)
            {
                MarkPoint(point, SettlementBitmap, Color.Red, 3);
            }

        }

        private void StartSimulation()
        {
            var maxIterations = _viewModelLocator.Designer.EndY;
            var breakpoints = _viewModelLocator.Designer.Breakpoints;

            _generator = new StructureGeneratorBuilder()
                .WithMaxIterations(maxIterations)
                .WithBreakpoints(breakpoints)
                .WithFields(_settlementInfo.Fields)
                .WithMainRoad(_settlementInfo.MainRoad)
                .Build();

            _generator.Breakpoint += OnBreakpoint;
            _generator.NextEpoch += OnNextEpoch;
            _generator.Finished += OnFinished;

            _generator.Start();

            Logs.Clear();
            SpinnerVisibility = Visibility.Visible;
        }

        private void StopSimulation()
        {
            _generator.Stop();

            SpinnerVisibility = Visibility.Hidden;
        }

        private void OnBreakpoint(object sender, EventArgs e)
        {
            UpdateSettlementBitmap();
        }

        private void OnNextEpoch(object sender, EventArgs e)
        {
            UpdateSettlementBitmap();
        }

        private void OnFinished(object sender, EventArgs e)
        {
            UpdateSettlementBitmap();

            MessageBox.Show($"Execution finished");

            SpinnerVisibility = Visibility.Hidden;
        }

        private void UpdateSettlementBitmap()
        {
            SettlementState = _generator.SettlementState;
            var buildings = SettlementState.Structures
                .Where(s => s is Building).Cast<Building>().ToList();
            
            if (!buildings.Any()) return;

            _logs = new List<string>(buildings.Select(s => s.ToString()));
            
            var originalColorMap= new Bitmap(_colorMap);
            foreach (var building in buildings)
            {
                var point = building.Position;
                MarkPoint(point, originalColorMap, StructuresLegend[building.GetType()].Item1, 1);
            }

            var newBitmap = GetTrimmedBitmap(buildings, originalColorMap);
            SettlementBitmap = new Bitmap(newBitmap);

            RaisePropertyChanged(nameof(Logs));
            RaisePropertyChanged(nameof(SettlementBitmap));
        }

        private Bitmap GetTrimmedBitmap(List<Building> buildings, Bitmap bitmap)
        {
            var minX = buildings.Min(b => b.Position.X);
            var minY = buildings.Min(b => b.Position.Y);
            var maxX = buildings.Max(b => b.Position.X);
            var maxY = buildings.Max(b => b.Position.Y);

            var offset = 10;
            var width = maxX - minX + 2 * offset;
            var height = maxY - minY + 2 * offset;
            var tmpBitmap = new Bitmap(width, height);

            using (var fastBitmap = bitmap.FastLock())
            {
                for (int i = minX - offset, n = 0; i < maxX + offset; i++, n++)
                {
                    for (int j = minY - offset, m = 0; j < maxY + offset; j++, m++)
                    {
                        if (i < 0 || j < 0 || i >= fastBitmap.Width || j >= fastBitmap.Height) continue;
                        var color = fastBitmap.GetPixel(i, j);
                        tmpBitmap.SetPixel(n, m, color);
                    }
                }
            }

            return tmpBitmap;
        }

        private void SetUpStructureLegend()
        {
            var structures = Assembly.Load("SettlementSimulation.Engine")
                .GetTypes()
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Building)))
                .ToList();

            for (int i = 0; i < structures.Count(); i++)
            {
                StructuresLegend.Add(structures[i], new Tuple<Color, string>(GetRandColor(i), structures[i].Name));
            }
        }

        private void MarkPoint(Point point, Bitmap bitmap, Color color, int offset = 5)
        {
            using (var fastBitmap = bitmap.FastLock())
            {
                for (int i = -offset; i < offset; i++)
                {
                    for (int j = -offset; j < offset; j++)
                    {
                        if (point.X + i < 0 || point.X + i >= bitmap.Width || point.Y + j < 0 || point.Y + j >= bitmap.Height)
                            continue;
                        fastBitmap.SetPixel(point.X + i, point.Y + j, color);
                    }
                }
            }
        }

        private Color GetRandColor(int index)
        {
            var colors = new List<Color>
            {
                Color.FromArgb(128, 0, 0),
                Color.FromArgb(170, 110, 40),
                Color.FromArgb(128, 128, 0),
                Color.FromArgb(0, 128, 128),
                Color.FromArgb(0, 0, 128),
                Color.FromArgb(230, 25, 75),
                Color.FromArgb(245, 130, 48),
                Color.FromArgb(210, 245, 60),
                Color.FromArgb(60, 180, 75),
                Color.FromArgb(70, 240, 240),
                Color.FromArgb(0, 130, 200),
                Color.FromArgb(145, 30, 180),
                Color.FromArgb(240, 50, 230),
                Color.FromArgb(128, 128, 128),
                Color.FromArgb(250, 190, 190),
                Color.FromArgb(255, 215, 180),
                Color.FromArgb(170, 255, 195),
                Color.FromArgb(230, 190, 255),
                Color.FromArgb(200, 205, 0),
                Color.FromArgb(60, 190, 200),
                Color.FromArgb(0, 60, 60),
            };

            return colors[index % colors.Count()];
        }
    }
}
