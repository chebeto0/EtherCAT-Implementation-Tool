using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtherCAT_Master.Core
{
    public class ErrorNotifications
    {
        public string TimeStamp { get; set; }

        //public string TimeStampString { get; set; }

        public string Notification { get; set; }
    }


    public class ErrorNotificationsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ErrorNotifications> _items;
        public ObservableCollection<ErrorNotifications> Items
        {
            get { return _items; }
            set
            {
                _items = value;
                OnPropertyChanged("Items");
            }
        }

        public Dictionary<uint, string> AbortCodes = new Dictionary<uint, string>
        {
            {0x05030000, "Toggle bit not changed" },
            {0x05040000, "SDO protocol timeout" },
            {0x05040001, "Client/Server command specifier not valid or unknown" },
            {0x05040005, "Out of memory" },
            {0x06010000, "Unsupported access to an object" },
            {0x06010001, "Attempt to read to write only object" },
            {0x06010002, "Attempt to write to read only object" },
            {0x06010003, "Subindex cannot be written, SI0 must be 0 for write access" },
            {0x06010004, "SDO Complete access not supported for objects of variable length" },
            {0x06010005, "Object length exceeds mailbox size" },
            {0x06010006, "Object mapped to RxPDO, SDO Download blocked" },
            {0x06020000, "The object does not exist in the object directory" },
            {0x06040041, "The object cannot be mapped into the PDO" },
            {0x06040042, "The number and length of the objects to be mapped would exceed the PDO length." },
            {0x06040043, "Geeral parameter incompatibility reason" },
            {0x06040047, "General internal incompatibility in the device" },
            {0x06060000, "Access failed due to hardware error" },
            {0x06070010, "Data type does not match, length of service parameter does not match" },
            {0x06070012, "Data type does not match, length of service parameter too high" },
            {0x06070013, "Data type does not match, length of service parameter too low" },
            {0x06090011, "Subindex does not exist" },
            {0x06090030, "Value range of parameter exceeded" },
            {0x06090031, "Value of parameter written too high" },
            {0x06090032, "Value of parameter written too low" },
            {0x06090036, "Maximum value is less than minimum value" },
            {0x08000000, "General error" },
            {0x08000020, "Data cannot be transferred or stored to the application" },
            {0x08000021, "Data cannot be transferred or stored to the application because of local control" },
            {0x08000022, "Data cannot be transferred or stored to the application because of the present device state" },
            {0x08000023, "Object dictionary dynamic generation fails or no object is present" },
        };

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
