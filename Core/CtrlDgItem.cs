using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EtherCAT_Master.Core
{
    public class CtrlDgItem : INotifyPropertyChanged
    {

        public int Id { get; set; }
        private string _current_position { get; set; }
        public string CurrentPosition
        {
            get { return _current_position; }
            set
            {
                if (_current_position != value)
                {
                    _current_position = value;
                    OnPropertyChanged("CurrentPosition");
                }
            }
        }
        private double _target_position;
        public double TargetPosition
        {
            get { return _target_position; }
            set
            {
                if (_target_position != value)
                {
                    _target_position = value;
                    OnPropertyChanged("TargetPosition");
                }
            }
        }
        private double _position_slider;
        public double PositionSlider
        {
            get { return _position_slider; }
            set
            {
                if (_position_slider != value)
                {
                    _position_slider = value;
                    OnPropertyChanged("PositionSlider");
                }
            }
        }
        private double _velocity;
        public double Velocity
        {
            get { return _velocity; }
            set
            {
                if (_velocity != value)
                {
                    _velocity = value;
                    OnPropertyChanged("Velocity");
                }
            }
        }
        private double _velocity_slider;
        public double VelocitySlider
        {
            get { return _velocity_slider; }
            set
            {
                if (_velocity_slider != value)
                {
                    _velocity_slider = value;
                    OnPropertyChanged("Velocity");
                }
            }
        }
        private double _acceleration;
        public double Acceleration
        {
            get { return _acceleration; }
            set
            {
                if (_acceleration != value)
                {
                    _acceleration = value;
                    OnPropertyChanged("Acceleration");
                }
            }
        }
        private double _acceleration_slider;
        public double AccelerationSlider
        {
            get { return _acceleration_slider; }
            set
            {
                if (_acceleration_slider != value)
                {
                    _acceleration_slider = value;
                    OnPropertyChanged("Acceleration");
                }
            }
        }
        private double _deceleration;
        public double Deceleration
        {
            get { return _deceleration; }
            set
            {
                if (_deceleration != value)
                {
                    _deceleration = value;
                    OnPropertyChanged("Deceleration");
                }
            }
        }
        private double _deceleration_slider;
        public double DecelerationSlider
        {
            get { return _deceleration_slider; }
            set
            {
                if (_deceleration_slider != value)
                {
                    _deceleration_slider = value;
                    OnPropertyChanged("Deceleration");
                }
            }
        }
        private readonly string[] _ramp_text = new string[2] { "Linear", "sin\xB2" };
        private short _ramp_type;
        public short RampType
        {
            get { return _ramp_type; }
            set
            {
                _ramp_type = value;
                RampTypeText = _ramp_text[_ramp_type];
                OnPropertyChanged("RampType");
            }
        }
        private string _ramp_type_text;
        public string RampTypeText
        {
            get { return _ramp_type_text; }
            set
            {
                if (_ramp_type_text != value)
                {
                    _ramp_type_text = value;
                    OnPropertyChanged("RampTypeText");
                }
            }
        }
        private PackIconMaterial _add_remove_text;
        public PackIconMaterial AddRemoveText
        {
            get { return _add_remove_text; }
            set
            {
                if (_add_remove_text != value)
                {
                    _add_remove_text = value;
                    OnPropertyChanged("AddRemoveText");
                }
            }
        }
        public Visibility Vis { get; set; }
        public SolidColorBrush ButtonColor { get; set; }

        private int _time_wait;
        public int TimeWait
        {
            get { return _time_wait; }
            set
            {
                if (_time_wait != value)
                {
                    _time_wait = value;
                    OnPropertyChanged("TimeWait");
                }
            }
        }

        public double TimeMinimum { get; set; }
        public double TimeInterval { get; set; }
        public double TickPos { get; set; }
        public double TickVel { get; set; }
        public double TickAcc { get; set; }
        public double TickDec { get; set; }

        private int _max_pos_slide;
        public int MaxPosSlide
        {
            get { return _max_pos_slide; }
            set
            {
                if (_max_pos_slide != value)
                {
                    _max_pos_slide = value;
                    OnPropertyChanged("MaxPosSlide");
                }
            }
        }

        private int _min_pos_slide;
        public int MinPosSlide
        {
            get { return _min_pos_slide; }
            set
            {
                if (_min_pos_slide != value)
                {
                    _min_pos_slide = value;
                    OnPropertyChanged("MinPosSlide");
                }
            }
        }

        private int _max_vel_slide;
        public int MaxVelSlide
        {
            get { return _max_vel_slide; }
            set
            {
                if (_max_vel_slide != value)
                {
                    _max_vel_slide = value;
                    OnPropertyChanged("MaxVelSlide");
                }
            }
        }
        private int _min_vel_slide;
        public int MinVelSlide
        {
            get { return _min_vel_slide; }
            set
            {
                if (_min_vel_slide != value)
                {
                    _min_vel_slide = value;
                    OnPropertyChanged("MinVelSlide");
                }
            }
        }
        private int _max_acc_slide;
        public int MaxAccSlide
        {
            get { return _max_acc_slide; }
            set
            {
                if (_max_acc_slide != value)
                {
                    _max_acc_slide = value;
                    OnPropertyChanged("MaxAccSlide");
                }
            }
        }
        private int _min_acc_slide;
        public int MinAccSlide
        {
            get { return _min_acc_slide; }
            set
            {
                if (_min_acc_slide != value)
                {
                    _min_acc_slide = value;
                    OnPropertyChanged("MinAccSlide");
                }
            }
        }
        private int _max_dec_slide;
        public int MaxDecSlide
        {
            get { return _max_dec_slide; }
            set
            {
                if (_max_dec_slide != value)
                {
                    _max_dec_slide = value;
                    OnPropertyChanged("MaxDecSlide");
                }
            }
        }
        private int _min_dec_slide;
        public int MinDecSlide
        {
            get { return _min_dec_slide; }
            set
            {
                if (_min_dec_slide != value)
                {
                    _min_dec_slide = value;
                    OnPropertyChanged("MinDecSlide");
                }
            }
        }

        public ProgressBar PB { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
