using System;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using EtherCAT_Master.Core.Communication;
using EtherCAT_Master.Core.Dictionary;

namespace EtherCAT_Master.Core
{
    public class PCS : SlaveDeviceBase
    {
        private int _act_pos_cnt;
        private int _actualPosition;
        public int ActualPosition
        {
            get { return _actualPosition; }
            set
            {
                _actualPosition = value;
                if (_act_pos_cnt > 1)
                {
                    OnPropertyChanged("ActualPosition");
                    _act_pos_cnt = 0;
                }
                _act_pos_cnt++;
            }
        }
        private int _act_vel_cnt;
        private int _actualVelocity;
        public int ActualVelocity
        {
            get { return _actualVelocity; }
            set
            {
                _actualVelocity = value;
                if (_act_vel_cnt>1)
                {
                    OnPropertyChanged("ActualVelocity");
                    _act_vel_cnt = 0;
                }
                _act_vel_cnt++;
            }
        }
        public int TargetPosition { get; set; }
        public int TargetVelocity { get; set; }
        private ushort _current_eb;
        public ushort CurrentEB
        {
            get { return _current_eb; }
            set
            {
                _current_eb = value;
                HaltBit = (_current_eb & 0x0100) == 0x0100;
            }
        } //Extra Bits: means these bits 
        private bool _halt_bit;
        public bool HaltBit {
            get { return _halt_bit; }
            set
            {
                _halt_bit = value;

                if (_current_mode != OM_PVM )
                {
                    PvmDispIcon = true;
                }
                else
                {
                    if(_halt_bit)
                    {
                        PvmDispIcon = true;
                    }
                    else
                    {
                        PvmDispIcon = false;
                    }
                }
                OnPropertyChanged("HaltBit");
            }
        }
        private bool _pvm_disp_icon;
        public bool PvmDispIcon
        {
            get { return _pvm_disp_icon; }
            set
            {
                _pvm_disp_icon = value;
                OnPropertyChanged("PvmDispIcon");
            }
        }
        public ushort CurrentCW { get; set; }
        public ushort Controlword { get; set; }
        public OMBitsLabel OmBits { get; set; }
        private sbyte _current_mode;
        public sbyte CurrentMode
        {
            get { return _current_mode; }
            set
            {
                if (_current_mode != value)
                {
                    _current_mode = value;
                    switch (_current_mode)
                    {
                        case 0:
                            OmBits.Name_om = "Operation Set to OFF";
                            OmBits.Name_bit10 = "SW Bit10";
                            OmBits.Name_bit12 = "SW Bit12";
                            OmBits.Name_bit13 = "SW Bit13";
                            break;
                        case 1:
                            OmBits.Name_om = "Profile Position Mode";
                            OmBits.Name_bit10 = "Target Reached";
                            OmBits.Name_bit12 = "Set-Point Acknowledged";
                            OmBits.Name_bit13 = "Following Error";
                            break;
                        case 3:
                            OmBits.Name_om = "Profile Velocity Mode";
                            OmBits.Name_bit10 = "Target Reached";
                            OmBits.Name_bit12 = "Speed = 0";
                            OmBits.Name_bit13 = "Max Slippage Reached";
                            break;
                        case 6:
                            OmBits.Name_om = "Homing Mode";
                            OmBits.Name_bit10 = "Target Reached";
                            OmBits.Name_bit12 = "Homing Attained";
                            OmBits.Name_bit13 = "Homing Error";
                            break;
                        default:
                            OmBits.Name_om = "Unsupported OM";
                            OmBits.Name_bit10 = "Reserved";
                            OmBits.Name_bit12 = "Reserved";
                            OmBits.Name_bit13 = "Reserved";
                            break;
                    }
                    
                    if (_current_mode != OM_PVM)
                    {
                        PvmDispIcon = true;
                    }
                    else
                    {
                        if (_halt_bit)
                        {
                            PvmDispIcon = true;
                        }
                        else
                        {
                            PvmDispIcon = false;
                        }

                    }
                }
            }
        }
        public sbyte TargetMode { get; set; }
        public short Temperature { get; set; }
        public uint ManufacturerStatus { get; set; }

