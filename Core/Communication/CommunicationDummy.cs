using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtherCAT_Master.Core.Communication
{

    /*
     * Placeholder class needed at initialization of app
     */

    class CommunicationDummy : CommunicationBase
    {
        
        public override void AsyncRead(int slave_number, ushort idx, byte subidx) { }

        public override void AsyncWrite(int slave_number, ushort index, byte subindex, object value) { }

        public override void Disconnect() { }

    }
}
