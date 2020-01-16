using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EtherCAT_Master.Core.UDP
{
    class UdpUser : UdpBase
    {
        private UdpUser() { }

        public static UdpUser ConnectTo(string hostname, int port)
        {
            var connection = new UdpUser();
            connection.Client.Connect(IPAddress.Parse(hostname), port);
            return connection;
        }

        public void Send(byte[] message)
        {
            Client.Send(message, message.Length);
        }


    }
}
