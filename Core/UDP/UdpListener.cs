using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EtherCAT_Master.Core.UDP
{
    class UdpListener : UdpBase
    {
        private readonly IPEndPoint _listenOn;

        public UdpListener() : this(new IPEndPoint(IPAddress.Any, 0xCCCC))
        {
        }

        public UdpListener(IPEndPoint endpoint)
        {
            _listenOn = endpoint;
            Client = new UdpClient(_listenOn);
        }

        public void Reply(byte[] message, IPEndPoint endpoint)
        {
            Client.Send(message, message.Length, endpoint);
        }


    }
}
