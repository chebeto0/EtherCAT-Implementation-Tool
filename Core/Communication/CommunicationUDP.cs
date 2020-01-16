using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using EtherCAT_Master.Core;
using EtherCAT_Master.Core.UDP;
using EtherCAT_Master.Core.Controls;
using EtherCAT_Master.Core.Dictionary;

namespace EtherCAT_Master.Core.Communication
{
    static class CMD
    {
        public const byte sdo_R = 0x00;
        public const byte sdo_W = 0x01;
        public const byte pdo_R = 0x10;
        public const byte pdo_W = 0x11;
    }


    public class CommunicationUDP : CommunicationBase
    {
        private UdpUser client;
        private Received received;

        public string IpAddress = "192.168.1.10";
        private static readonly ushort _port = 0xCCCC;

        public CancellationTokenSource ts_cycle;
        
        private struct SdoObject
        {
            public byte   appl;
            public ushort index;
            public byte   subindex;
            public byte[] data;
        }
        
        public CommunicationUDP(UdpCommControl udpctl, CheckBox checkboxEoeRw)
        {
            MW = udpctl.MW;
            Devices.Clear();
            Devices.Add(new PCS(this, 1, "Intec PCS", MW.ObjectDictionary));
            (Devices[0] as PCS).EcStateMachine = EC_SM.EC_SM_NA;
            IpAddress = udpctl.Ipaddress;
            
            //create a new client
            client = UdpUser.ConnectTo(IpAddress, _port);
            commType = CommType.COMM_UDP;
            Connected = true;

            WriteFlag = checkboxEoeRw.IsChecked.Value;

            MmTimer = new MultimediaTimer { Interval = 100 };
            
            Task<byte[]> ret;

            byte[] send_client_msg = new byte[182];
            
            ts_cycle = new CancellationTokenSource();
            CancellationToken ct = ts_cycle.Token;

            Task.Factory.StartNew(async () =>
            {

                while (true)
                {
                    if (ct.IsCancellationRequested) /* TRUE when ts_cyle.Cancel() is called */
                    {
                        break; /* Get out of loop to end thread*/
                    }

                    send_client_msg = new byte[182];

                    if (Connected && Devices[0] is PCS device)
                    {
                        
                        InsertPdoWriteControlword();    /* PDO 1 */
                        InsertPdoReadStatusword();      /* PDO 2 */
                        InsertPdoWriteTargetPosition(); /* PDO 3 */
                        InsertPdoReadActualPosition();  /* PDO 4 */
                        InsertPdoWriteTargetVelocity(); /* PDO 5 */
                        InsertPdoReadActualVelocity();  /* PDO 6 */
                        InsertPdoWriteTargetMode();     /* PDO 7 */
                        InsertPdoReadModeDisp();        /* PDO 8 */

                        InsertScopeObjects();

                        PutBufferInSendArray(ref send_client_msg);
                        
                        ret = SendUdp(send_client_msg); // This must be only thread to call SendUDP
                        
                        GetObjectsFromBuffer(ret.Result);

                    }

                    await Task.Delay(1);
                }
            }, ct);
            
        }
        
