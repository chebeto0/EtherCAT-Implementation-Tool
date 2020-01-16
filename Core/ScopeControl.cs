using EtherCAT_Master.Core.Dictionary;
using MahApps.Metro.IconPacks;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EtherCAT_Master.Core
{
    public class ScopeControl
    {
        public CancellationTokenSource ts_scope;
        public List<DictItem> PlotObjects = new List<DictItem>();

        private System.Timers.Timer saveTimer = new System.Timers.Timer();

        public object locker = new object();

        private PCS device;
        public PlotViewModel plotScope;

        private DateTime datenow;
        
        public HambViewModelScope Hamb1 = new HambViewModelScope();

        private double _x_0 = 0;

        public bool ScopeRunning { get; private set; }

        public ScopeControl(PCS device)
        {
            this.device = device;
            plotScope = new PlotViewModel();

            saveTimer.Elapsed += new ElapsedEventHandler(DoSaveToFile);
            saveTimer.Interval = 10000;

            Hamb1.Items = new ObservableCollection<HambMenuItemScope>
            {
                new HambMenuItemScope()
                {
                    Id = 1,
                    Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.Play },
                    IconColor = new SolidColorBrush(IntecColors.green),
                    Name = "Start Plot (F5)",
                    Vis = Visibility.Hidden
                },
                new HambMenuItemScope()
                {
                    Id = 2,
                    Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.Crosshairs },
                    IconColor = new SolidColorBrush(IntecColors.white),
                    Name = "Readjust Axes",
                    Vis = Visibility.Hidden
                },
                new HambMenuItemScope()
                {
                    Id = 3,
                    Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.ChartLine },
                    IconColor = new SolidColorBrush(IntecColors.white),
                    ChckBx = new CheckBox() { IsChecked = true },
                    Bttn = new Button() { Content = new PackIconMaterial() { Kind = PackIconMaterialKind.ArrowLeft } },
                    Combo = new ComboBox()
                    {
                        ItemsSource = device.ObjectDictionary.simpleDict,
                        SelectedValuePath = "Key",
                        DisplayMemberPath = "Value",
                        IsTextSearchEnabled = true,
                        Foreground = new SolidColorBrush(IntecColors.white),
                        Background = new SolidColorBrush(IntecColors.plotColors[0]),
                        FontSize = 14,
                        FontFamily = new FontFamily("Lucida Console")
                    }
                },
                new HambMenuItemScope()
                {
                    Id = 4,
                    Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.ChartLine },
                    IconColor = new SolidColorBrush(IntecColors.white),
                    ChckBx = new CheckBox() { IsChecked = false },
                    Bttn = new Button() { Content = new PackIconMaterial() { Kind = PackIconMaterialKind.ArrowLeft } },
                    Combo = new ComboBox()
                    {
                        ItemsSource = device.ObjectDictionary.simpleDict,
                        SelectedValuePath = "Key",
                        DisplayMemberPath = "Value",
                        IsTextSearchEnabled = true,
                        Foreground = new SolidColorBrush(IntecColors.white),
                        Background = new SolidColorBrush(IntecColors.plotColors[1]),
                        FontSize = 14,
                        FontFamily = new FontFamily("Lucida Console")
                    }
                },
                new HambMenuItemScope()
                {
                    Id = 5,
                    Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.Download },
                    IconColor = new SolidColorBrush(IntecColors.white),
                    ChckBx = new CheckBox() { IsChecked = false },
                    Bttn = new Button() { Content = new PackIconMaterial() { Kind = PackIconMaterialKind.ArrowLeft } },
                    Combo = new ComboBox()
                    {
                        ItemsSource = device.ObjectDictionary.simpleDict,
                        SelectedValuePath = "Key",
                        DisplayMemberPath = "Value",
                        IsTextSearchEnabled = true,
                        Foreground = new SolidColorBrush(IntecColors.white),
                        Background = new SolidColorBrush(IntecColors.plotColors[2]),
                        FontSize = 14,
                        FontFamily = new FontFamily("Lucida Console")
                    }
                },
                new HambMenuItemScope()
                {
                    Id = 6,
                    Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.ExitToApp },
                    IconColor = new SolidColorBrush(IntecColors.white),
                    Name = "",
                    Vis = Visibility.Hidden
                }
            };
            
            Hamb1.Items[2].Combo.SelectionChanged += HambMenuCombo_SelectionChanged;
            Hamb1.Items[2].ChckBx.Checked += ChckBx_Checked;
            Hamb1.Items[2].ChckBx.Unchecked += ChckBx_Unchecked;
            Hamb1.Items[2].Bttn.Click += Bttn_Click;
            Hamb1.Items[3].Combo.SelectionChanged += HambMenuCombo_SelectionChanged;
            Hamb1.Items[3].ChckBx.Checked += ChckBx_Checked;
            Hamb1.Items[3].ChckBx.Unchecked += ChckBx_Unchecked;
            Hamb1.Items[3].Bttn.Click += Bttn_Click;
            Hamb1.Items[4].Combo.SelectionChanged += HambMenuCombo_SelectionChanged;
            Hamb1.Items[4].ChckBx.Checked += ChckBx_Checked;
            Hamb1.Items[4].ChckBx.Unchecked += ChckBx_Unchecked;
            Hamb1.Items[4].Bttn.Click += Bttn_Click;

        }

        
        /// <summary>
        /// When an elemen of a combobox in the hamburger mennu is selected the data of the dictionary of said element is set to the information of the device that is needed for polling the certain object for the scope.
        /// It gets the index, subindex, data type (signed and length) and the name of the object
        /// </summary>
        private void HambMenuCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var CmbBx = sender as ComboBox;
                int ID = (CmbBx.DataContext as HambMenuItemScope).Id - 3;
                var key = CmbBx.SelectedValue as string;
                plotScope.PlotObjects[ID] = device.ObjectDictionary.dictOfCoE[key];
                CmbBx.Background = new SolidColorBrush(IntecColors.plotColors[ID]);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }


        /// <summary>
        /// Triggers when the checkbox of in the scope hamburger menu is checked. It makes the series of the selected item visible on the plot.
        /// </summary>
        private void ChckBx_Checked(object sender, RoutedEventArgs e)
        {
            var Item = (HambMenuItemScope)(e.Source as CheckBox).DataContext;
            plotScope.PlotModel.Series[Item.Id - 3].IsVisible = true;
            //scopeSeriesEnabled[Item.Id - 3] = true;
        }

        /// <summary>
        /// Triggers when the checkbox of in the scope hamburger menu is unchecked. It makes the series of the selected item NOT visible on the plot.
        /// </summary>
        private void ChckBx_Unchecked(object sender, RoutedEventArgs e)
        {
            var Item = (HambMenuItemScope)(e.Source as CheckBox).DataContext;
            plotScope.PlotModel.Series[Item.Id - 3].IsVisible = false;
            //scopeSeriesEnabled[Item.Id - 3] = false;
        }
        
        /// <summary>
        /// This function makes the element on which the arrow button was clicked to switch between the left (axisY1) and right (axisY2) Y axes on the plot
        /// </summary>
        private void Bttn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var item = (HambMenuItemScope)(sender as Button).DataContext;

                string yAxis = (plotScope.PlotModel.Series[item.Id - 3] as LineSeries).YAxisKey;

                if (yAxis == "axisY1") //If the element is currently set to the first Y axis it will be set to the second one
                {
                    (plotScope.PlotModel.Series[item.Id - 3] as LineSeries).YAxisKey = "axisY2";
                    item.Bttn.Content = new PackIconMaterial() { Kind = PackIconMaterialKind.ArrowRight };
                    plotScope.PlotModel.Axes[2].IsAxisVisible = true;
                }
                else if (yAxis == "axisY2") //If the element is currently set to the second Y axis it will be set to the first one
                {
                    (plotScope.PlotModel.Series[item.Id - 3] as LineSeries).YAxisKey = "axisY1";
                    item.Bttn.Content = new PackIconMaterial() { Kind = PackIconMaterialKind.ArrowLeft };
                    plotScope.PlotModel.Axes[2].IsAxisVisible = false;
                    for (int i = 0; i < plotScope.PlotModel.Series.Count; i++)
                    {
                        if ((plotScope.PlotModel.Series[i] as LineSeries).YAxisKey == "axisY2")
                        {
                            plotScope.PlotModel.Axes[2].IsAxisVisible = true;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }

        public void ScopeTask()
        {
            ts_scope = new CancellationTokenSource();
            CancellationToken ct = ts_scope.Token;

            Task.Factory.StartNew(async () =>
            {
                double _x = 0;
                
                bool flag = false;

                ScopeRunning = true;
                
                await Task.Delay(50);

                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        ScopeRunning = false;
                        saveTimer.Stop();
                        break; /* jump out of while loop to stop task */
                    }

                    for (int i = 0; i < plotScope.PlotModel.Series.Count; i++)
                    {
                        lock (locker)
                        {
                            if (plotScope.PlotObjects[i].Index != 0/*(plotScope.PlotModel.Series[i] as LineSeries).IsVisible*/)
                            {
                                DictItem item = device.ObjectDictionary.GetItem(plotScope.PlotObjects[i].Index, plotScope.PlotObjects[i].Subindex);
                                if (!flag)
                                {
                                    plotScope.PlotModel.Axes[0].Maximum = 10;
                                    plotScope.PlotModel.Axes[0].Minimum = 0;
                                    _x_0 = Convert.ToDouble(item.TimeStamp) / 1000.0;
                                    datenow = DateTime.Now;
                                    saveTimer.Start();
                                    flag = true;
                                }
                                LineSeries line = (plotScope.PlotModel.Series[i] as LineSeries);
                                _x = Convert.ToDouble(item.TimeStamp) / 1000.0;
                                if (line.Points.Count == 0 || (_x - _x_0) != line.Points[line.Points.Count - 1].X)
                                {
                                    line.Points.Add(new DataPoint(_x - _x_0, Convert.ToDouble(item.Value)));
                                }
                            }

                            if ((plotScope.PlotModel.Series[i] as LineSeries).Points.Count > 4000)
                            {
                                (plotScope.PlotModel.Series[i] as LineSeries).Points.RemoveRange(0, 1);
                            }
                        }
                    }

                    if (_x - _x_0 >= plotScope.PlotModel.Axes[0].Minimum + 10)
                    {
                        plotScope.PlotModel.Axes[0].Maximum += 0.5;
                        plotScope.PlotModel.Axes[0].Minimum += 0.5;
                    }

                    await Task.Delay(5);
                }

            }, ct);
        }

        public void DoSaveToFile(object source, ElapsedEventArgs e)
        {
            try
            {
                string path = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);

                if (plotScope.PlotObjects[2].Index == 0)
                {
                    return;
                }

                string filename = string.Format("{0:yyyyMMdd_HHmmss}_{1:X4}_{2:X2}.csv",
                                                datenow, 
                                                plotScope.PlotObjects[2].Index, 
                                                plotScope.PlotObjects[2].Subindex,
                                                CultureInfo.CurrentCulture);

                path = path + @"\Saves\"+ filename;
                
                if (plotScope.PlotObjects[2].Index != 0)
                {
                    DictItem item = device.ObjectDictionary.GetItem(plotScope.PlotObjects[2].Index, plotScope.PlotObjects[2].Subindex);

                    WriteCSV(new CsvWriteItem { TimeStamp = (Math.Round(Convert.ToDouble(item.TimeStamp) / 1000.0,0) - Convert.ToUInt32(_x_0)), Value = item.Value }, path);
                }
            }
            catch (Exception err)
            {
                saveTimer.Stop();
                MessageBox.Show(err.ToString());
                MessageBox.Show("Something went wrong and saving process has been stopped. If you think the issue has been solved, stop and start the scope again.");
            }
        }

        public void WriteCSV<T>(T item, string path)
        {
            Type itemType = typeof(T);
            
            var properties = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .OrderBy(p => p.Name);

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var writer = new StreamWriter(path,true))
            {
                writer.WriteLine(string.Join(";", properties.Select(p => p.GetValue(item, null))));
            }
        }
    }


    class CsvWriteItem
    {

        public double TimeStamp { get; set; }
        public object Value { get; set; }

    }

    
}
