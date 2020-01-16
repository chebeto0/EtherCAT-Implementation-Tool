using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EtherCAT_Master.Core.UDP
{
    public struct Received
    {
        public IPEndPoint Sender;
        public byte[] Message;
    }

    class UdpBase : IDisposable
    {

        protected UdpClient Client;
        private bool disposed = false;

        protected UdpBase()
        {
            Client = new UdpClient();
        }

        public async Task<Received> Receive()
        {
            UdpReceiveResult result = await Client.ReceiveAsync();
            return new Received()
            {
                Message = result.Buffer,
                Sender = result.RemoteEndPoint
            };
        }

        public void Dispose()
        {
            if (!disposed)
            {
                Client.Close();
                Client.Dispose();
                //if (disposing)
                //{
                //}
            }
            disposed = true;
            GC.SuppressFinalize(this);
        }


    }
}