        private void GetObjectsFromBuffer(byte[] rcv_client_msg)
        {
            byte cmd, subindex;
            ushort index;
            ushort num_obj = BitConverter.ToUInt16(rcv_client_msg, 0);
            uint timestamp = BitConverter.ToUInt32(rcv_client_msg, 2);
            
            DictItem item;

            try
            {
                PCS device = Devices[0] as PCS;
            
                int pointer = 6 ;//+ _num_pdos * 8;
            
                for (int i = 1; i <= num_obj; i++)
                {
                    cmd = rcv_client_msg[pointer];
                    pointer += 1;
                    index = BitConverter.ToUInt16(rcv_client_msg, pointer);
                    pointer += 2;
                    subindex = rcv_client_msg[pointer];
                    pointer += 1;

                    try
                    {
                        item = MW.ObjectDictionary.GetItem(index, subindex);
                    }
                    catch(Exception err)
                    {
                        MessageBox.Show(err.ToString());
                        continue;
                    }
                
                    switch (cmd) /* For the meaning of the commands see the definition of the static class CMD */
                    {
                        case CMD.sdo_R://0x00:
                            switch (item.Type)
                            {
                                
                                case "SINT":
                                    item.Value = unchecked((sbyte)rcv_client_msg[pointer]);
                                    break;
                                case "BOOL":
                                case "USINT":
                                    item.Value = rcv_client_msg[pointer];
                                    break;
                                case "INT":
                                    item.Value = BitConverter.ToInt16(rcv_client_msg, pointer);
                                    break;
                                case "UINT":
                                    item.Value = BitConverter.ToUInt16(rcv_client_msg, pointer);
                                    break;
                                case "DINT":
                                    item.Value = BitConverter.ToInt32(rcv_client_msg, pointer);
                                    break;
                                case "UDINT":
                                    item.Value = BitConverter.ToUInt32(rcv_client_msg, pointer);
                                    break;
                                case "STRING":
                                    item.Value = rcv_client_msg.SubArray(pointer, item.Length);
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                            DictionaryBuilder.FormatDisplayString(item);
                            item.TimeStamp = timestamp;
                            break;
                        case CMD.sdo_W://0x01:
                            uint abort_ret = BitConverter.ToUInt32(rcv_client_msg, pointer);
                            
                            if (abort_ret == 0)
                            {
                                item.Access = "RWG";
                            }
                            else
                            {
                                item.Access = "RWR";

                                MW.Dispatcher.Invoke(() =>
                                {
                                    MW.ErrorNoti.Items.Insert(0, new ErrorNotifications
                                    {
                                        TimeStamp = DateTime.Now.ToLongTimeString(),
                                        Notification = string.Format("SDO Abort Transfer: {0} (0x{1:X8})",
                                        MW.ErrorNoti.AbortCodes[abort_ret], abort_ret)
                                    });
                                });

                            }
                            
                            break;
                        case CMD.pdo_R://0x10:
                            if (item.Index == 0x6041)
                            {
                                device.stateMachineDsp402.StateWord = BitConverter.ToUInt16(rcv_client_msg, pointer);
                            }
                            else if (item.Index == 0x6064)
                            {
                                device.ActualPosition = BitConverter.ToInt32(rcv_client_msg, pointer);
                            }
                            else if (item.Index == 0x606C)
                            {
                                device.ActualVelocity = BitConverter.ToInt32(rcv_client_msg, pointer);
                            }
                            else if (item.Index == 0x6061)
                            {
                                device.CurrentMode = unchecked((sbyte)rcv_client_msg[pointer]);
                            }
                            break;
                        case CMD.pdo_W://0x11:

                            break;
                        default:
                            break;
                    }
                    pointer += Math.Max(item.Length,4);
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }

        private void InsertScopeObjects()
        {
            PCS device = Devices[0] as PCS;
            List<DictItem> PlotObjects =  device.scopeControl.plotScope.PlotObjects;

            for (int i = 0; i < device.scopeControl.plotScope.PlotModel.Series.Count; i++)
            {
                if ( device.scopeControl.ScopeRunning && PlotObjects[i].Index != 0)
                {
                    FillServiceBuffer(CMD.sdo_R, PlotObjects[i].Index, PlotObjects[i].Subindex, new byte[4]);
                }
            }
        }

        private void PutBufferInSendArray(ref byte[] send_client_msg)
        {
            int num_obj = BitConverter.ToUInt16(send_client_msg, 0);
            int cnt = 6 + 8*num_obj;

            lock (comm_locker)
            {
                while (object_buffer.Count > 0 && (cnt + 8 + object_buffer[0].data.Length) < send_client_msg.Length)
                {
                    BitConverter.GetBytes(object_buffer[0].appl).CopyTo(send_client_msg, cnt);
                    cnt += 1;
                    BitConverter.GetBytes(object_buffer[0].index).CopyTo(send_client_msg, cnt);
                    cnt += 2;
                    BitConverter.GetBytes(object_buffer[0].subindex).CopyTo(send_client_msg, cnt);
                    cnt += 1;
                    object_buffer[0].data.CopyTo(send_client_msg, cnt);
                    cnt += object_buffer[0].data.Length;

                    object_buffer.RemoveAt(0);
                    num_obj++;
                }
            }

            BitConverter.GetBytes((ushort)num_obj).CopyTo(send_client_msg, 0);
        }
        
        private List<SdoObject> object_buffer = new List<SdoObject>();
        public void FillServiceBuffer(byte app, ushort idx, byte subidx, byte[] data)
        {
            PCS device = Devices[0] as PCS;

            if ((app & 0x01) == 1 && !WriteFlag) //do not allow writes if write flag is not set.
            {
                return;
            }

            SdoObject sdoObj = new SdoObject { appl = app, index = idx, subindex = subidx, data = data };
            lock (comm_locker)
            {
                object_buffer.Add(sdoObj);
            }
        }
        
        private void InsertPdoWriteControlword()
        {
            PCS device = Devices[0] as PCS;
            byte[] buf = new byte[4];
            
            BitConverter.GetBytes(device.Controlword).CopyTo(buf, 0);
               
            FillServiceBuffer(CMD.pdo_W, 0x6040, 0, buf);
        }

        private void InsertPdoReadStatusword()
        {
            byte[] buf = new byte[4];

            FillServiceBuffer(CMD.pdo_R, 0x6041, 0, buf);
        }

        private void InsertPdoWriteTargetPosition()
        {
            PCS device = Devices[0] as PCS;
            byte[] buf = new byte[4];

            BitConverter.GetBytes(device.TargetPosition).CopyTo(buf, 0);

            FillServiceBuffer(CMD.pdo_W, 0x607A, 0, buf);
        }

        private void InsertPdoReadActualPosition()
        {
            byte[] buf = new byte[4];

            FillServiceBuffer(CMD.pdo_R, 0x6064, 0, buf);
        }

        private void InsertPdoWriteTargetVelocity()
        {
            PCS device = Devices[0] as PCS;
            byte[] buf = new byte[4];

            BitConverter.GetBytes(device.TargetVelocity).CopyTo(buf, 0);

            FillServiceBuffer(CMD.pdo_W, 0x60FF, 0, buf);
        }

        private void InsertPdoReadActualVelocity()
        {
            byte[] buf = new byte[4];

            FillServiceBuffer(CMD.pdo_R, 0x606C, 0, buf);
        }
        
        private void InsertPdoWriteTargetMode()
        {
            PCS device = Devices[0] as PCS;
            byte[] buf = new byte[4];

            BitConverter.GetBytes(device.TargetMode).CopyTo(buf, 0);

            FillServiceBuffer(CMD.pdo_W, 0x6060, 0, buf);
        }
        
        private void InsertPdoReadModeDisp()
        {
            byte[] buf = new byte[4];

            FillServiceBuffer(CMD.pdo_R, 0x6061, 0, buf);
        }

        public async Task<byte[]> SendUdp(byte[] send_client_msg)
        {
            try
            {
                client.Send(send_client_msg);
                received = await client.Receive();
            }
            catch (Exception err)
            {
                Console.WriteLine(err);
            }

            return received.Message;
        }
        
        public override void AsyncWrite(int slave_number, ushort index, byte subindex, object value)
        {
            try
            {
                DictItem item = MW.ObjectDictionary.GetItem(index, subindex);
                var len = Math.Max(item.Length, 4);
                byte[] buf = new byte[len];

                switch (item.Type)
                {
                    case "SINT":
                        BitConverter.GetBytes((sbyte)value).CopyTo(buf, 0);
                        break;
                    case "BOOL":
                    case "USINT":
                        BitConverter.GetBytes((byte)value).CopyTo(buf, 0);
                        break;
                    case "INT":
                        BitConverter.GetBytes((short)value).CopyTo(buf, 0);
                        break;
                    case "UINT":
                        BitConverter.GetBytes((ushort)value).CopyTo(buf, 0);
                        break;
                    case "DINT":
                        BitConverter.GetBytes((int)value).CopyTo(buf, 0);
                        break;
                    case "UDINT":
                        BitConverter.GetBytes((uint)value).CopyTo(buf, 0);
                        break;
                    case "STRING":
                        ((byte[])value).CopyTo(buf, 0);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                FillServiceBuffer(CMD.sdo_W, index, subindex, buf);
            }
            catch(Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }
        
        public override void AsyncRead(int slave_number, ushort index, byte subindex)
        {
            var len = Math.Max(MW.ObjectDictionary.GetItem(index, subindex).Length, 4);
            FillServiceBuffer(CMD.sdo_R, index, subindex, new byte[len]);
        }
        
        public override void Disconnect()
        {
            ts_cycle.Cancel();
            Connected = false;
            client.Dispose();
        }
        

    }
    
    
}
