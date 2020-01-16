using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace EtherCAT_Master.Core
{
    public class GaugeItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public int MinVal { get; set; }
        private int _maxVal;
        public int MaxVal
        {
            get { return _maxVal; }
            set
            {
                _maxVal = value;
                OnPropertyChanged("MaxVal");
            }
        }
        public int DivisionsCount { get; set; }
        private int _optStart;
        public int OptStart
        {
            get { return _optStart; }
            set
            {
                _optStart = value;
                OnPropertyChanged("OptStart");
            }
        }

        private int _optEnd;
        public int OptEnd
        {
            get { return _optEnd; }
            set
            {
                _optEnd = value;
                OnPropertyChanged("OptEnd");
            }
        }

        public string Unit { get; set; }
        public ushort Index { get; set; }
        public byte Subindex { get; set; }
        public int DisplayFactor { get; set; }

        private double _currentValue;
        public double CurrentValue
        {
            get { return _currentValue; }
            set
            {
                _currentValue = value;
                OnPropertyChanged("CurrentValue");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
