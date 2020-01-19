using EtherCAT_Master.Core.Controls;
using EtherCAT_Master.Core.Dictionary;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace EtherCAT_Master.Core.Communication
{
    /// <summary>
    /// Struct defined to be the structure of the data returned by the low-level soem.dll
    /// used to return wkcs and abort codes.
    /// </summary>
    public struct Ret
    {
        public int wkc;
        public uint abort_code;
    }

    public class CommunicationSOEM : CommunicationBase
    {
        public int wkc_pdo;
        
        public static byte[] buffer = new byte[1024];
        private Stopwatch stopwatch = new Stopwatch();
        private System.Timers.Timer timerUpdatePdoProperties = new System.Timers.Timer();
        public int wkc_wathchdog = 0;

        public CommunicationSOEM(EtherCatControl ecatctl)
        {
            SlaveCount = 0;
            Connected = false;
            
            MW = ecatctl.MW; /* Get reference to the Main Window instance */

            commType = CommType.COMM_ECAT; /* set communication type to EtherCAT */

            stopwatch.Start();
        }
        
        /// <summary>
        /// Starts the 
        /// </summary>
        public void StartPdoExchangeTask()
        {
            /* Declare multimedia Timer with 20 ms interval */
            MmTimer = new MultimediaTimer { Interval = 1 };

            /* Timer event in charge of the exchange of PDO data and for the calling of Scope SDOs */
            MmTimer.Elapsed += async (o, e) =>
            {
                /* Call an exchange of PDO */
                wkc_pdo = EcSendRecieveProcessdataExtern();

                for (int i = 0; i < Devices.Count; i++) 
                {
                    if (Devices[i] is PCS device)
                    {
                        device.pdo_output_map.controlword = device.Controlword;

                        device.pdo_output_map.target_position = device.TargetPosition;
                        device.pdo_output_map.target_velocity = device.TargetVelocity;
                        device.pdo_output_map.modes_of_oper = device.TargetMode;

                        /* From low-level soem code copy the pdo data */
                        EcCopyPdos(device.SlaveNumber, ref device.pdo_input_map, device.pdo_output_map);

                        device.stateMachineDsp402.StateWord = device.pdo_input_map.statusword;
                        //device.CurrentMode = device.pdo_input_map.modes_of_oper_disp;
                        //device.ActualPos = device.pdo_input_map.actual_position;
                        //device.ActualVel = device.pdo_input_map.actual_velocity;
                    }
                }

                wkc_wathchdog = (wkc_pdo <= 0) ? wkc_wathchdog + 1 : 0; /* check for worker counter, increase watchdog if worker counter is <=0 */

                if (wkc_wathchdog > 25)
                {
                    Disconnect();
                    await MW.ShowMessageAsync("Client Disconnected", "Lost contact to device.");
                }
            };
            
            /* start multimedia timer */
            MmTimer.Start();

            StartScopeTask();

            timerUpdatePdoProperties.Elapsed += new ElapsedEventHandler(DoTimeEventUpdateProperties); //Non realtime relevant stuff
            timerUpdatePdoProperties.Interval = 50; // in ms
            timerUpdatePdoProperties.Enabled = true;
            timerUpdatePdoProperties.AutoReset = true;

        }

        public CancellationTokenSource ts_scope;

        private void StartScopeTask()
        {
            ts_scope = new CancellationTokenSource();
            CancellationToken ct = ts_scope.Token;

            Task.Factory.StartNew(async () =>
            {

                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }
                    PCS device = Devices[MW.SelectedDevice] as PCS;

                    if (device.scopeControl.ScopeRunning)
                    {
                        ReadScopeObjects();
                    }
                    else
                    {
                        await Task.Delay(10);
                    }
                    
                }
            }, ct);

        }


        /// <summary>
        /// Called to request SDOs for the scope if the scope is running and an scope object is selected
        /// </summary>
        private void ReadScopeObjects()
        {
            PCS device = Devices[MW.SelectedDevice] as PCS;
            List<DictItem> PlotObjects = device.scopeControl.plotScope.PlotObjects;

            for (int i = 0; i < device.scopeControl.plotScope.PlotModel.Series.Count; i++)
            {
                if (device.scopeControl.ScopeRunning
                    && PlotObjects[i].Index != 0)
                //&& (device.plotScope.PlotModel.Series[i] as LineSeries).IsVisible )
                {
                    AsyncRead(device.SlaveNumber, PlotObjects[i].Index, PlotObjects[i].Subindex);
                }
            }
        }

        /// <summary>
        /// Updates the properties of the devices that were obtained frim the PDOs
        /// (Except for statusword because statusword is more "Real-time")
        /// </summary>
        public void DoTimeEventUpdateProperties(object source, ElapsedEventArgs e)
        {
            for (int i = 0; i < Devices.Count; i++) 
            {
                if (Devices[i] is PCS device)
                {
                    device.CurrentMode = device.pdo_input_map.modes_of_oper_disp;
                    device.ActualPosition = device.pdo_input_map.actual_position;
                    device.ActualVelocity = device.pdo_input_map.actual_velocity;
                }
            }
        }
        
        /// <summary>
        /// Stop cyclic processes of the communication, set devices to init,
        /// close socket and set Connected to false
        /// </summary>
        public override async void Disconnect()
        {
            try
            {
                timerUpdatePdoProperties.Enabled = false;

                for (int i = 0; i < Devices.Count; i++) //
                {
                    if (Devices[i] is PCS device)
                    {
                        device.timerNonRealTime.Enabled = false;
                    }
                }

                ts_scope.Cancel();
                MmTimer.Stop();

                await Task.Delay(25);

                await Task.Run(() =>
                {
                    try
                    {
                        EcSmRequestStateAll(EC_SM.EC_STATE_INIT);
                        EcCloseSocket();
                    }
                    catch (Exception err)
                    {
                        MessageBox.Show(err.ToString());
                    }
                });
                Connected = false;
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }

        /* Declaration of low level Sdo functions */
        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern Ret SdoWriteString(int slave_number, ushort idx, byte subidx, byte[] buf, int size);

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void SdoReadString(int slave_number, ushort idx, byte subidx, byte[] buf, int size);

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern Ret SdoWriteUInt32(int slave_number, ushort index, byte subindex, uint value);

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern uint SdoReadUInt32(int slave_number, ushort index, byte subindex);

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern Ret SdoWriteInt32(int slave_number, ushort index, byte subindex, int value);

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int SdoReadInt32(int slave_number, ushort index, byte subindex);

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern Ret SdoWriteUInt16(int slave_number, ushort index, byte subindex, ushort value);

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern ushort SdoReadUInt16(int slave_number, ushort index, byte subindex);

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern Ret SdoWriteInt16(int slave_number, ushort index, byte subindex, short value);

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short SdoReadInt16(int slave_number, ushort index, byte subindex);

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern Ret SdoWriteUInt8(int slave_number, ushort index, byte subindex, byte value);

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern byte SdoReadUInt8(int slave_number, ushort index, byte subindex);

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern Ret SdoWriteInt8(int slave_number, ushort index, byte subindex, sbyte value);

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern sbyte SdoReadInt8(int slave_number, ushort index, byte subindex);

        /// <summary>
        /// Definition of the AsyncRead funcion for the SOEM functionality.
        /// It takes the slave number to communicate to, the index, and subindex of an object
        /// and it will call the respective needed low level SDO function to read the value of the object.
        /// The return value will be set on the value of the object dictionary
        /// </summary>
        /// <param name="slave_number">number of position of the slave to address</param>
        /// <param name="index">index of the object to read</param>
        /// <param name="subindex">subindex of the object to read</param>
        public override void AsyncRead(int slave_number, ushort index, byte subindex)
        {
            try
            {
                DictItem item = MW.ObjectDictionary.GetItem(index, subindex);

                switch (item.Type)
                {
                    case "SINT":
                        lock (comm_locker)
                            item.Value = SdoReadInt8(slave_number, index, subindex);
                        break;
                    case "BOOL":
                    case "USINT":
                        lock(comm_locker)
                            item.Value = SdoReadUInt8(slave_number, index, subindex);
                        break;
                    case "INT":
                        lock (comm_locker)
                            item.Value = SdoReadInt16(slave_number, index, subindex);
                        break;
                    case "UINT":
                        lock (comm_locker)
                            item.Value = SdoReadUInt16(slave_number, index, subindex);
                        break;
                    case "DINT":
                        lock (comm_locker)
                            item.Value = SdoReadInt32(slave_number, index, subindex);
                        break;
                    case "UDINT":
                        lock (comm_locker)
                            item.Value = SdoReadUInt32(slave_number, index, subindex);
                        break;
                    case "STRING":
                        byte[] buf = new byte[item.Length];
                        lock (comm_locker)
                            SdoReadString(slave_number, index, subindex, buf, item.Length);
                        item.Value = buf;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                DictionaryBuilder.FormatDisplayString(item); /* formats the display string of the value (dec, bin, hex) */
                item.TimeStamp = Convert.ToUInt32(stopwatch.ElapsedMilliseconds);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }

        /// <summary>
        /// Definition of the AsyncWrite funcion for the SOEM functionality.
        /// It takes the slave number to communicate to, the index, and subindex of an object
        /// and it will call the respective needed low level SDO function to write the given
        /// value to the object.
        /// 
        /// </summary>
        /// <param name="slave_number">>number of position of the slave to address</param>
        /// <param name="index">index of the object to write</param>
        /// <param name="subindex">subindex of the obejct to write</param>
        /// <param name="value">value to write to the obejct</param>
        public override void AsyncWrite(int slave_number, ushort index, byte subindex, object value)
        {
            try
            {
                DictItem item = MW.ObjectDictionary.GetItem(index, subindex);
                Ret ret = new Ret();

                switch (item.Type)
                {
                    case "SINT":
                        lock (comm_locker)
                            ret = SdoWriteInt8(slave_number, index, subindex, (sbyte)value);
                        break;
                    case "BOOL":
                    case "USINT":
                        lock (comm_locker)
                            ret = SdoWriteUInt8(slave_number, index, subindex, (byte)value);
                        break;
                    case "INT":
                        lock (comm_locker)
                            ret = SdoWriteInt16(slave_number, index, subindex, (short)value);
                        break;
                    case "UINT":
                        lock (comm_locker)
                        {

                            if (index== 0x6040 && subindex == 0)
                            {
                                (Devices[slave_number - 1] as PCS).Controlword = (ushort)value;
                            }

                            ret = SdoWriteUInt16(slave_number, index, subindex, (ushort)value);
                        }
                        break;
                    case "DINT":
                        lock (comm_locker)
                            ret = SdoWriteInt32(slave_number, index, subindex, (int)value);
                        break;
                    case "UDINT":
                        lock (comm_locker)
                            ret = SdoWriteUInt32(slave_number, index, subindex, (uint)value);
                        break;
                    case "STRING":
                        byte[] buf = new byte[item.Length];
                        ((byte[])value).CopyTo(buf, 0);
                        lock (comm_locker)
                            ret = SdoWriteString(slave_number, index, subindex, buf, item.Length);
                        break;
                    default:
                        throw new NotImplementedException();
                }
                
                if (ret.wkc > 0)
                {
                    item.Access = "RWG";
                }
                else
                {
                    item.Access = "RWR";

                     /* Use MainWindow dispatcher because Objects here are in aother thread */
                    MW.Dispatcher.Invoke(() =>
                    {
                        MW.ErrorNoti.Items.Insert(0, new ErrorNotifications
                        {
                            TimeStamp = DateTime.Now.ToLongTimeString(),
                            Notification = string.Format("SDO Abort Transfer: {0} (0x{1:X8})",
                            MW.ErrorNoti.AbortCodes[ret.abort_code], ret.abort_code)
                        });
                    });
                    
                }

            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }
        
        /// <summary>
        /// Calls a low level function that opens the socket and starts the ethercat communication with
        /// the slaves.
        /// </summary>
        /// <param name="slave_number"></param>
        /// <param name="network_adapter_number"></param>
        /// <param name="buf"></param>
        /// <returns></returns>
        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern ushort ec_connect_extern(int slave_number, int network_adapter_number, byte[] buf);
        public ushort EcConnectExtern(int slave_number, int network_adapter_number, byte[] buf)
        {
            lock (comm_locker)
                return ec_connect_extern(slave_number, network_adapter_number, buf);
        }

        /// <summary>
        /// closes the communication socket
        /// </summary>
        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void ec_disconnect_extern();
        public void EcDisconnectExtern()
        {
            lock (comm_locker)
                ec_disconnect_extern();
        }
        
        /// <summary>
        /// Low level function to make a send/receive of the Process Data
        /// </summary>
        /// <returns>return worker counter</returns>
        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int ec_send_recieve_processdata_extern();
        public int EcSendRecieveProcessdataExtern()
        {
            lock (comm_locker)
                return ec_send_recieve_processdata_extern();
        }
        
        //////////////////////// EtherCAT State Machine functions /////////////////////
        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern EC_SM ec_sm_request_state_extern(int slave_number, EC_SM req_state);

        public static void EcSmRequestStateAll(EC_SM req_state)
        {
            //if (Connected)
                //lock (comm_locker)
                    ec_sm_request_state_extern(0, req_state);
            
        }

        public static void EcSmRequestState(PCS device, EC_SM req_state)
        {
            //lock (comm_locker)
                device.EcStateMachine = ec_sm_request_state_extern(device.SlaveNumber, req_state);
            
        }

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern EC_SM ec_sm_request_state_boot_extern(int slave_number);
        public static void EcSmRequestStateBoot(PCS device)
        {
            //lock (comm_locker)
                device.EcStateMachine = ec_sm_request_state_boot_extern(device.SlaveNumber);
        }
        ///////////////////////////////////////////////////////////////////////////////////

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern bool ec_close_socket();
        public bool EcCloseSocket()
        {
            if (Connected)
                lock (comm_locker)
                    return ec_close_socket();

            return false;
        }

        
        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int foe_fw_update(int sln, string fw_full_file_path, string fw_file_name, uint password);
        public int FoeFwUpdate(int sln, string fw_full_file_path, string fw_file_name, uint password)
        {
            if(Connected)
                lock (comm_locker)
                    return foe_fw_update(sln, fw_full_file_path, fw_file_name, password);

            return 0;
        }

        /// <summary>
        /// Stzabcs for devices
        /// </summary>
        /// <param name="num_network_adapter">The index of the network adapter that should be scanned</param>
        /// <param name="buf">to store the names</param>
        /// <returns></returns>
        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int ScanForEcDevices(int num_network_adapter, byte[] buf);
        public int EcScanForEcDevices(int num_network_adapter, byte[] buf)
        {
            lock (comm_locker)
                return ScanForEcDevices(num_network_adapter, buf);
        }

        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void ec_copy_pdos(int slave_number,ref PCS.PdoInput inputs, PCS.PdoOutput outputs);
        public void EcCopyPdos(int num_network_adapter,ref PCS.PdoInput inputs, PCS.PdoOutput outputs)
        {
            if (Connected)
                lock (comm_locker)
                    ec_copy_pdos(num_network_adapter,ref inputs, outputs);
        }

        /// <summary>
        /// Performs firmware update. To be honest, I don't know why I made it static. It could also be 
        /// non-static, i guess. 
        /// </summary>
        /// <param name="slave_number"></param>
        /// <param name="password"></param>
        /// <param name="fw_full_file_path"></param>
        /// <param name="fw_file_name"></param>
        /// <returns></returns>
        public static int DoFirmwareUpdate(int slave_number, uint password, string fw_full_file_path, string fw_file_name)
        {
            //if (Connected)
            //ret = FoeFwUpdate(slave_number, fw_full_file_path, fw_file_name, password);
            return foe_fw_update(slave_number, fw_full_file_path, fw_file_name, password);
        }


    }

}