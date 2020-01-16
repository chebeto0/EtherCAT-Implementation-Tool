using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EtherCAT_Master.Core
{

    public class StateMachine : INotifyPropertyChanged
    {
        //
        public const uint NOT_READY_TO_SWITCH_ON = 0x0000;	/* Zustand: Not Ready to Switch On */
        public const uint SWITCH_ON_DISABLED = 0x0040;	/* Zustand: Switch On Disabled */
        public const uint READY_TO_SWITCH_ON = 0x0021;	/* Zustand: Ready to Swicht On */
        public const uint SWITCHED_ON = 0x0023;	/* Zustand: Switched On */
        public const uint OPERATION_ENABLED = 0x0027;	/* Zustand: Operation Enabled */

        public const uint QUICK_STOP_ACTIVE = 0x0007;	/* Zustand: Quick Stop Active */
        public const uint FAULT_REACTION_ACTIVE = 0x000F;	/* Zustand: Fault Reaction Active */
        public const uint FAULT = 0x0008;    /* Zustand: Fault */

        public const uint BIT_10 = 0x0400;    /* Zustand: Mode-specific (Target reached)*/

        private bool _enable_if_no_fault = true;
        public bool EnableIfNoFault
        {
            get { return _enable_if_no_fault; }
            set
            {
                if(value != _enable_if_no_fault)
                {
                    _enable_if_no_fault = value;
                    OnPropertyChanged("EnableIfNoFault");
                }
            }
        }

        public uint ControlWord { get; set; }
        private ushort _state;
        public ushort StateWord
        {
            get { return _state; }
            set
            {
                if (value != _state)
                {
                    _state = value;
                    OperEnabled = false;
                    OnPropertyChanged("StateWord");
                    TargetReached = IS_BIT10_ACTIVE();
                    SetPointAcknowledged = IS_BIT12_ACTIVE();
                    if (IS_OPERATION_ENABLED())
                    {
                        EnableIfNoFault = true;
                        ContentStatus = "Operation Enabled";
                        ContentCmdButton = "Disable";
                        OperEnabled = true;
                    }
                    else if (IS_FAULT())
                    {
                        ContentStatus = "Fault";
                        ContentCmdButton = "Fault Reset";
                        EnableIfNoFault = false;
                       
                    }
                    else
                    {
                        EnableIfNoFault = true;
                        if (IS_NOT_READY_TO_SWITCH_ON())
                        {
                            ContentStatus = "Not Ready to Switch On";
                        }
                        if (IS_SWITCH_ON_DISABLED())
                        {
                            ContentStatus = "Switch On Disabled";
                        }
                        if (IS_READY_TO_SWITCH_ON())
                        {
                            ContentStatus = "Ready to Switch On";
                        }
                        if (IS_SWITCHED_ON())
                        {
                            ContentStatus = "Switched On";
                        }
                        if (IS_QUICK_STOP_ACTIVE())
                        {
                            ContentStatus = "Quick Stop Active";
                        }
                        if (IS_FAULT_REACTION_ACTIVE())
                        {
                            ContentStatus = "Fault Reaction Active";
                        }
                        ContentCmdButton = "Enable";
                       
                    }
                }
            }
        }
        public bool _oper_enabled;
        public bool OperEnabled
        {
            get { return _oper_enabled; }
            set
            {
                _oper_enabled = value;
                OnPropertyChanged("OperEnabled");
            }
        }
        public string _content_cmd_button;
        public string ContentCmdButton
        {
            get { return _content_cmd_button; }
            set
            {
                _content_cmd_button = value;
                OnPropertyChanged("ContentCmdButton");
            }
        }
        public string _content_status;
        public string ContentStatus
        {
            get { return _content_status; }
            set
            {
                _content_status = value;
                OnPropertyChanged("ContentStatus");
            }
        }
        
        public bool TargetReached { get; set; }
        public bool SetPointAcknowledged { get; set; }

        public static readonly ushort SM_CW_SHUTDOWN     = 0x06;
        public static readonly ushort SM_CW_SWITCH_ON    = 0x07;
        public static readonly ushort SM_CW_DISABLE_VOLT = 0x00;
        public static readonly ushort SM_CW_QUICK_STOP   = 0x02;
        public static readonly ushort SM_CW_DISABLE_OP   = 0x07;
        public static readonly ushort SM_CW_ENABLE_OP    = 0x0F;
        public static readonly ushort SM_CW_FAULT_RESET  = 0x80;

        public StateMachine( )
        {
            StateWord = 0;
            ControlWord = 0x0080;

            //SM_CW_SHUTDOWN = 0x06;
            //SM_CW_SWITCH_ON = 0x07;
            //SM_CW_DISABLE_VOLT = 0x00;
            //SM_CW_QUICK_STOP = 0x02;
            //SM_CW_DISABLE_OP = 0x07;
            //SM_CW_ENABLE_OP = 0x0F;
            //SM_CW_FAULT_RESET = 0x80;
        }

        public bool IS_NOT_READY_TO_SWITCH_ON() { return (((_state) & 0x004F) == NOT_READY_TO_SWITCH_ON); }
        public bool IS_SWITCH_ON_DISABLED()     { return (((_state) & 0x004F) == SWITCH_ON_DISABLED); }
        public bool IS_READY_TO_SWITCH_ON()     { return (((_state) & 0x006F) == READY_TO_SWITCH_ON); }
        public bool IS_SWITCHED_ON()            { return (((_state) & 0x006F) == SWITCHED_ON); }
        public bool IS_OPERATION_ENABLED()      { return (((_state) & 0x006F) == OPERATION_ENABLED); }
        public bool IS_QUICK_STOP_ACTIVE()      { return (((_state) & 0x006F) == QUICK_STOP_ACTIVE); }
        public bool IS_FAULT_REACTION_ACTIVE()  { return (((_state) & 0x004F) == FAULT_REACTION_ACTIVE); }
        public bool IS_FAULT()                  { return (((_state) & 0x004F) == FAULT);  }
        public bool IS_BIT10_ACTIVE()           { return (((_state) & 0x0400) == 0x0400); }
        public bool IS_BIT12_ACTIVE()           { return (((_state) & 0x1000) == 0x1000); }
        public bool IS_BIT13_ACTIVE()           { return (((_state) & 0x2000) == 0x2000); }
        public bool IS_WARNING_BIT_ACTIVE()     { return (((_state) & 0x0080) == 0x0080); }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    
    
}
