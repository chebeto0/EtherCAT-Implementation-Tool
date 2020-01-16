using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtherCAT_Master.Core
{
    public class OMBitsLabel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string _name_om;
        public string Name_om
        {
            get { return _name_om; }
            set
            {
                _name_om = value;
                OnPropertyChanged("Name_om");
            }
        }
        public string _name_bit10;
        public string Name_bit10
        {
            get { return _name_bit10; }
            set
            {
                _name_bit10 = value;
                OnPropertyChanged("Name_bit10");
            }
        }
        public string _name_bit12;
        public string Name_bit12
        {
            get { return _name_bit12; }
            set
            {
                _name_bit12 = value;
                OnPropertyChanged("Name_bit12");
            }
        }
        public string _name_bit13;
        public string Name_bit13
        {
            get { return _name_bit13; }
            set
            {
                _name_bit13 = value;
                OnPropertyChanged("Name_bit13");
            }
        }

        public OMBitsLabel(string nom, string nb10, string nb12, string nb13)
        {
            Name_om = nom;
            Name_bit10 = nb10;
            Name_bit12 = nb12;
            Name_bit13 = nb13;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