        // MODES OF OPERATION
        public const sbyte OM_OFF = 0; // # Off
        public const sbyte OM_PPM = 1; //# Profile Position Mode
        // #OM_VM = 2 # Velocity Mode (not supported)
        public const sbyte OM_PVM = 3; //# Profile Velocity Mode
        // #OM_PTM = 4 # Profile Torque Mode (not supported)
        public const sbyte OM_HM = 6; // # Homing Mode
        public const sbyte OM_CSP = 8; // # CSP Mode
        public const sbyte OM_STP = -4; // # Stepper Mode

        public Timer timerNonRealTime = new Timer();

        public StateMachine stateMachineDsp402 = new StateMachine();
  
        public ObservableCollection<GaugeItem> Gauges { get; set; }

        public DictionaryBuilder ObjectDictionary;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PdoOutput
        {
            public ushort controlword;
            public int target_position;
            public int target_velocity;
            public short target_torque;
            public sbyte modes_of_oper;
            public uint digital_outputs;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PdoInput
        {
            public ushort statusword;
            public int actual_position;
            public int actual_velocity;
            public short actual_torque;
            public sbyte modes_of_oper_disp;
            public uint digital_inputs;
        }
        
        public PdoInput pdo_input_map;
        public PdoOutput pdo_output_map;
        
        public ScopeControl scopeControl;

        public PCS(CommunicationBase comm, int slave_number, string name, DictionaryBuilder dictionary)
        {
            
            SlaveNumber = slave_number;
            DeviceName = name;
            IsSelectable = true;
            COMM = comm;
            ObjectDictionary = dictionary;

            scopeControl = new ScopeControl(this);

            stateMachineDsp402 = new StateMachine();

            EcStateMachine = EC_SM.EC_STATE_OPER;

            pdo_input_map = new PdoInput
            {
                statusword = 0,
                actual_position = 0,
                actual_velocity = 0,
                actual_torque = 0,
                modes_of_oper_disp = 0,
                digital_inputs = 0
            };

            pdo_output_map = new PdoOutput
            {
                controlword = 0,
                target_position = 0,
                target_velocity = 0,
                target_torque = 0,
                modes_of_oper = 0,
                digital_outputs = 0
            };
            
            Gauges = new ObservableCollection<GaugeItem>();

            OmBits = new OMBitsLabel("No OM Selected", "SW Bit10", "SW Bit12", "SW Bit13");
            ActualPosition = 0;
            CurrentCW = 0;
            CurrentMode = 0;
            
            timerNonRealTime.Elapsed += new ElapsedEventHandler(DoTimeEventNonRealTime); /*Non realtime relevant stuff*/
            timerNonRealTime.Interval = 100; /* in milliseconds */
            timerNonRealTime.Enabled = false;
            timerNonRealTime.AutoReset = true;

            Gauges.Add(new GaugeItem()
            {
                Name = "Temperature",
                Index = 0x200D,
                Subindex = 0x00,
                Unit = "°C",
                MinVal = 0,
                MaxVal = 120,
                DivisionsCount = 10,
                OptStart = 0,
                OptEnd = 85,
                DisplayFactor = 1
            });
            Gauges.Add(new GaugeItem()
            {
                Name = "DC Link Circuit Voltage",
                Index = 0x2079,
                Subindex = 0x01,
                Unit = "V",
                MinVal = 0,
                MaxVal = 76,
                DivisionsCount = 10,
                OptStart =40,
                OptEnd = 60,
                DisplayFactor = 1000
            });
            
            EmcyMsgs = new ObservableCollection<EmergencyMessage>
            {
                new EmergencyMessage() { SequenceNumber = "0", EmcyMsgID = 0, Msg = "--" },
                new EmergencyMessage() { SequenceNumber = "0", EmcyMsgID = 0, Msg = "--" },
                new EmergencyMessage() { SequenceNumber = "0", EmcyMsgID = 0, Msg = "--" },
                new EmergencyMessage() { SequenceNumber = "0", EmcyMsgID = 0, Msg = "--" },
                new EmergencyMessage() { SequenceNumber = "0", EmcyMsgID = 0, Msg = "--" },
                new EmergencyMessage() { SequenceNumber = "0", EmcyMsgID = 0, Msg = "--" },
                new EmergencyMessage() { SequenceNumber = "0", EmcyMsgID = 0, Msg = "--" },
                new EmergencyMessage() { SequenceNumber = "0", EmcyMsgID = 0, Msg = "--" },
            };
        }
        
        /// <summary>
        /// Gets a list of class DictItem and polls the data of each objectm from the device via SDO.
        /// </summary>
        /// <param name="selectedItems">List to class DictItem to be polled via SDO.</param>
        public void UpdateDictionaryValues(ObservableCollection<DictItem> selectedItems)
        {
            foreach (DictItem si in selectedItems)
            {
                SdoRead(si.Index, si.Subindex);
            }
        }

        //public int NumEmcyMsgs { get; set; }

        public ObservableCollection<EmergencyMessage> EmcyMsgs = new ObservableCollection<EmergencyMessage>();
        
        /// <summary>
        /// This 
        /// </summary>
        public async void DoTimeEventNonRealTime(object source, ElapsedEventArgs e)
        {
            try
            {
                timerNonRealTime.Interval = 750;

                if ( EcStateMachine == EC_SM.EC_STATE_OPER
                    || EcStateMachine == EC_SM.EC_STATE_SAFE_OP
                    || EcStateMachine == EC_SM.EC_STATE_PRE_OP
                    || COMM.commType == CommType.COMM_UDP )
                {
                    uint temp;

                    SdoRead(0x1002, 0x00); /* Read manufacturer status register */

                    SdoRead(Gauges[0].Index, Gauges[0].Subindex); /* Read the default value for the 0th Gauge */
                    SdoRead(Gauges[1].Index, Gauges[1].Subindex); /* Read the default value for the 1th Gauge */

                    for (byte i = 1; i <= EmcyMsgs.Count; i++)
                    {
                        SdoRead(0x1003, i); /* Ask all the emergency messages */
                    }

                    await Task.Delay(150); /* Delay to await responses (because EoE is asyncronous) */

                    /* Get value from object dictionary */
                    ManufacturerStatus = (uint)ObjectDictionary.GetItem(0x1002, 0x00).Value;

                    /* Get values from object dictionary */
                    Gauges[0].CurrentValue = Convert.ToDouble(ObjectDictionary.GetItem(Gauges[0].Index, Gauges[0].Subindex).Value) / Gauges[0].DisplayFactor;
                    Gauges[1].CurrentValue = Convert.ToDouble(ObjectDictionary.GetItem(Gauges[1].Index, Gauges[1].Subindex).Value) / Gauges[1].DisplayFactor; ;


                    /* Get emergency message values from object dictionary */
                    for (byte i = 1; i <= EmcyMsgs.Count; i++)
                    {
                        DictItem item = ObjectDictionary.GetItem(0x1003, i);

                        temp = (uint)item.Value;

                        EmcyMsgs[i - 1].SequenceNumber = (temp >> 16).ToString();
                        if (((temp) & 0xFF00) == 0x6100 || ((temp) & 0xFF00) == 0x6200 || ((temp) & 0xFF00) == 0x6300)
                        {
                            EmcyMsgs[i - 1].EmcyMsgID = temp;
                            
                            EmcyMsgs[i - 1].messageDict.TryGetValue((temp & 0xFF00), out string value);
                            EmcyMsgs[i - 1].Msg = string.Format("{0} - Page {1}", value, (temp & 0xFF));
                        }
                        else
                        {
                            EmcyMsgs[i - 1].EmcyMsgID = temp;

                            EmcyMsgs[i - 1].messageDict.TryGetValue((temp & 0x0000FFFF), out string value);
                            EmcyMsgs[i - 1].Msg = value;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                timerNonRealTime.Stop();
                MessageBox.Show(err.ToString());
                MessageBox.Show("Measurement timers stopped beacuse of exception.");
            }
        }
        
        /// <summary>
        /// ??
        /// </summary>
        public void GetGaugesInfo()
        {
            //Gauges[0].OptEnd = ( COMM.AsyncReadInt(SlaveNumber, 0x6510, 0x14) ) / Gauges[0].DisplayFactor;
            //Gauges[1].OptEnd = ( COMM.AsyncReadInt(SlaveNumber, 0x6510, 0x03) ) / Gauges[1].DisplayFactor;
            //Gauges[1].OptStart = ( COMM.AsyncReadInt(SlaveNumber, 0x6510, 0x02) ) / Gauges[1].DisplayFactor;
            //Gauges[1].MaxVal = ( COMM.AsyncReadInt(SlaveNumber, 0x6510, 0x07) ) / Gauges[1].DisplayFactor;
        }

        public void SetTargetPosition(int target_position)
        {
            TargetPosition = target_position;
        }

        public void SetTargetVelocity(int target_velocity)
        {
            TargetVelocity = target_velocity;
        }

        public void SetProfileVelocity(uint target_velocity)
        {
            COMM.AsyncWrite(SlaveNumber, 0x6081, 0, target_velocity);
        }

        public void SetProfileAcceleration(uint acceleration)
        {
            COMM.AsyncWrite(SlaveNumber, 0x6083, 0, acceleration);

        }
        
        public void SetProfileDeceleration(uint deceleration)
        {
            COMM.AsyncWrite(SlaveNumber, 0x6084, 0, deceleration);
        }

        public void SetOperModeHM() 
        {
            SetOperMode(OM_HM);
        }

        public void SetOperMode(sbyte OM)
        {
            TargetMode = OM;
        }

        public void SetOperModePPM()
        {
            SetOperMode(OM_PPM);
        }

        public void SetOperModePVM()
        {
            SetOperMode(OM_PVM);
        }

        public void SetOperModeStepper()
        {
            SetOperMode(OM_STP);
        }

        public void SetOperMode0()
        {
            SetOperMode(0); /* Oper Mode -> off */
        }

        public void SetRampType(short ramp_type)
        {
            COMM.AsyncWrite(SlaveNumber, 0x6086, 0, ramp_type);
        }

        public void SdoWrite(ushort index, byte subindex, object value)
        {
            try
            {
                COMM.AsyncWrite(SlaveNumber, index, subindex, value);
            }
            catch(Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }

        public void WriteDictionaryValue(DictItem item)
        {
           SdoWrite(item.Index, item.Subindex, item.Value);
        }

        public object SdoRead(ushort index, byte subindex)
        {
            COMM.AsyncRead(SlaveNumber, index, subindex);

            return ObjectDictionary.GetItem(index, subindex).Value;
        }

        /// <summary>
        /// Sets the ControlWord to the device by using the definded DSP402 commands 
        /// </summary>
        /// <param name="controlword">Base DSP402 command</param>
        /// <param name="extra_bits">Extra bits like Halt Bit, New Set Point, etc.</param>
        public void SetControlWordPdo(ushort controlword, ushort extra_bits)
        {
            CurrentEB = extra_bits;
            CurrentCW = controlword;
            Controlword = (ushort)(controlword | extra_bits);
        }
        public void SetControlWordPdoNoExtraBit(ushort controlword)
        {
            Controlword = (ushort)(controlword | CurrentEB);
        }


    }

 
}