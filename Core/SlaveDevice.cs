using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using EtherCAT_Master.Core.Communication;

namespace EtherCAT_Master.Core
{
    public enum EC_SM
    {
        EC_SM_NA            = -1,
        EC_SM_NONE          = 0,
        EC_STATE_INIT       = 1,
        EC_STATE_PRE_OP     = 2,
        EC_STATE_BOOT       = 3,
        EC_STATE_SAFE_OP    = 4,
        EC_STATE_OPER       = 8,
        EC_SM_ERROR         = 16,
    }

    public class SlaveDeviceBase : INotifyPropertyChanged
    {
        public CommunicationBase COMM;

        public string DeviceName { get; set; }

        public bool DeviceIsSelected { get; set; }

        public int SlaveNumber { get; set; }

        public bool IsSelectable { get; set; }
        
        private EC_SM _ecStateMachine;
        public EC_SM EcStateMachine
        {
            get { return _ecStateMachine; }
            set
            {
                _ecStateMachine = value;
                OnPropertyChanged("EcStateMachine");
            }
        }

        public SlaveDeviceBase()
        {
            SlaveNumber = 1;
            IsSelectable = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    
}
