using EtherCAT_Master.Core.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtherCAT_Master.Core
{
    class OtherVendorDevice : SlaveDeviceBase
    {
        
        public OtherVendorDevice(CommunicationBase comm, int slave_number, string name)
        {
            SlaveNumber = slave_number;
            DeviceName = name;
            IsSelectable = false;
            COMM = comm;
        }
        
    }
}
