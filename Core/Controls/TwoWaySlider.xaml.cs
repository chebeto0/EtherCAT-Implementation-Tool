using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EtherCAT_Master.Core.Controls
{
    /// <summary>
    /// Interaction logic for TwoWaySlider.xaml
    /// </summary>
    public partial class TwoWaySlider : UserControl
    {

        private Track track;
        private RepeatButton decButton;
        private RepeatButton incButton;
        private Thumb thumb;
        private double FSize { get; set; }

        public TwoWaySlider()
        {
            this.InitializeComponent();
            this.Loaded += new RoutedEventHandler(TWSlider_Loaded);
        }

        private void TWSlider_Loaded(object sender, RoutedEventArgs e)
        {
            track = (Track)this.TWSlider.Template.FindName("PART_Track", this.TWSlider);
            decButton = track.DecreaseRepeatButton;
            incButton = track.IncreaseRepeatButton;
            thumb = track.Thumb;
            this.LayoutUpdated += new EventHandler(RangeSlider_LayoutUpdated);
        }

        void RangeSlider_LayoutUpdated(object sender, EventArgs e)
        {

            if (TWSlider.Value < -99999)
                TWSlider.FontSize = 8.5;
            else
                TWSlider.FontSize = 10;

            GridC0.MaxWidth = LayoutRoot.ActualWidth / 2;
            GridC11.MaxWidth = LayoutRoot.ActualWidth / 2;

            SetProgressBorder();
        }

        private void SetProgressBorder()
        {
            GridX.Width = decButton.ActualWidth * 1.5;
            GridY.Width = incButton.ActualWidth * 1.5;
        }
        
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public double Value
        {
            get { return (double)GetValue(MaximumProperty); }
            set
            {
                SetValue(ValueProperty, value);
            }
        }

        public double LargeChange
        {
            get { return (double)GetValue(LargeChangeProperty); }
            set
            {
                SetValue(LargeChangeProperty, value);
            }
        }
        public double TickFrequency
        {
            get { return (double)GetValue(TickFrequencyProperty); }
            set
            {
                SetValue(TickFrequencyProperty, value);
            }
        }
        

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(TwoWaySlider), new UIPropertyMetadata(0d, new PropertyChangedCallback(PropertyChanged)));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(TwoWaySlider), new UIPropertyMetadata(10d, new PropertyChangedCallback(PropertyChanged)));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(TwoWaySlider), new UIPropertyMetadata(100d, new PropertyChangedCallback(PropertyChanged)));

        public static readonly DependencyProperty LargeChangeProperty =
            DependencyProperty.Register("LargeChange", typeof(double), typeof(TwoWaySlider), new UIPropertyMetadata(100d, new PropertyChangedCallback(PropertyChanged)));

        public static readonly DependencyProperty TickFrequencyProperty =
            DependencyProperty.Register("TickFrequency", typeof(double), typeof(TwoWaySlider), new UIPropertyMetadata(100d, new PropertyChangedCallback(PropertyChanged)));
        

        private static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TwoWaySlider slider = (TwoWaySlider)d;

            //slider.SetProgressBorder();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {

        }
    }
}
