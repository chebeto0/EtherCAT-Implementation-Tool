namespace EtherCAT_Master.Core
{
    using System;
    using System.ComponentModel;
    using OxyPlot;
    using OxyPlot.Axes;
    using OxyPlot.Series;
    using System.Collections.Generic;
    using System.Windows;
    using EtherCAT_Master.Core.Dictionary;

    public class PlotViewModel : INotifyPropertyChanged, IDisposable
    {
        private bool disposed;

        //public List<bool> ScopeSeriesEnabled { get; set; }
        public int TotalNumberOfPoints { get; private set; }

        public PlotModel PlotModel { get; private set; }

        public PlotViewModel()
        {
            SetupModel();
        }

        private void SetupModel()
        {
            PlotModel = new PlotModel() {
                LegendFontSize =14,
                LegendFontWeight = OxyPlot.FontWeights.Bold,
                LegendBorder = OxyColor.FromRgb(0,0,0),
                LegendBackground = OxyColor.FromRgb(0xff, 0xff, 0xff)
            };

            PlotModel.Axes.Add(new LinearAxis {
                Minimum = 0,
                Maximum = 10,
                MajorGridlineStyle = LineStyle.Solid,
                Title = "Time [s]",
                MaximumPadding =0,
                Position = AxisPosition.Bottom
            });

            PlotModel.Axes.Add(new LinearAxis {
                Key = "axisY1",
                IntervalLength = 30.0,
                MajorGridlineStyle = LineStyle.Solid,
                MinimumPadding = 0.05,
                MaximumPadding = 0.25,
                MajorGridlineThickness = 1.0,
                Position = AxisPosition.Left,
                StringFormat = "N0"
            });
            PlotModel.Axes.Add(new LinearAxis {
                Key = "axisY2",
                IntervalLength = 30.0,
                //MajorGridlineStyle = LineStyle.Solid,
                MinimumPadding = 0.05,
                MaximumPadding = 0.25,
                MajorGridlineThickness = 1.0,
                Position = AxisPosition.Right,
                IsAxisVisible = false,
                StringFormat = "N0"
            });

            PlotModel.Series.Add(new LineSeries {
                IsVisible = true,
                LineStyle = LineStyle.Solid,
                YAxisKey = "axisY1",
                MarkerStroke = OxyColor.FromRgb(IntecColors.plotColors[0].R, IntecColors.plotColors[0].G, IntecColors.plotColors[0].B),
                MarkerSize = 3,
                MarkerStrokeThickness = 2,
                MarkerResolution = 0,
                MarkerFill = OxyColors.Automatic,
                Color = OxyColor.FromRgb(IntecColors.plotColors[0].R, IntecColors.plotColors[0].G, IntecColors.plotColors[0].B)
            });

            PlotModel.Series.Add(new LineSeries {
                IsVisible = false,
                LineStyle = LineStyle.Solid,
                YAxisKey = "axisY1",
                MarkerStroke = OxyColor.FromRgb(IntecColors.plotColors[1].R, IntecColors.plotColors[1].G, IntecColors.plotColors[1].B),
                MarkerSize = 3,
                MarkerStrokeThickness = 2,
                MarkerResolution = 0,
                MarkerFill = OxyColors.Automatic,
                Color = OxyColor.FromRgb(IntecColors.plotColors[1].R, IntecColors.plotColors[1].G, IntecColors.plotColors[1].B)
            });

            PlotModel.Series.Add(new LineSeries {
                IsVisible = false,
                LineStyle = LineStyle.Solid,
                YAxisKey = "axisY1",
                MarkerStroke = OxyColor.FromRgb(IntecColors.plotColors[2].R, IntecColors.plotColors[2].G, IntecColors.plotColors[2].B),
                MarkerSize = 3,
                MarkerStrokeThickness = 2,
                MarkerResolution = 0,
                MarkerFill = OxyColors.Automatic,
                Color = OxyColor.FromRgb(IntecColors.plotColors[2].R, IntecColors.plotColors[2].G, IntecColors.plotColors[2].B)
            });

            PlotObjects.Add(new DictItem());
            PlotObjects.Add(new DictItem());
            PlotObjects.Add(new DictItem());

            OnPropertyChanged("PlotModel");
        }

        public void AddSeries()
        {
            PlotModel.Series.Add(new LineSeries {
                IsVisible = true,
                LineStyle = LineStyle.Solid,
                YAxisKey = "axisY1",
                MarkerStroke = OxyColor.FromRgb(IntecColors.plotColors[PlotModel.Series.Count].R, IntecColors.plotColors[PlotModel.Series.Count].G, IntecColors.plotColors[PlotModel.Series.Count].B),
                MarkerSize = 3,
                MarkerStrokeThickness = 2,
                MarkerResolution = 0,
                MarkerFill = OxyColors.Automatic,
                Color = OxyColor.FromRgb(IntecColors.plotColors[PlotModel.Series.Count].R, IntecColors.plotColors[PlotModel.Series.Count].G, IntecColors.plotColors[PlotModel.Series.Count].B)
            });
            //PlotModel.Axes[2].IsAxisVisible = true;
            OnPropertyChanged("PlotModel");
        }

        public void RemoveSeries(int rmvidx)
        {
            PlotModel.Series.RemoveAt(rmvidx);
            for (int i = 0; i < PlotModel.Series.Count; i++)
            {
                (PlotModel.Series[i] as LineSeries).Color = OxyColor.FromRgb(IntecColors.plotColors[i].R, IntecColors.plotColors[i].G, IntecColors.plotColors[i].B);
            }
            if (PlotModel.Series.Count < 2)
            {
                PlotModel.Axes[2].IsAxisVisible = false;
            }

            OnPropertyChanged("PlotModel");
        }

        public void ShowSeriesMarks()
        {
            foreach( LineSeries ser in PlotModel.Series )
            {
                ser.MarkerType = MarkerType.Plus;
            }
            //OnPropertyChanged("PlotModel");
        }

        public void ToggleSeriesMarks()
        {
            foreach (LineSeries ser in PlotModel.Series)
            {
            }
        }

        public void HideSeriesMarks()
        {
            foreach (LineSeries ser in PlotModel.Series)
            {
                ser.MarkerType = MarkerType.None;
            }
        }
        
        public int plot_window = 10;
        public void Update(List<LineSeries> plot_input)
        {
            int n = 0;
            try
            {
                for (int i = 0; i < plot_input.Count; i++)
                {
                    var s = (LineSeries)PlotModel.Series[i];

                    int len = plot_input[i].Points.Count;
                    if (enabled)
                    {
                        s.Points.AddRange(plot_input[i].Points);

                        if (s.MaxX - s.MinX > 20)
                        {
                            s.Points.RemoveRange(0, len);
                        }

                        n += s.Points.Count;
                    }
                }
            }
            catch(Exception err)
            {
                MessageBox.Show(err.ToString());
            }
            
            if (TotalNumberOfPoints != n)
            {
                TotalNumberOfPoints = n;
                OnPropertyChanged("TotalNumberOfPoints");
            }
        }

        //public CancellationTokenSource ts_scope;
        //public Stopwatch stopwatch = new Stopwatch();
        public List<DictItem> PlotObjects = new List<DictItem>();

        public void StartStop()
        {
            enabled = enabled ? false : true;
        }

        public bool enabled = true;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                }
            }

            disposed = true;
        }
    }

    public class ObjectToPlotInterface
    {
        ushort  Index { get; set; }
        byte    Subindex { get; set; }

        bool SeriesEnables { get; set; }

    }
}