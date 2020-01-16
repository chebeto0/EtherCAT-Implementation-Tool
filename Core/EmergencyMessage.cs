using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtherCAT_Master.Core
{
    public class EmergencyMessage : INotifyPropertyChanged
    {
        private string _sequenceNumber;
        public string SequenceNumber
        {
            get { return _sequenceNumber; }
            set
            {
                _sequenceNumber = value;
                OnPropertyChanged("SequenceNumber");
            }
        }

        public uint EmcyMsgID { get; set; }

        private string _msg;
        public string Msg
        {
            get { return _msg; }
            set
            {
                _msg = value;
                OnPropertyChanged("Msg");
            }
        }

        /* These are the defined Emergency messages */
        public Dictionary<uint, string> messageDict = new Dictionary<uint, string>() {
                { 0x3210, "DcLink Overvoltage Error" },
                { 0x3220, "DcLink Undervoltage Warning" },
                { 0x3221, "DcLink Undervoltage Error" },
                { 0x3110, "Logic Overvoltage Error" },
                { 0x3120, "Logic Undervoltage Error" },
                { 0x4310, "Power Stage Overtemperature Error" },
                { 0x4311, "Power Stage Overtemperature Warning" },
                { 0x5111, "Driver Undervoltage Error" },
                { 0x7121, "Blockfail" },

                { 0xFF01, "IN 1 Logic Rising Edge" },
                { 0xFF02, "IN 2 Logic Rising Edge" },
                { 0xFF03, "IN 3 Logic Rising Edge" },
                { 0xFF04, "STOB Missing" },
                { 0xFF08, "STOA Missing" },
                { 0xFF22, "No Reference" },
                { 0xFF32, "Runtime Data Loss" },

                { 0x7300, "Hardware Error Encoder" },
                { 0x5001, "Hardware Error Voltage Logic" },
                { 0x5002, "Hardware Error Voltage DcLink" },
                { 0x5003, "Hardware Error Voltage Driver" },
                { 0x5004, "Hardware Error Power Stage" },
                { 0x5005, "Hardware Error Current A" },
                { 0x5006, "Hardware Error Current B" },

                { 0x5530, "EEPROM Error" },

                { 0x6010, "Reset Watchdog" },
                { 0x6011, "Reset Power On" },
                { 0x6012, "Reset Supply Watchdog" },
                { 0x6013, "Reset Power Validation" },
                { 0x6014, "Reset CPU Lockup" },
                { 0x6015, "Reset Parity Error" },
                { 0x6016, "Reset CPU System" },

                { 0x6100, "EEPROM CRC Error" },
                { 0x6200, "EEPROM Page Empty" },
                { 0x6300, "EEPROM I2C Error" },
                { 0x6400, "EEPROM Flag Error" },
                { 0x6500, "EEPROM I2C Error Read" },
                { 0x6600, "EEPROM Flag Error Write" },

                { 0x7501, "Leaving OP While So" },

                { 0x8611, "Following Error" },

                { 0, "--" }
        };

        public EmergencyMessage()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
