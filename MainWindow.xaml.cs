using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Timers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Configuration;
using System.Windows.Markup;
using System.Globalization;
using MahApps.Metro.IconPacks;
using OxyPlot.Series;
using System.Windows.Data;
using System.Threading;
using System.Runtime.InteropServices;
using EtherCAT_Master.Core.Controls;
using EtherCAT_Master.Core.Dictionary;
using EtherCAT_Master.Core;
using EtherCAT_Master.Core.Communication;

/* 
 * TODO 07-01-2019 
 * -Make the dictionary for the emergency messages from the config file.
 * 
 * TODO 26-03-2019
 * ???-Make instance of object dictionary in each device. ?Maybe? Do we still need this? Probably yes if two processes.
 * for different devices are working in the background.
 * 
 * TODO 12-04-2019
 * -In communication SOEM make it so that the SCOPE is not on the same thread as the PDO.
 * -Make it so that the EoE write protect of the UI only protects write of PDO or something like that.
 */

namespace EtherCAT_Master
{

    public partial class MainWindow : MetroWindow
    {
        public System.Timers.Timer MyTimer1 = new System.Timers.Timer(); // timer for triggering every 500ms
        private readonly System.Timers.Timer _timerUpdatePlots = new System.Timers.Timer();

        /* Variables for the selection of device */
        private const int HALT_BIT = 0x0100;

        /* Path to the program executable */
        public readonly string exePath; 

        private ModeSpecificBits _modeSpecBits = new ModeSpecificBits();
        private readonly DriveSwitch DriveSwitch = new DriveSwitch();
        public DictionaryBuilder ObjectDictionary;

        private readonly ScopeUserControl _scopeUserControl = new ScopeUserControl();

        private UserControl CommControl;

        public int AdapterNumber = 0;

        public int SelectedDevice { get; set; }

        //private ModeSpecificBits test_modspec = new ModeSpecificBits();
        private int slide_size_ticks_vel;
        private int slide_size_ticks_acc;
        private int slide_size_ticks_dec;
        private int slide_size_ticks_pos;
        private int slider_max_vel;
        private int slider_max_acc;
        private int slider_max_dec;
        private int slider_max_pos;
        private double ds_time_minimum;
        private double ds_time_interval;

        public ErrorNotificationsViewModel ErrorNoti = new ErrorNotificationsViewModel();

        private ObservableCollection<HambMenuItemDict> HambMenuDict = new ObservableCollection<HambMenuItemDict>();

        private static RoutedCommand CommandEnable = new RoutedCommand(); /* Command for the drive switch to action the drive State Machine */
        private static RoutedCommand CommandFindCtrlF = new RoutedCommand();
        private static RoutedCommand CommandF5 = new RoutedCommand();
        private static RoutedCommand CommandF2 = new RoutedCommand();
        private static RoutedCommand CommandDel = new RoutedCommand();
        private static RoutedCommand CommandChangeDecHexCoeDict = new RoutedCommand();

        private ObservableCollection<CtrlDgItem> obj_dg_ds = new ObservableCollection<CtrlDgItem>();
        private ObservableCollection<CtrlDgItem> obj_dg_pvm = new ObservableCollection<CtrlDgItem>();

        private HomingMethods homing;

        public CommunicationBase Communication;

        public MainWindow()
        {
            /* Set at the beginning Selected device to -1 so no device is selected*/
            SelectedDevice = -1;

            /* Create dummy instance of communication because the application needs it */
            Communication = new CommunicationDummy();

            /* Get path to the executable of the application*/
            exePath = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);

            InitializeComponent();
            
        }

        /// <summary>
        /// Event for when the Main Window has been loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DictionaryStartUp();
            SetUpUiElements();
            LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }

        /// <summary>
        /// Event for when the content of the Main Window has been rendered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            MyTimer1.Elapsed += new ElapsedEventHandler(DoTimeEvent1); /* Timer for update of some UI things like the "LEDs" in the General Tab */
            MyTimer1.Interval = 200; /* in milliseconds */
            
            _timerUpdatePlots.Elapsed += new ElapsedEventHandler(UpdatePlotsTimeEvent); /* Timer for update of plots */
            _timerUpdatePlots.Interval = 487; /* in milliseconds */
        }

        #region Initialization of UI things
        
        /// <summary>
        /// Set Up of UI Properties and Data Context from MainWindow
        /// </summary>
        private void SetUpUiElements()
        {
            _scopeUserControl.hamburger1.ItemClick += OnMenuItemClick1;
            ContentScope.DataContext = _scopeUserControl;

            /* Open the config file to get the config data*/
            var configManager = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var confCollection = configManager.AppSettings.Settings;

            configManager.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configManager.AppSettings.SectionInformation.Name);

            /* Get the config data for the controls (like the sliders) for velocity, acceleration, position, etc.*/
            ds_time_minimum = Convert.ToDouble(confCollection["drice_sequence_time_minimum"].Value);
            ds_time_interval = Convert.ToDouble(confCollection["drice_sequence_time_interval"].Value);
            slider_max_vel = Convert.ToInt32(confCollection["slider_max_vel"].Value);
            slide_size_ticks_vel = Convert.ToInt32(confCollection["slider_step_vel"].Value);
            slider_max_acc = Convert.ToInt32(confCollection["slider_max_acc"].Value);
            slide_size_ticks_acc = Convert.ToInt32(confCollection["slider_step_acc"].Value);
            slider_max_dec = Convert.ToInt32(confCollection["slider_max_dec"].Value);
            slide_size_ticks_dec = Convert.ToInt32(confCollection["slider_step_dec"].Value);
            slider_max_pos = Convert.ToInt32(confCollection["slider_max_pos"].Value);
            slide_size_ticks_pos = Convert.ToInt32(confCollection["slider_step_pos"].Value);

            WindowState = WindowState.Maximized;/* Maximize Main Window */

            /* Get image*/
            var uri = new Uri(exePath + @"\Images\20171122_Logo_NodeMaster_EC.png");
            var bitmapImage = new BitmapImage(uri);
            //INTECLogo.Source = bitmapImage;

            /* Get the "Intec" Icon */
            uri = new Uri(exePath + @"\Images\intec_icon.ico"); 
            bitmapImage = new BitmapImage(uri);
            Icon = bitmapImage;
            
            /////////////////////////////////////////////////////////////////////////////
            ///////////////////////////// DRIVE SEQUENCE PPM ////////////////////////////
            /////////////////////////////////////////////////////////////////////////////
            HaltContinueDS.Content = new PackIconMaterial() { Kind = PackIconMaterialKind.Pause };
            HaltContinueDS.Foreground = new SolidColorBrush(IntecColors.dark_grey);
            driveSequence.ItemsSource = null;
            obj_dg_ds.Add(new CtrlDgItem() /* Add first element to the drive sequence DataGrid by default */
            {
                Id = 0,
                AddRemoveText = new PackIconMaterial() { Kind = PackIconMaterialKind.Minus },
                ButtonColor = new SolidColorBrush(IntecColors.light_red),
                TargetPosition = 0,
                Velocity = 500,
                Acceleration = slider_max_acc / 2,
                Deceleration = slider_max_dec / 2,
                TimeWait = 1000,
                TimeMinimum = ds_time_minimum,
                TimeInterval = ds_time_interval,
                TickPos = slide_size_ticks_pos,
                TickVel = slide_size_ticks_vel,
                TickAcc = slide_size_ticks_acc,
                TickDec = slide_size_ticks_dec,
                PB = new ProgressBar(),
                MaxPosSlide = slider_max_pos,
                MinPosSlide = -slider_max_pos,
                MaxVelSlide = slider_max_vel,
                MinVelSlide = 1,
                MaxAccSlide = slider_max_acc,
                MinAccSlide = 1,
                MaxDecSlide = slider_max_dec,
                MinDecSlide = 1,
                RampType = 0,
                Vis = Visibility.Visible
            });
            obj_dg_ds.Add(new CtrlDgItem() /* Add the "last" element of the drive sequence DataGrid that will only include a button to add more elements to the sequence */
            {
                Id = 1,
                AddRemoveText = new PackIconMaterial() { Kind = PackIconMaterialKind.Plus },
                ButtonColor = new SolidColorBrush(IntecColors.green),
                PB = new ProgressBar(),
                Vis = Visibility.Hidden
            });
            driveSequence.ItemsSource = obj_dg_ds;

            /////////////////////////////////////////////////////////////////////////////
            ///////////////////////////// PROFILE VELOCITY MODE /////////////////////////
            /////////////////////////////////////////////////////////////////////////////
            obj_dg_pvm.Add(new CtrlDgItem()
            {
                Id = 0,
                Velocity = 0,
                Acceleration = slider_max_acc / 2,
                Deceleration = slider_max_dec / 2,
                PB = new ProgressBar(),
                TickVel = slide_size_ticks_vel,
                TickAcc = slide_size_ticks_acc,
                TickDec = slide_size_ticks_dec,
                MaxVelSlide = slider_max_vel,
                MinVelSlide = -slider_max_vel,
                MaxAccSlide = slider_max_acc,
                MaxDecSlide = slider_max_dec,
                RampType = 0,
                Vis = Visibility.Visible
            });
            dataGridPVM.ItemsSource = obj_dg_pvm;
            foreach (var obj in obj_dg_pvm)
            {
                obj.PropertyChanged += VelocityPropertyChangedHandler;
            }

            //////////////////////////////////////////// 
            ////////////////////////////////////////////
            /* Initialize some colors for the UI elements */
            MWindow.Background = new SolidColorBrush(IntecColors.bg_grey);
            statusBit0.Fill = new SolidColorBrush(IntecColors.light_grey);
            statusBit1.Fill = new SolidColorBrush(IntecColors.light_grey);
            statusBit2.Fill = new SolidColorBrush(IntecColors.light_grey);
            statusBit3.Fill = new SolidColorBrush(IntecColors.light_grey);
            statusBit4.Fill = new SolidColorBrush(IntecColors.light_grey);
            statusBit5.Fill = new SolidColorBrush(IntecColors.light_grey);
            statusBit6.Fill = new SolidColorBrush(IntecColors.light_grey);
            statusBit7.Fill = new SolidColorBrush(IntecColors.light_grey);
            
            _modeSpecBits.statusBit10.Fill = new SolidColorBrush(IntecColors.light_grey);
            _modeSpecBits.statusBit12.Fill = new SolidColorBrush(IntecColors.light_grey);
            _modeSpecBits.statusBit13.Fill = new SolidColorBrush(IntecColors.light_grey);
            _modeSpecBits.warningBit.Fill = new SolidColorBrush(IntecColors.light_grey);
            
            /* Text for the Headers of the DataGrid of the Profile Velocity Mode */
            dataGridPVM.Columns[0].Header = "Velocity\n[min\x207B\xB9]";
            dataGridPVM.Columns[1].Header = "Acceleration\n[1/s\xB2]";
            dataGridPVM.Columns[2].Header = "Deceleration\n[1/s\xB2]";
            
            /* Text for the Headers of the DataGrid of the Profile Position Mode */
            driveSequence.Columns[1].Header = "Velocity\n[min\x207B\xB9]";
            driveSequence.Columns[2].Header = "Acceleration\n[1/s\xB2]";
            driveSequence.Columns[3].Header = "Deceleration\n[1/s\xB2]";

            /* Set the data context of the Mode-Specific-Bit Control to respective object */
            DriveSwitch.ContentGeneralModeSpecBits.DataContext = _modeSpecBits;
            /////////////////////////////////////////////////////////////////////////////
            ///////////////////////////// ErrorNoti.Items //////////////////////////////
            /////////////////////////////////////////////////////////////////////////////

            ErrorNoti.Items = new ObservableCollection<ErrorNotifications>();

            dataGridNotifications.DataContext = ErrorNoti.Items;

            /////////////////////////////////////////////////////////////////////////////
            //////////////////////////////// Drive Switch ///////////////////////////////
            /////////////////////////////////////////////////////////////////////////////
            ContentSwitchDS.DataContext = DriveSwitch;
            DriveSwitch.IsEnabled = false;
            

            /////////////////// Command declaration for Drive Switch 
            var cb = new CommandBinding(CommandEnable,
                CommandExecute, MyCommandCanExecute);
            this.CommandBindings.Add(cb);
            DriveSwitch.CmdSM.Command = CommandEnable;
            var kg = new KeyGesture(Key.M, ModifierKeys.Control);
            var ib = new InputBinding(CommandEnable, kg);
            this.InputBindings.Add(ib);

            /////////////////////////////////////////////////////////////////////////////
            ///////////////////////////// Get ADAPTERS //////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////

            AdapterNumber = Convert.ToInt32(confCollection["number_network_adapter"].Value);
            ScanAdapters();
            CmbxNetworkAdapter.SelectedIndex = AdapterNumber;

            //////////////////////////////////////////////////////////////////////////////
            ///////////////////////////// CoE Dictionary /////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////
            dataGridDictionary.DataContext = ObjectDictionary.DictViewModel;

            hamburgerDict.HamburgerButtonClick += HamburgerDict_HamburgerButtonClick;

            HambMenuDict = (new HambMenuItemDict()).GetItems();
            HambMenuDict[2].TxtBox.TextChanged += TxtBox_TextChanged;
            hamburgerDict.ItemsSource = HambMenuDict;

            CommandChangeDecHexCoeDict.InputGestures.Add(new KeyGesture(Key.F6)); /* Declare command for Change dec/hex hotkey */
            CommandBindings.Add(new CommandBinding(CommandChangeDecHexCoeDict, CommandChangeDecHexCoeDictExcecuted));

            /////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////// Declare Commands //////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////

            CommandFindCtrlF.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control)); /* Declare command for find hotkey */
            CommandBindings.Add(new CommandBinding(CommandFindCtrlF, CommandFindCtrlFExecuted));

            CommandF5.InputGestures.Add(new KeyGesture(Key.F5)); /* Declare command for Refresh hotkey */
            CommandBindings.Add(new CommandBinding(CommandF5, CommandF5Executed));

            CommandDel.InputGestures.Add(new KeyGesture(Key.Delete)); /* Declare command for Refresh hotkey */
            CommandBindings.Add(new CommandBinding(CommandF5, CommandDelExecuted));

            /////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////// Homing Methods ////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////

            homing = new HomingMethods();

            ComboBoxHoming.ItemsSource = homing.methodDict;
            
            /////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////
            /////////////////////////////////////////////////////////////////////////////////
            RadButEcat.IsChecked = true;
            ChooseComm.DataContext = Communication;

            /*  */
            buttonSplitScope.Content = new PackIconMaterial() { Kind = PackIconMaterialKind.ChartLine };
        }

        //private void MakeNewScopeHambMenus()
        //{
        //}

        /// <summary>
        /// Event whene the EtherCAT Radial Button was Checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadButEcat_Checked(object sender, RoutedEventArgs e)
        {
            CommControl = new EtherCatControl(this);
            CommContentControl.DataContext = CommControl;
            (CommControl as EtherCatControl).dataGridDevices.SelectedCellsChanged += dataGridDevices_SelectedCellsChanged;
            CommContentControl.IsEnabled = true;
        }

        /// <summary>
        /// Event whene the UDP/IP Radial Button was Checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadButUdp_Checked(object sender, RoutedEventArgs e)
        {
            CommControl = new UdpCommControl(this);
            CommContentControl.DataContext = CommControl;
            (CommControl as UdpCommControl).dataGridDevices.SelectedCellsChanged += dataGridDevices_SelectedCellsChanged;
            CommContentControl.IsEnabled = true;
        }
        
        /// <summary>
        /// Command to execute when the Hotkey Ctrl+F is pressed. On Tab Scope it will focus on the first Combobox. On the Dictionary Tab it will focus on the search TextBox.
        /// </summary>
        private void CommandFindCtrlFExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (Tabs.SelectedIndex == 4) /* If tab CoE Dictionary is open */
                {
                    hamburgerDict.IsPaneOpen = true;
                    dataGridDictionary.Margin = new Thickness(240, 0, 0, 0);

                    HambMenuDict[2].TxtBox.Text = "";

                    HambMenuDict[2].TxtBox.Focusable = true;
                    Keyboard.Focus(HambMenuDict[2].TxtBox);
                }
                else if (Tabs.SelectedIndex == 3) /* If tab Scope is open */
                {
                    if (!(Communication.Devices[SelectedDevice] is PCS device))
                    {
                        throw new Exception("No device of type PCS connected and/or selected");
                    }
                    _scopeUserControl.hamburger1.IsPaneOpen = true;
                    _scopeUserControl.scope1.Margin = new Thickness(240, 0, 0, 0);
                    device.scopeControl.Hamb1.Items[2].Combo.Focusable = true;
                    Keyboard.Focus(device.scopeControl.Hamb1.Items[2].Combo);
                }
            
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }

        }

        /// <summary>
        /// Command to execute when the Hotkey F5 is pressed.
        /// </summary>
        private void CommandF5Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (Tabs.SelectedIndex == 4)
                {
                    RefreshCoeValues();
                }
                else if (Tabs.SelectedIndex == 3)
                {
                    StartStopPlottingAction();
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }

        private void CommandDelExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("Del");
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }

        /// <summary>
        /// Declaration of external SOEM Function to read the names of the Network adapters
        /// </summary>
        /// <param name="buf"></param>
        [DllImport("Resources\\soem.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void GetNetworkAdapter(byte[] buf);
        public void EcGetNetworkAdapter(byte[] buf)
        {
            if (!Communication.Connected)
            {
                GetNetworkAdapter(buf);
            }
                
        }
        
        /// <summary>
        /// Scan for Network Interface Controllers on the system and return them to the Combobox "CmbxNetWorkAdapter" so one can be selected.
        /// </summary>
        private void ScanAdapters()
        {
            try
            {
                byte[] buf = new byte[300];

                EcGetNetworkAdapter(buf);

                var ret = Encoding.ASCII.GetString(buf);

                var retSplitString = ret.Split('*');
                var retTokens = new List<string>();
                for (var i = 0; i < retSplitString.Length - 1; i++)
                {
                    retTokens.Add(retSplitString[i]);
                }
                CmbxNetworkAdapter.ItemsSource = retTokens;
                
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }

        /// <summary>
        /// Button Click event for calling the function to scan for Network Interface Controllers.
        /// </summary>
        private void ScanAdapters_Click(object sender, RoutedEventArgs e)
        {
            ScanAdapters();
        }

        /// <summary>
        /// Event triggered when the selection of a NIC is done. The function saves the index of the adapter for soem and it saves this index into the config file for the next time.
        /// </summary>
        private void CmbxNetworkAdapter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                AdapterNumber = (sender as ComboBox).SelectedIndex;

                var configManager = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var confCollection = configManager.AppSettings.Settings;

                configManager.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configManager.AppSettings.SectionInformation.Name);

                confCollection["number_network_adapter"].Value = AdapterNumber.ToString();
                configManager.Save(ConfigurationSaveMode.Modified);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }

        public ObservableCollection<string> scanned_devices = new ObservableCollection<string>();


        /// <summary>
        /// This is a function triggered for the selected cells event from the Devices DatGrid.
        /// It is mainly used to set the ItemsSource and DataContext of the View elements
        /// to the PCS device object that is being selected, so that data bindings will work.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridDevices_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            try
            {
                /* check if selected device is of type PCS */
                if (!((sender as DataGrid).SelectedItem is PCS device))
                {
                    return;
                }

                foreach (PCS dev in Communication.Devices)
                {
                    if (dev is PCS)
                    {
                        dev.timerNonRealTime.Stop();
                    }
                }

                SelectedDevice = (sender as DataGrid).SelectedIndex;
                _scopeUserControl.scope1.DataContext = device.scopeControl.plotScope;

                dataGridEmcyMsgs.ItemsSource = device.EmcyMsgs;
                DriveSwitch.CmdSM.DataContext = device.stateMachineDsp402;
                DriveSwitch.statusLabel.DataContext = device.stateMachineDsp402;
                DriveSwitch.IsEnabled = true;
                _modeSpecBits.DataContext = device.OmBits;
                HaltContinuePVM.DataContext = device;
                ppmGrid.DataContext = device.stateMachineDsp402;
                pvmGrid.DataContext = device.stateMachineDsp402;
                hmGrid.DataContext  = device.stateMachineDsp402;

                _scopeUserControl.hamburger1.DataContext = device.scopeControl.Hamb1;
                _scopeUserControl.hamburger1.HamburgerButtonClick += Hamburger1_HamburgerButtonClick;

                device.TargetVelocity = 0;
                obj_dg_pvm[0].Velocity = 0;

                myGauge0.DataContext = device.Gauges[0];
                myGauge1.DataContext = device.Gauges[1];

                if (Tabs.SelectedIndex == 5)
                {
                    if (Communication.Connected)
                    {
                        device.timerNonRealTime.Interval = 100;
                        device.timerNonRealTime.Start();
                    }
                }

                myGauge0.InvalidateArrange();
                myGauge1.InvalidateArrange();
                myGauge0.InvalidateVisual();
                myGauge1.InvalidateVisual();
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }

        /// <summary>
        /// Can execute command of drive switch
        /// </summary>
        private static void MyCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        /// <summary>
        /// Execute command when drive switch is clicked.
        /// </summary>
        private async void CommandExecute(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (!(Communication.Devices[SelectedDevice] is PCS device))
                {
                    MessageBox.Show("Selected device is not of type PCS.");
                    return;
                }
                    
                if (device.EcStateMachine == EC_SM.EC_STATE_OPER 
                    || device.EcStateMachine == EC_SM.EC_STATE_SAFE_OP 
                    || device.EcStateMachine == EC_SM.EC_STATE_PRE_OP
                    || Communication.commType == CommType.COMM_UDP)
                {
                    if (device.stateMachineDsp402.IS_FAULT() 
                        || device.stateMachineDsp402.IS_FAULT_REACTION_ACTIVE())
                    {
                        await Task.Run(() =>
                        {
                            device.SetControlWordPdo(StateMachine.SM_CW_FAULT_RESET, 0x0100);
                        });

                        await Task.Delay(100);

                        await Task.Run(() =>
                        {
                            device.SetControlWordPdo(StateMachine.SM_CW_DISABLE_VOLT, 0x0100);
                        });

                        if (device.EcStateMachine == EC_SM.EC_STATE_PRE_OP)
                        {
                            await Task.Delay(100);
                            //device.StateMach.StateWord = (ushort)
                                //device.SdoRead(0x6041, 0x00);
                        }

                        Dispatcher.Invoke(() =>
                        {
                            TabPPM.IsEnabled = true;
                            TabPVM.IsEnabled = true;
                            TabHM.IsEnabled = true;
                            TabSCOPE.IsEnabled = true;
                        });
                    }
                    else if (device.stateMachineDsp402.IS_OPERATION_ENABLED())
                    {
                        Console.WriteLine("Is Operation Enabled");
                        await Task.Run(() =>
                        {
                            StopDriveSequence();
                            device.SetOperMode0();
                            device.SetControlWordPdo(StateMachine.SM_CW_SHUTDOWN, 0x0100);
                        });
                    }
                    else if (!device.stateMachineDsp402.IS_OPERATION_ENABLED())
                    {
                        Console.WriteLine("NOT Operation Enabled");
                        device.SetControlWordPdo(StateMachine.SM_CW_SHUTDOWN, 0x0100);
                        await Task.Delay(150);
                        device.SetControlWordPdo(StateMachine.SM_CW_ENABLE_OP, 0x0100); 
                        
                        switch(Tabs.SelectedIndex)
                        {
                            case 0:
                                device.SetOperModePPM();
                                break;
                            case 1:
                                device.SetOperModePVM();
                                break;
                            case 2:
                                device.SetOperModeHM();
                                break;
                            default:
                                device.SetOperMode0();
                                break;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
        }

        /// <summary>
        /// Event for when the 
        /// </summary>
        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl && Communication.Connected && SelectedDevice > -1)
            {
                foreach (PCS dev in Communication.Devices)
                {
                    if (dev is PCS)
                    {
                        dev.timerNonRealTime.Stop();
                    }
                }

                var device = Communication.Devices[SelectedDevice] as PCS;
                
                var tab = sender as TabControl;

                if (tab.SelectedIndex == 0)
                {
                    Dispatcher.Invoke(() => { StartDS.Content = "Start"; });
                }
                else if (tab.SelectedIndex == 1)
                {
                }
                else if (tab.SelectedIndex == 2)
                {
                }
                else if (tab.SelectedIndex == 3) /* Scope Tab */
                {
                    RemoveScopeToSplitWindow();
                }
                else if (tab.SelectedIndex == 4) /* Dict Tab */
                {
                }
                else if (tab.SelectedIndex == 5)
                {
                    if (Communication.Connected)
                    {
                        device.timerNonRealTime.Interval = 100;
                        device.timerNonRealTime.Start();
                    }

                    myGauge0.DataContext = device.Gauges[0];
                    myGauge1.DataContext = device.Gauges[1];
                }
            }
        }

        /// <summary>
        /// Disconnect the communication
        /// </summary>
        public void Disconnect() 
        {
            MyTimer1.Stop();
            StopPlots1();
            Communication.Disconnect();
        }

        /// <summary>
        /// The disconnect function that is called when the application is being shut down
        /// </summary>
        public async void Disconnect_Shutdown()
        {
            try
            {
                MyTimer1.Stop();
                StopPlots1();

                await Task.Run(() =>
                {
                    foreach (PCS device in Communication.Devices)
                    {
                        if (device is PCS)
                        {
                            device.SetControlWordPdo(StateMachine.SM_CW_DISABLE_VOLT, 0);
                            
                            StopDriveSequence();

                            foreach (PCS dev in Communication.Devices)
                            {
                                if (dev is PCS)
                                {
                                    dev.timerNonRealTime.Stop();
                                }
                            }
                            
                            if (device.scopeControl.ts_scope != null)
                            {
                                device.scopeControl.ts_scope.Cancel();
                            }
                        }
                    }
                });

                await Task.Delay(400);

                Communication.Disconnect();

                //await Task.Delay(400);

                //ObjectDictionary.Dispose();
                //DictionaryStartUp();
                //dataGridDictionary.DataContext = ObjectDictionary.DictViewModel;

            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
        }

        /// <summary>
        /// Calls the construction of the main dictionary and created the instance of the Object Dictionary
        /// </summary>
        private void DictionaryStartUp()
        {
            /* Construct the dictionary from the path to the ESI file. */
            ObjectDictionary = new DictionaryBuilder(Path.Combine(exePath, @"ESI\INTEC_PCS.xml"));
        }

        #endregion

        #region Time Event
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //!!!!!!!!!!!! TIME EVENT !!!!!!!!!!!!!!!!!!! TIME EVENT !!!!!!!!!!!!!!!!!!!!!!! TIME EVENT !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        private int last_velocity;
        private uint last_acceleration;
        private uint last_deceleration;
        /// <summary>
        /// Timed event to cyclicaly update information from the device to be displayed.
        /// It will also write the demand Velocity, Acceleration and Deceleration for the Profile Velocity mode is they have changed.
        /// </summary>
        public void DoTimeEvent1(object source, ElapsedEventArgs e)
        {
            GetStatusWord();
            GetManufacturerStatus();
        }

        /// <summary>
        /// Color the different lightings of the UI depending on the status word.
        /// </summary>
        private void GetStatusWord()
        {
            try
            {
                if (Communication.Connected && SelectedDevice>-1)
                {
                    var device = Communication.Devices[SelectedDevice] as PCS;

                    ////### IS_NOT_READY_TO_SWITCH_ON ###//
                    if ( device.stateMachineDsp402.IS_NOT_READY_TO_SWITCH_ON() )
                        this.Dispatcher.Invoke(() => { statusBit0.Fill = new SolidColorBrush(IntecColors.dark_red); });
                    else
                        this.Dispatcher.Invoke(() => { statusBit0.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### IS_SWITCH_ON_DISABLED ###//
                    if ( device.stateMachineDsp402.IS_SWITCH_ON_DISABLED() )
                        this.Dispatcher.Invoke(() => { statusBit1.Fill = new SolidColorBrush(IntecColors.orange); });
                    else
                        this.Dispatcher.Invoke(() => { statusBit1.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### IS_READY_TO_SWITCH_ON ###//
                    if ( device.stateMachineDsp402.IS_READY_TO_SWITCH_ON() )
                        this.Dispatcher.Invoke(() => { statusBit2.Fill = new SolidColorBrush(IntecColors.yellow); });
                    else
                        this.Dispatcher.Invoke(() => { statusBit2.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### IS_SWITCHED_ON ###//
                    if ( device.stateMachineDsp402.IS_SWITCHED_ON() )
                        this.Dispatcher.Invoke(() => { statusBit3.Fill = new SolidColorBrush(IntecColors.yellow); });
                    else
                        this.Dispatcher.Invoke(() => { statusBit3.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### IS_OPERATION_ENABLED ###//
                    if ( device.stateMachineDsp402.IS_OPERATION_ENABLED() )
                        this.Dispatcher.Invoke(() => { statusBit4.Fill = new SolidColorBrush(IntecColors.green); });
                    else
                        this.Dispatcher.Invoke(() => { statusBit4.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### IS_QUICK_STOP_ACTIVE ###//
                    if ( device.stateMachineDsp402.IS_QUICK_STOP_ACTIVE() )
                        this.Dispatcher.Invoke(() => { statusBit5.Fill = new SolidColorBrush(IntecColors.dark_red); });
                    else
                        this.Dispatcher.Invoke(() => { statusBit5.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### IS_FAULT_REACTION_ACTIVE ###//
                    if ( device.stateMachineDsp402.IS_FAULT_REACTION_ACTIVE() )
                        this.Dispatcher.Invoke(() => { statusBit6.Fill = new SolidColorBrush(IntecColors.dark_red); });
                    else
                        this.Dispatcher.Invoke(() => { statusBit6.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### IS_FAULT ###//
                    if (device.stateMachineDsp402.IS_FAULT() )
                        this.Dispatcher.Invoke(() => { statusBit7.Fill = new SolidColorBrush(IntecColors.dark_red); });
                    else
                        this.Dispatcher.Invoke(() => { statusBit7.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### IS_BIT10_ACTIVE ###//
                    if (device.stateMachineDsp402.IS_BIT10_ACTIVE() )
                        this.Dispatcher.Invoke(() => { _modeSpecBits.statusBit10.Fill = new SolidColorBrush(IntecColors.green); });
                    else
                        this.Dispatcher.Invoke(() => { _modeSpecBits.statusBit10.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### IS_BIT12_ACTIVE ###//
                    if (device.stateMachineDsp402.IS_BIT12_ACTIVE() )
                        this.Dispatcher.Invoke(() => { _modeSpecBits.statusBit12.Fill = new SolidColorBrush(IntecColors.green); });
                    else
                        this.Dispatcher.Invoke(() => { _modeSpecBits.statusBit12.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### IS_BIT13_ACTIVE ###//
                    if (device.stateMachineDsp402.IS_BIT13_ACTIVE() )
                        this.Dispatcher.Invoke(() => { _modeSpecBits.statusBit13.Fill = new SolidColorBrush(IntecColors.orange); });
                    else
                        this.Dispatcher.Invoke(() => { _modeSpecBits.statusBit13.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### IS_WARNING_ACTIVE ###//
                    if (device.stateMachineDsp402.IS_WARNING_BIT_ACTIVE() )
                        this.Dispatcher.Invoke(() => { _modeSpecBits.warningBit.Fill = new SolidColorBrush(IntecColors.yellow); });
                    else
                        this.Dispatcher.Invoke(() => { _modeSpecBits.warningBit.Fill = new SolidColorBrush(IntecColors.light_grey); });
                }
            }
            catch (Exception err)
            {
                MyTimer1.Stop();//////////////////////////
                Console.WriteLine(err.ToString());
            }
        }

        private void GetManufacturerStatus()
        {
            try
            {
                if (Communication.Connected && SelectedDevice>-1)
                {
                    var device = Communication.Devices[SelectedDevice] as PCS;

                    ////### Antrieb refezenziert ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 0)) == (0x00000001 << 0) )
                        Dispatcher.Invoke(() => { manuStatusBit0.Fill = new SolidColorBrush(IntecColors.green); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit0.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### Blockfaht Ereignis ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 1)) == (0x00000001 << 1) )
                        Dispatcher.Invoke(() => { manuStatusBit1.Fill = new SolidColorBrush(IntecColors.green); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit1.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### nterspannung Treiber ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 2)) == (0x00000001 << 2) )
                        Dispatcher.Invoke(() => { manuStatusBit2.Fill = new SolidColorBrush(IntecColors.green); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit2.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //###  Status Bremse; 1=closed 0=open ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 3)) == (0x00000001 << 3) )
                        Dispatcher.Invoke(() => { manuStatusBit3.Fill = new SolidColorBrush(IntecColors.green); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit3.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### IN3 ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 7)) == (0x00000001 << 7) )
                        Dispatcher.Invoke(() => { manuStatusBit7.Fill = new SolidColorBrush(IntecColors.green); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit7.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### IN2 ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 8)) == (0x00000001 << 8) )
                        Dispatcher.Invoke(() => { manuStatusBit8.Fill = new SolidColorBrush(IntecColors.green); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit8.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### IN1 ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 9)) == (0x00000001 << 9) )
                        Dispatcher.Invoke(() => { manuStatusBit9.Fill = new SolidColorBrush(IntecColors.green); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit9.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### STOB ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 10)) == (0x00000001 << 10) )
                        Dispatcher.Invoke(() => { manuStatusBit10.Fill = new SolidColorBrush(IntecColors.green); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit10.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### STOA ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 11)) == (0x00000001 << 11) )
                        Dispatcher.Invoke(() => { manuStatusBit11.Fill = new SolidColorBrush(IntecColors.green); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit11.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### Uebertemperatur ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 15)) == (0x00000001 << 15) )
                        Dispatcher.Invoke(() => { manuStatusBit15.Fill = new SolidColorBrush(IntecColors.yellow); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit15.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### Ueber-/Unterspannung Logik ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 16)) == (0x00000001 << 16) )
                        Dispatcher.Invoke(() => { manuStatusBit16.Fill = new SolidColorBrush(IntecColors.yellow); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit16.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### Ueber-/Unterspannung Zwischenkreis ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 17)) == (0x00000001 << 17) )
                        Dispatcher.Invoke(() => { manuStatusBit17.Fill = new SolidColorBrush(IntecColors.yellow); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit17.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### Uebertemperatur ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 21)) == (0x00000001 << 21) )
                        Dispatcher.Invoke(() => { manuStatusBit21.Fill = new SolidColorBrush(IntecColors.dark_red); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit21.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### Unterspannung Logik  ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 22)) == (0x00000001 << 22) )
                        Dispatcher.Invoke(() => { manuStatusBit22.Fill = new SolidColorBrush(IntecColors.dark_red); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit22.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### Ueberspannung Logik ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 23)) == (0x00000001 << 23) )
                        Dispatcher.Invoke(() => { manuStatusBit23.Fill = new SolidColorBrush(IntecColors.dark_red); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit23.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### Unterspannung Zwischenkreis   ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 24)) == (0x00000001 << 24) )
                        Dispatcher.Invoke(() => { manuStatusBit24.Fill = new SolidColorBrush(IntecColors.dark_red); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit24.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### Ueberspannung Zwischenkreis ###//
                    if ((device.ManufacturerStatus & (0x00000001 << 25)) == (0x00000001 << 25) )
                        Dispatcher.Invoke(() => { manuStatusBit25.Fill = new SolidColorBrush(IntecColors.dark_red); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit25.Fill = new SolidColorBrush(IntecColors.light_grey); });

                    //### Hardware > Details Objekt  ###//
                    if ((device.ManufacturerStatus & ((uint)0x00000001 << 31)) == ((uint)0x00000001 << 31) )
                        Dispatcher.Invoke(() => { manuStatusBit31.Fill = new SolidColorBrush(IntecColors.dark_red); });
                    else
                        Dispatcher.Invoke(() => { manuStatusBit31.Fill = new SolidColorBrush(IntecColors.light_grey); });
                }
            }
            catch (Exception err)
            {
                MyTimer1.Stop();
                Console.WriteLine(err.ToString());
            }

        }
        #endregion

        #region Profile Position Mode
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //!!!!!!!!!!!! PPM DRIVE SEQUENCE !!!!!!!!!!!!!!!! PPM DRIVE SEQUENCE !!!!!!!!!!!!!!!!!!!! PPM DRIVE SEQUENCE !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        /// <summary>
        /// Add a new element to to the drive sequence data grid items source
        /// </summary>
        private void DataGridDriveSequenceAdd()
        {
            obj_dg_ds.Insert(obj_dg_ds.Count - 1,
                new CtrlDgItem() /*  */
                {
                    Id = obj_dg_ds.Count - 1,
                    AddRemoveText = new PackIconMaterial() { Kind = PackIconMaterialKind.Minus },
                    ButtonColor = new SolidColorBrush(IntecColors.light_red),
                    TargetPosition = 0,
                    Velocity = 500,
                    Acceleration = slider_max_acc / 2,
                    Deceleration = slider_max_dec / 2,
                    TimeWait = 1000,
                    TimeMinimum = ds_time_minimum,
                    TimeInterval = ds_time_interval,
                    TickPos = slide_size_ticks_pos,
                    TickVel = slide_size_ticks_vel,
                    TickAcc = slide_size_ticks_acc,
                    TickDec = slide_size_ticks_dec,
                    PB = new ProgressBar(),
                    MaxPosSlide = slider_max_pos,
                    MinPosSlide = -slider_max_pos,
                    MaxVelSlide = slider_max_vel,
                    MinVelSlide = 1,
                    MaxAccSlide = slider_max_acc,
                    MinAccSlide = 1,
                    MaxDecSlide = slider_max_dec,
                    MinDecSlide = 1,
                    RampType = 0,
                    Vis = Visibility.Visible
                });

            for (int i = 0; i < obj_dg_ds.Count; i++)
            {
                obj_dg_ds[i].Id = i;
            }
        }

        /// <summary>
        /// Insert a value for the target position when double click on the slide bar
        /// </summary>
        private async void DS_DoubleClick_TargetPosition(object sender, MouseButtonEventArgs e)
        {
            var obj = ((sender as TwoWaySlider).DataContext as CtrlDgItem);
            string str = await this.ShowInputAsync("Target Position", "Please introduce a target position.");
            var val = Convert.ToDouble(str);
            if (val > obj.MaxPosSlide)
            {
                obj.MaxPosSlide = Convert.ToInt32(val);
                obj.MinPosSlide = obj.MaxPosSlide * -1;
            }
            if (val < obj.MinPosSlide)
            {
                obj.MinPosSlide = Convert.ToInt32(val);
                obj.MaxPosSlide = obj.MinPosSlide * -1;
            }
            obj.TargetPosition = val;
        }

        /// <summary>
        /// Insert a value for the profile velocity when double click on the slide bar
        /// </summary>
        private async void DS_DoubleClick_TargetVelocity(object sender, MouseButtonEventArgs e)
        {
            CtrlDgItem obj = ((sender as Slider).DataContext as CtrlDgItem);
            string str = await this.ShowInputAsync("Velocity", "Please introduce a velocity.");
            double val = Convert.ToDouble(str);

            if (val <= 0)
            {
                return;
            }

            if (val > obj.MaxVelSlide)
            {
                obj.MaxVelSlide = Convert.ToInt32(val);
            }
            obj.Velocity = val;
        }

        /// <summary>
        /// Insert a value for the profile acceleration when double click on the slide bar
        /// </summary>
        private async void DS_DoubleClick_TargetAcceleration(object sender, MouseButtonEventArgs e)
        {
            var obj = ((sender as Slider).DataContext as CtrlDgItem);
            string str = await this.ShowInputAsync("Acceleration", "Please introduce an acceleration.");
            var val = Convert.ToDouble(str);

            if (val <= 0)
            {
                return;
            }

            if (val > obj.MaxAccSlide)
            {
                obj.MaxAccSlide = Convert.ToInt32(val);
            }
            obj.Acceleration = val;
        }

        /// <summary>
        /// Insert a value for the profile deceleration when double click on the slide bar
        /// </summary>
        private async void DS_DoubleClick_TargetDeceleration(object sender, MouseButtonEventArgs e)
        {
            var obj = ((sender as Slider).DataContext as CtrlDgItem);
            string str = await this.ShowInputAsync("Deceleration", "Please introduce a deceleration.");
            var val = Convert.ToDouble(str);

            if (val <= 0)
            {
                return;
            }

            if (val > obj.MaxDecSlide)
            {
                obj.MaxDecSlide = Convert.ToInt32(val);
            }
            obj.Deceleration = Convert.ToDouble(str);
        }

        /// <summary>
        /// Remove element from the drive sequence items source
        /// </summary>
        /// <param name="ID">This is the ID of the elemen in the list of the items source</param>
        private void DataGridDriveSequenceRemove(int ID)
        {
            obj_dg_ds.RemoveAt(ID);

            for (int i = 0; i < obj_dg_ds.Count; i++)
            {
                obj_dg_ds[i].Id = i;
            }
        }

        /// <summary>
        /// event triggered when clicking the ramp button to change between linear (0) ramp and sin^2 (1) ramp 
        /// </summary>
        public void Button_ChangeRampPPM(object sender, RoutedEventArgs e)
        {
            int ID = ((sender as Button).DataContext as CtrlDgItem).Id;

            obj_dg_ds[ID].RampType = (obj_dg_ds[ID].RampType == 0) ? obj_dg_ds[ID].RampType = 1 : obj_dg_ds[ID].RampType = 0;
        }

        /// <summary>
        /// Event triggerd when clickinmg on the button in the last column of the drive sequence dg. If the clicked element is the last one it will call to add an element, else it will remove the clicked element.
        /// </summary>
        public void Button_AddRemoveDS(object sender, RoutedEventArgs e)
        {
            int ID = ((sender as Button).DataContext as CtrlDgItem).Id;

            if (ID == obj_dg_ds.Count - 1)
            {
                DataGridDriveSequenceAdd();
            }
            else
            {
                DataGridDriveSequenceRemove(ID);
            }
        }

        /// <summary>
        /// Event of button to start the drive sequence. It will set the operation mode to PPM and then call the parallel task to run the drive sequence.
        /// </summary>
        private async void Button_StartDriveSequence(object sender, RoutedEventArgs e)
        {
            StartDS.IsEnabled = false;

            try
            {
                if (Communication.Connected)
                {
                    if (!(Communication.Devices[SelectedDevice] is PCS device))
                    {
                        MessageBox.Show("Selected device is not of type PCS.");
                        return;
                    }

                    device.SetOperMode(PCS.OM_PPM);

                    await Task.Delay(50);
                    StopDriveSequence(); /* Call the Stop sequence function every time the start is called to stop old thread */
                    await Task.Delay(50);

                    foreach (CtrlDgItem obj in obj_dg_ds)
                    {
                        obj.PB.Value = 0;
                    }

                    ds_halted = false; /*  */
                    TaskDriveSequence(driveSequence.ItemsSource as ObservableCollection<CtrlDgItem>, Convert.ToInt32(repeat.Value), device);

                    HaltContinueDS.Content = new PackIconMaterial() { Kind = PackIconMaterialKind.Pause };
                    HaltContinueDS.Foreground = new SolidColorBrush(IntecColors.dark_grey);
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }

            
            StartDS.IsEnabled = true;
        }

        public bool ds_halted;
        public bool ds_halt_in_wait;
        public bool DsRunning { get; set; }
        public int x1 = 0;
        public int x2 = 0;
        private CancellationTokenSource ts_ds = new CancellationTokenSource();
        public ManualResetEvent mrse = new ManualResetEvent(true);
        private enum DS_SM
        {
            START_POSITIONING = 1,
            RELEASE_POSITIONING = 2,
            WAIT_FOR_TARGET_REACHED = 3,
            WAIT_FOR_TIMER_EXPIRE = 4
        }
        public readonly object ds_locker = new object();
        /// <summary>
        /// Starts a Task which initializes and performs the drive sequence.
        /// </summary>
        /// <param name="ds">List with all the points of the drive sequence</param>
        /// <param name="repeat">Determines the amount of times the drve sequence must be repeated.</param>
        private void TaskDriveSequence(ObservableCollection<CtrlDgItem> ds, int repeat, PCS device)
        {
            DsRunning = true;
            ts_ds = new CancellationTokenSource();
            //DsRunning = true;

            CancellationToken ct = ts_ds.Token;
            Task.Factory.StartNew( async () => /* Start a new Task for the drive sequence */
            {
                
                ushort _state_previous;
                bool _while_loop_flag;
                DS_SM _actions;
                bool _stop_ds_flag = false;
                    
                device.SetControlWordPdo(device.CurrentCW, 0x0120); /* New positioning and  */

                Dispatcher.Invoke(() =>
                {
                    PBPPM.Value = 0;
                });
                
                for (int i = 0; i <= repeat; i++)
                {
                    Dispatcher.Invoke(() =>
                    {
                        CountDS.Content = i;
                    });

                    if (ct.IsCancellationRequested)
                    {
                        DsRunning = false;
                        return;
                    }

                    for (int j = 0; j < ds.Count - 1; j++) /* minus 1 because the last element of the */
                    {
                        x1 = device.ActualPosition;
                        x2 = Convert.ToInt32(ds[j].TargetPosition);
                        device.SetTargetPosition(x2);
                        if (ct.IsCancellationRequested)
                        {
                            DsRunning = false;
                            return;
                        }
                        device.SetProfileVelocity(Convert.ToUInt32(ds[j].Velocity));//
                        device.SetProfileAcceleration(Convert.ToUInt32(ds[j].Acceleration));//
                        device.SetProfileDeceleration(Convert.ToUInt32(ds[j].Deceleration));//
                        device.SetRampType(ds[j].RampType);
                        await Task.Delay(50);
                        if (ct.IsCancellationRequested)
                        {
                            DsRunning = false;
                            return;
                        }
                        Dispatcher.Invoke(() =>
                        {
                            driveSequence.SelectedItem = driveSequence.Items[j];
                            driveSequence.ScrollIntoView(driveSequence.SelectedItem);
                        });

                        _actions = DS_SM.START_POSITIONING;
                        _state_previous = 0;
                        _while_loop_flag = true;

                        while (_while_loop_flag)
                        {
                            if (ct.IsCancellationRequested) 
                            {
                                /* Jump out of loop to stop drive sequence task */
                                _stop_ds_flag = true;
                                DsRunning = false;
                                return;
                            }
                            else
                            {
                                mrse.WaitOne();
                            }

                            switch (_actions) /* State Machine to control the process of the start of the positioning */
                            {
                                case DS_SM.START_POSITIONING:
                                    device.SetControlWordPdo(device.CurrentCW, 0x0030); /* CW Set bit 4 and 5 */
                                    _actions = DS_SM.RELEASE_POSITIONING;
                                    break;
                                case DS_SM.RELEASE_POSITIONING:
                                    if (device.stateMachineDsp402.SetPointAcknowledged)
                                    {
                                        device.SetControlWordPdo(device.CurrentCW, 0x0000); /* CW Reset bit 4 and 5 */
                                        _actions = DS_SM.WAIT_FOR_TARGET_REACHED;
                                        /* clear bit 10 (to achieve rising edge (because of timings)) */
                                        device.stateMachineDsp402.StateWord &= 0xFBFF; 
                                    }
                                    break;
                                case DS_SM.WAIT_FOR_TARGET_REACHED:
                                    if (device.stateMachineDsp402.TargetReached)    /* Rising Edge TargetReached */
                                    {
                                        _actions = DS_SM.WAIT_FOR_TIMER_EXPIRE;
                                        if (j >= ds.Count - 2)
                                        {
                                            Dispatcher.Invoke(() =>
                                            {
                                                PBPPM.Value = (i + 1) * 100 / (repeat + 1);
                                            });
                                        }
                                    }
                                    break;
                                case DS_SM.WAIT_FOR_TIMER_EXPIRE:
                                    await Task.Delay(Math.Max(ds[j].TimeWait-50,1));
                                    _actions = DS_SM.START_POSITIONING;
                                    _while_loop_flag = false;
                                    break;
                            }
                            _state_previous = device.stateMachineDsp402.StateWord; /* previous state -  used to see rising edges */
                            await Task.Delay(1); /* delay task 1 millisecond to avoid high cpu workload */
                        }
                        if (_stop_ds_flag)
                        {
                            break;
                        }
                    }
                    if (_stop_ds_flag)
                    {
                        break;
                    }
                }
                DsRunning = false;
                
            }, ct);
        }
        

        /// <summary>
        /// function to stop drive sequence
        /// </summary>
        public void StopDriveSequence()
        {
            try
            {
                mrse.Set();
                if (ts_ds != null)
                {
                    lock (ts_ds)
                    {
                        ts_ds.Cancel();
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
            DsRunning = false;
        }

        /// <summary>
        /// Event triggered when clicking the halt button. It will pause the drive sequence task if is running, or let it continue if it is halted.
        /// </summary>
        private async void Button_HaltDriveSequence(object sender, RoutedEventArgs e)
        {
            if (!Communication.Connected)
            {
                return;
            }
            if ( !(Communication.Devices[SelectedDevice] is PCS device) )
            {
                return;
            }
            if  ( device.CurrentMode == PCS.OM_PPM )
            {
                if (!ds_halted)
                {
                    device.SetControlWordPdo(device.CurrentCW, HALT_BIT);
                    ds_halted = true;
                    mrse.Reset();
                    HaltContinueDS.Content = new PackIconMaterial() { Kind = PackIconMaterialKind.Play };
                    HaltContinueDS.Foreground = new SolidColorBrush(IntecColors.green);
                }
                else
                {
                    device.SetControlWordPdo(device.CurrentCW, 0x0000);
                    await Task.Delay(5);
                    mrse.Set();
                    ds_halted = false;
                    HaltContinueDS.Content = new PackIconMaterial() { Kind = PackIconMaterialKind.Pause };
                    HaltContinueDS.Foreground = new SolidColorBrush(IntecColors.dark_grey);
                }
            }
        }
        #endregion

        #region Profile Velocity Mode
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //!!!!!!!!!!!! PVM VELOCITY MODE !!!!!!!!!!!!!!!! PVM VELOCITY MODE !!!!!!!!!!!!!!!!!!!! PVM VELOCITY MODE !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        /// <summary>
        /// Event triggered when a property of the PVM items source list is changed. It's purpose is to reset the progress bar to 0 when the target velocity changes.
        /// </summary>
        private void VelocityPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        { 
            if (Communication.Connected) 
            { 
                if (!(Communication.Devices[SelectedDevice] is PCS device))
                { 
                    MessageBox.Show("Selected device is not of type PCS."); 
                    return; 
                }
                
                last_velocity = Convert.ToInt32(obj_dg_pvm[0].Velocity);
                device.SetTargetVelocity(last_velocity);
                
                last_acceleration = Convert.ToUInt32(obj_dg_pvm[0].Acceleration);
                device.SetProfileAcceleration(last_acceleration);

                last_deceleration = Convert.ToUInt32(obj_dg_pvm[0].Deceleration);
                device.SetProfileDeceleration(last_deceleration);
            }
        }

        /// <summary>
        /// Event triggered when the the acceleration slider changes value. Its purpose is to have a value greater than zero.
        /// </summary>
        private void Slider_ValueChanged_PVMAcc(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            (sender as Slider).Value = Math.Max(1, (sender as Slider).Value);
        }

        /// <summary>
        /// Event triggered when the the acceleration slider changes value. Its purpose is to have a value greater than zero.
        /// </summary>
        private void Slider_ValueChanged_PVMDec(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            (sender as Slider).Value = Math.Max(1, (sender as Slider).Value);
        }


        private async void PVM_DoubleClick_TargetVelocity(object sender, MouseButtonEventArgs e)
        {
            var obj = ((sender as TwoWaySlider).DataContext as CtrlDgItem);
            string str = await this.ShowInputAsync("Target Velocity", "Please introduce a target velocity.");
            var val = Convert.ToDouble(str);

            if (val > obj.MaxVelSlide)
            {
                obj.MaxVelSlide = Convert.ToInt32(val);
                obj.MinVelSlide = obj.MaxVelSlide * -1;
            }
            if (val < obj.MinVelSlide)
            {
                obj.MinVelSlide = Convert.ToInt32(val);
                obj.MaxVelSlide = obj.MinVelSlide * -1;
            }
            obj.Velocity = val;
        }

        private async void PVM_DoubleClick_Acceleration(object sender, MouseButtonEventArgs e)
        {
            var obj = ((sender as Slider).DataContext as CtrlDgItem);
            string str = await this.ShowInputAsync("Acceleration", "Please introduce an acceleration.");
            var val = Convert.ToDouble(str);

            if (val <= 0)
            {
                return;
            }

            if (val > obj.MaxAccSlide)
            {
                obj.MaxAccSlide = Convert.ToInt32(val);
            }
            obj.Acceleration = Convert.ToDouble(str);
        }

        private async void PVM_DoubleClick_Deceleration(object sender, MouseButtonEventArgs e)
        {
            var obj = ((sender as Slider).DataContext as CtrlDgItem);
            string str = await this.ShowInputAsync("Deceleration", "Please introduce a deceleration.");
            var val = Convert.ToDouble(str);

            if (val <= 0)
            {
                return;
            }

            if (val > obj.MaxDecSlide)
            {
                obj.MaxDecSlide = Convert.ToInt32(val);
            }
            obj.Deceleration = Convert.ToDouble(str);
        }

        /// <summary>
        /// Starts, halts and restarts the drive in profile velocity mode mode. It sets and resets the halt bit. It also resets the progress bar.
        /// </summary>
        private async void Button_HaltPVM(object sender, RoutedEventArgs e)
        {
            if (Communication.Connected)
            {
                if (!(Communication.Devices[SelectedDevice] is PCS device))
                {
                    MessageBox.Show("Selected device is not of type PCS.");
                    return;
                }
                
                device.SetOperMode(PCS.OM_PVM);

                if (device.stateMachineDsp402.IS_OPERATION_ENABLED())
                {

                    StopDriveSequence();
                    if (device.CurrentMode != PCS.OM_PVM)
                    {
                        await Task.Run(() =>
                        {
                            device.SetControlWordPdo(device.CurrentCW, 0x0100); /* Set halt bit */
                            device.SetTargetVelocity(Convert.ToInt32(obj_dg_pvm[0].Velocity));
                            device.SetProfileAcceleration(Convert.ToUInt32(obj_dg_pvm[0].Acceleration));
                            device.SetProfileDeceleration(Convert.ToUInt32(obj_dg_pvm[0].Deceleration));
                        });
                        await Task.Run(() =>
                        {
                            device.SetControlWordPdo(device.CurrentCW, 0x0000); /* Reset halt bit */
                        });
                    }
                    else
                    {
                        if (device.HaltBit)
                        {
                            await Task.Run(() =>
                            {
                                device.SetControlWordPdo(device.CurrentCW, 0x000);
                            });
                        }
                        else
                        {
                            await Task.Run(() =>
                            {
                                device.SetControlWordPdo(device.CurrentCW, 0x100);
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// event triggered when clicking the ramp button to change between linear (0) ramp and sin^2 (1) ramp 
        /// </summary>
        public async void Button_ChangeRampPVM(object sender, RoutedEventArgs e)
        {
            int ID = ((sender as Button).DataContext as CtrlDgItem).Id;

            if (obj_dg_pvm[ID].RampType == 0)
            {
                obj_dg_pvm[ID].RampType = 1;
            }
            else
            {
                obj_dg_pvm[ID].RampType = 0;
            }
            try
            {
                if (Communication.Connected && SelectedDevice > -1)
                {
                    await Task.Run(() =>
                    {
                        (Communication.Devices[SelectedDevice] as PCS).SetRampType(obj_dg_pvm[ID].RampType);
                    });
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
            
        }
        #endregion

        #region Homing
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //!!!!!!!!!!!!!!! HOMING MODE !!!!!!!!!!!!!!!!!!!!!! HOMING MODE !!!!!!!!!!!!!!!!!!!!!!!!!!! HOMING MODE !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        /// <summary>
        /// Sets on a parallel task the operation mode to Homing
        /// </summary>
        private async void Button_Click_HOMING(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Communication.Connected && SelectedDevice > -1)
                {
                    if (!(Communication.Devices[SelectedDevice] is PCS device))
                    {
                        MessageBox.Show("Selected device is not of type PCS.");
                        return;
                    }

                    var home_offset = Convert.ToInt32(TextBoxHomeOffset.Text);
                    var index_pulse = Convert.ToUInt32(TextBoxIndexPulse.Text);
                    var fast_speed = Convert.ToUInt32(TextBoxHomingSpeedFast.Text);
                    var slow_speed = Convert.ToUInt32(TextBoxHomingSpeedSlow.Text);
                    var home_acceleration = Convert.ToUInt32(TextBoxHomingAcceleration.Text);

                    StopDriveSequence();
                    if (Communication.Connected)
                    {
                        await Task.Run(() =>
                        {
                            sbyte test = 0;
                            Dispatcher.Invoke(() =>
                            {
                                try
                                {
                                    test = (sbyte)ComboBoxHoming.SelectedValue;
                                }
                                catch (Exception err)
                                {
                                    Console.WriteLine(err.ToString());
                                }
                            });
                            homing.SetOperModeHM((Communication.Devices[SelectedDevice] as PCS), test, home_offset, index_pulse, fast_speed, slow_speed, home_acceleration);

                        });
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
        }
        #endregion

        #region Scope
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //!!!!!!!!!!!! SCOPE !!!!!!!!!!!!!!!!!!! SCOPE !!!!!!!!!!!!!!!!!!!!!!! SCOPE !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        /// <summary>
        /// It changes the margin of the plot when the hamburger pane is open or closed by clicking on the hamburger button
        /// </summary>
        private void Hamburger1_HamburgerButtonClick(object sender, ItemClickEventArgs e)
        {
            _scopeUserControl.scope1.Margin = (_scopeUserControl.hamburger1.IsPaneOpen) ? new Thickness(240,0,0,0) : new Thickness(48,0,0,0);
        }

        /// <summary>
        /// Event triggered on the clicking of an item on the hamburger menu
        /// It has a switch that is controlled by the Id of the  Id of the clicked item.
        /// Case 0 is the last item of the list and will add a new element for a new plotting series in the plot.
        /// Case 1 is the first element and will start or stop the plot.
        /// Case 2 will reset all the axes on the plot.
        /// Case 3 will open and close the pane and accordingly change the margin of the plot.
        /// Default are the elements with plotting series extra to the first one and it will remove them.
        /// </summary>
        private void OnMenuItemClick1(object sender, ItemClickEventArgs e)
        {
            try
            {
                if (!(e.ClickedItem is HambMenuItemScope menuItem)
                || !(Communication.Devices[SelectedDevice] is PCS device))
                {
                    return;
                }
                    
                switch (menuItem.Id)
                {
                    case 0:
                        break;
                    case 1:
                        StartStopPlottingAction();
                        break;
                    case 2:
                        device.scopeControl.plotScope.PlotModel.ResetAllAxes();
                        device.scopeControl.plotScope.PlotModel.InvalidatePlot(true);
                        break;
                    case 3:
                        if (_scopeUserControl.hamburger1.IsPaneOpen)
                        {
                            _scopeUserControl.hamburger1.IsPaneOpen = false;
                            _scopeUserControl.scope1.Margin = new Thickness(48, 0, 0, 0);
                        }
                        else
                        {
                            _scopeUserControl.hamburger1.IsPaneOpen = true;
                            _scopeUserControl.scope1.Margin = new Thickness(240, 0, 0, 0);
                        }
                        break;
                    case 6:
                        RemoveScopeToSplitWindow();
                        break;
                    default:
                        break;
                }
                
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }

            _scopeUserControl.hamburger1.SelectedIndex = -1;
        }

        /// <summary>
        /// Starts or stops the pltos if the device is connected
        /// </summary>
        private void StartStopPlottingAction()
        {
            if (Communication.Connected  )
            {
                if (!(Communication.Devices[SelectedDevice] is PCS device))
                {
                    return;
                }

                if ( !flagScope1)
                {
                    device.scopeControl.plotScope.HideSeriesMarks();
                    StartPlots1();
                    device.scopeControl.Hamb1.Items[0].Name = "Stop Plot (F5)";
                    device.scopeControl.Hamb1.Items[0].Icon.Kind = PackIconMaterialKind.Pause;
                    device.scopeControl.Hamb1.Items[0].IconColor = new SolidColorBrush(IntecColors.yellow);
                    foreach (LineSeries ls in device.scopeControl.plotScope.PlotModel.Series)
                    {
                        ls.Points.Clear();
                    }
                    device.scopeControl.plotScope.PlotModel.ResetAllAxes();
                }
                else
                {
                    StopPlots1();
                    device.scopeControl.plotScope.ShowSeriesMarks();
                    device.scopeControl.plotScope.PlotModel.InvalidatePlot(true);
                }
            }
        }

        

        /// <summary>
        /// Cyclic time event to refresh the plots if the scope is active. It will lock the ScopeBuffer as soon as it becomes available, call the scope update function and then clear the buffer.
        /// It also sets the range of the X-axis according to the definded plot_window
        /// </summary>
        private void UpdatePlotsTimeEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (!(Communication.Devices[SelectedDevice] is PCS device))
                {
                    return;
                }

                if (flagScope1)
                {
                    lock (device.scopeControl.locker)
                    {
                        device.scopeControl.plotScope.PlotModel.InvalidatePlot(true);
                    }
                }
            }
            catch (Exception err)
            {
                _timerUpdatePlots.Stop();
                MessageBox.Show(err.ToString());
            }
        }

        private bool flagScope1;
        /// <summary>
        /// Starts the stopwatch for the plot, starts the timer for update plots and starts the scope task. sets the flagScope to true
        /// </summary>
        private void StartPlots1()
        {
            if (!(Communication.Devices[SelectedDevice] is PCS device))
            {
                return;
            }
               
            if ( Communication.Connected )
            {
                _timerUpdatePlots.Start();
                device.scopeControl.ScopeTask();
            }

            flagScope1 = true;
        }

        public void AddScopeElements(int plot_number)
        {
            
        }

        public void RemoveScopeElements(int rmvidx, int plot_number)
        {
            
        }
        
        /// <summary>
        /// Stops the stopwatch for the plot, Stops the timer for update plots and cancels the scope task. sets the flagScope to false
        /// </summary>
        private void StopPlots1()
        {
            try
            {
                flagScope1 = false;

                if (Communication.Connected)
                {
                    if (!(Communication.Devices[SelectedDevice] is PCS device))
                    {
                        return;
                    }

                    _timerUpdatePlots.Stop();
                    
                    if (device.scopeControl.ts_scope != null)
                    {
                        device.scopeControl.ts_scope.Cancel();
                    }

                    device.scopeControl.Hamb1.Items[0].Name = "Start Plot (F5)";
                    device.scopeControl.Hamb1.Items[0].Icon.Kind = PackIconMaterialKind.Play;
                    device.scopeControl.Hamb1.Items[0].IconColor = new SolidColorBrush(IntecColors.green);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
        }
        #endregion

        #region Object Dictionary
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //!!!!!!!!!!!! CoE Dictionary !!!!!!!!!!!!!!!! CoE Dictionary !!!!!!!!!!!!!!!!!!!! CoE Dictionary !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        /// <summary>
        /// Command called from the hotkey f6 to change from hex to dec data and viceversa
        /// </summary>
        private void CommandChangeDecHexCoeDictExcecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (Tabs.SelectedIndex == 4)
            {
                RefreshAllCoeValues();
            }
        }

        private void dataGridDictionary_Loaded(object sender, RoutedEventArgs e)
        {
            if (dataGridDictionary.SelectedItem != null)
            {
                dataGridDictionary.ScrollIntoView(dataGridDictionary.SelectedItem);
            }
                
        }

        private void CommandEscExcecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (Tabs.SelectedIndex == 4) { }/* ?? Should do something in Object Dicionary?? */
        }

        /// <summary>
        /// Event called when the content of the Search TextBox is changed.
        /// It filters the object dictionary according to the input (at the moment only filter for index)
        /// </summary>
        private void TxtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBoxName = (TextBox)sender;
            string filterText = textBoxName.Text;
            ICollectionView cv = ObjectDictionary.DictViewModel.CollectionView;

            cv.Filter = o =>
            {
                /* change to get data row value */
                DictItem p = o as DictItem;
                return (p.Index.ToString("X").ToUpper().Contains(filterText.ToUpper()) || p.Name.ToUpper().Contains(filterText.ToUpper()));
                /* end change to get data row value */
            };

            ObjectDictionary.DictViewModel.ExpandedSecondLevel = (filterText.Length >= 4) ? true : false;

            ObjectDictionary.DictViewModel.ExpandedFirstLevel = (filterText.Length >= 1) ? true : false;

            //if(ObjectDictionary.DictViewModel.ExpandedFirstLevel == false)
            //    ObjectDictionary.DictViewModel.ExpandedFirstLevel = true;
            //if (ObjectDictionary.DictViewModel.ExpandedSecondLevel == false)
            //    ObjectDictionary.DictViewModel.ExpandedSecondLevel = true;

            //ObjectDictionary.DictViewModel.ExpandedSecondLevel = true;
            //ObjectDictionary.DictViewModel.ExpandedFirstLevel = true;

            if (dataGridDictionary.Items.Count == 1)
            {
                dataGridDictionary.SelectedIndex = 0;
                RefreshCoeValues();
            }
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            var a = (sender as Expander).TemplatedParent as GroupItem;
            bool flag = false;

            dataGridDictionary.SelectedItems.Clear();

            if (a.DataContext.ToString() != "{DisconnectedItem}")
            {
                foreach (DictItem item in (a.DataContext as CollectionViewGroup).Items)
                {
                    dataGridDictionary.SelectedItems.Add(item);
                    flag = true;
                }

                if (flag)
                {
                    RefreshCoeValues();
                }
            }
        }

        /// <summary>
        /// This function write the set value on a Value Cell to the device.
        /// Triggerd when the Value cell of the dictionary loses focus (i.e. when user presses enter).
        /// </summary>
        private async void DictWriteObject(DictItem item)
        {
            if (Communication.Connected)
            {
                try
                {
                    var device = Communication.Devices[SelectedDevice] as PCS;
                    Ret ret = new Ret { wkc = 0, abort_code = 0 };

                    if (item.Access.Substring(0, 2) == "RW")
                    {
                        if (item.ValueDisplay == "")
                        {
                            item.Access = "RW";
                            return;
                        }
                       

                        DictionaryBuilder.SetValueFromDisplayString(item);

                        item.Access = "RW";

                        await Task.Delay(30);

                        await Task.Run(() =>
                        {
                            device.WriteDictionaryValue(item);
                        });
                    }
                }
                catch (Exception err)
                {
                    if (err.HResult == -2146233066)
                    {
                        item.Access = "RWR";
                    }
                    else if (err.HResult == -2146233033)
                    {
                        item.Access = "RWR";
                        MessageBox.Show("The input has the wrong format.");
                    }
                    else
                    {
                        MessageBox.Show(err.ToString());
                    }


                }
            }
            else
            {
                item.Access = "RWR";
            }
        }

        /// <summary>
        /// Read the values from the device of all selected objects
        /// </summary>
        private async void RefreshCoeValues()
        {
            try
            {
                if (Communication.Connected)
                {
                    var device = Communication.Devices[SelectedDevice] as PCS;
                    ObservableCollection<DictItem> selectedItems = new ObservableCollection<DictItem>();

                    foreach (DictItem mydi in dataGridDictionary.SelectedItems)
                    {
                        if (mydi.Access.Substring(0, 2) == "RW")
                        {
                            mydi.Access = "RW";
                        }
                        selectedItems.Add(mydi);
                    }
                    foreach (var si in selectedItems)
                    {
                        si.ValueDisplay = "";
                    }
                    await Task.Run(() =>
                    {
                        device.UpdateDictionaryValues(selectedItems);
                    });

                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
            
        }

        /// <summary>
        /// Read the values of all items in the Object Dictionary
        /// </summary>
        private async void RefreshAllCoeValues()
        {
            if (Communication.Connected)
            {
                foreach (var si in ObjectDictionary.listOfCoE)
                {
                    si.ValueDisplay = "";
                }

                await Task.Run(() =>
                {
                    (Communication.Devices[SelectedDevice] as PCS).UpdateDictionaryValues(ObjectDictionary.listOfCoE);
                });
            }
        }

        /// <summary>
        /// Read value of the element in the OD that is being double clicked
        /// </summary>
        private void dataGridDictionary_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            RefreshCoeValues();
        }
        
        /// <summary>
        /// changes the margin of the dictionary when the hamburger button is clicked to match the open/closed pane
        /// </summary>
        private void HamburgerDict_HamburgerButtonClick(object sender, ItemClickEventArgs e)
        {
            dataGridDictionary.Margin = (hamburgerDict.IsPaneOpen) ? new Thickness(240,0,0,0) : new Thickness(48,0,0,0);
        }
        
        /// <summary>
        /// Event triggeres when the items on the dict hamburger menu are clicked.
        /// Case 0 is calls a refresh of the selected items
        /// Case 1 opens the pane
        /// Case 2 calls the change between decimal and hexadecimal format
        /// </summary>
        private async void OnMenuItemClickDict(object sender, ItemClickEventArgs e)
        {
            if (!(e.ClickedItem is HambMenuItemDict menuItem))
            {
                return;
            }

            switch (menuItem.Id)
            {
                case 0:
                    RefreshCoeValues();
                    break;
                case 1:
                    hamburgerDict.IsPaneOpen = true;
                    dataGridDictionary.Margin = new Thickness(240, 0, 0, 0);
                    break;
                case 2:
                    DictionaryBuilder.ChangeDecHexDisplayValuesAllSelectedItems(dataGridDictionary);
                    break;
                case 3:
                    menuItem.Color = new SolidColorBrush(IntecColors.white);
                    //await Task.Delay(50);
                    if (Communication.Connected)
                    {
                        (Communication.Devices[SelectedDevice] as PCS).SdoWrite(0x1010, 0x01, (uint)0x65766173);

                        await Task.Delay(100);

                        if (ObjectDictionary.GetItem(0x1010,0x01).Access ==  "RWG" || ObjectDictionary.GetItem(0x1010, 0x01).Access == "RW")
                        {
                            menuItem.Color = new SolidColorBrush(IntecColors.green);
                        }
                        else
                        {
                            menuItem.Color = new SolidColorBrush(IntecColors.light_red);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Not connected to any device.");
                    }
                    break;
                case 4:
                    if (Communication.Connected)
                    {
                        (Communication.Devices[SelectedDevice] as PCS).SdoWrite(0x1011, 0x01, (uint)0x64616F6C);

                        await Task.Delay(100);

                        if (ObjectDictionary.GetItem(0x1011, 0x01).Access == "RWG" || ObjectDictionary.GetItem(0x1011, 0x01).Access == "RW")
                        {
                            menuItem.Color = new SolidColorBrush(IntecColors.green);
                        }
                        else
                        {
                            menuItem.Color = new SolidColorBrush(IntecColors.light_red);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Not connected to any device.");
                    }
                    break;
                case 5:
                    RefreshAllCoeValues();
                    break;
                default:
                    break;
            }
            hamburgerDict.SelectedIndex = -1;
        }
        
        /// <summary>
        /// Write the value of the object in the OD if the key pressed is Enter or Return
        /// </summary>
        private void TextBoxDictionary_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox txtbox = sender as TextBox;

            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                DictWriteObject((DictItem)txtbox.DataContext);
                txtbox.SelectAll();

                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                Console.WriteLine("XX");
            }
        }

        /// <summary>
        /// Change the displayed numeric type of the elemen in the OD when button pressed
        /// </summary>
        private void Button_Click_ChangeNumType(object sender, RoutedEventArgs e)
        {
            var item = (DictItem)(sender as Button).DataContext;
            
            DictionaryBuilder.ChangeDecHexDisplayValuesSingleItem(item);
        }
        #endregion

        #region General
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //!!!!!!!!!!!!!!!! GENERAL !!!!!!!!!!!!!!!!!!!!!!!!!! GENERAL !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! GENERAL !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        
        private async void Button_SetCwReset(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                if(Communication.Connected)
                {
                    var device = Communication.Devices[SelectedDevice] as PCS;
                    if (device.EcStateMachine == EC_SM.EC_STATE_OPER
                    || device.EcStateMachine == EC_SM.EC_STATE_SAFE_OP
                    || device.EcStateMachine == EC_SM.EC_STATE_PRE_OP
                    || Communication.commType == CommType.COMM_UDP)
                    {
                        device.SetControlWordPdo(StateMachine.SM_CW_FAULT_RESET, 0x0100);
                    }
                }
            });
        }
        //
        private async void Button_SetCwShutdown(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                if (Communication.Connected)
                {
                    var device = Communication.Devices[SelectedDevice] as PCS;
                    device.SetControlWordPdo(StateMachine.SM_CW_SHUTDOWN, 0x0100);
                }
            });
        }
        //
        private async void Button_SetCwSwitchOn(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                if (Communication.Connected)
                {
                    var device = Communication.Devices[SelectedDevice] as PCS;
                    //if ((int)device.EcStateMachine > 2)
                    //{
                        device.SetControlWordPdo(StateMachine.SM_CW_SWITCH_ON, 0x0100);
                    //}
                }
            });
        }
        //
        private async void Button_SetCwEnableOP(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                if (Communication.Connected)
                {
                    var device = Communication.Devices[SelectedDevice] as PCS;
                    device.SetControlWordPdo(StateMachine.SM_CW_ENABLE_OP, 0x0100);
                }
            });
        }
        //
        private async void Button_SetCwDisableOP(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                if (Communication.Connected)
                {
                    var device = Communication.Devices[SelectedDevice] as PCS;
                    device.SetControlWordPdo(StateMachine.SM_CW_DISABLE_OP, 0x0100);
                }
            });
        }
        //
        private async void Button_SetCwQuickStop(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                if (Communication.Connected)
                {
                    var device = Communication.Devices[SelectedDevice] as PCS;
                    device.SetControlWordPdo(StateMachine.SM_CW_QUICK_STOP, 0x0000);
                }
            });
        }

        #endregion
        
        /// <summary>
        /// Event when closing window
        /// used to stop all timers and disconnect from the device to avoid exeptions and error on the device
        /// </summary>
        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                e.Cancel = true;
                IsEnabled = false;

                MyTimer1.Stop();
                _timerUpdatePlots.Stop();
                
                if (Communication.Connected)
                {
                    Disconnect_Shutdown();
                }

                await Task.Delay(600);

                Application.Current.Shutdown();
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
        }
        
        /// <summary>
        /// Request Boot state from the ECAT state machine
        /// </summary>
        private async void Button_Click_Request_SM_Boot(object sender, RoutedEventArgs e)
        {
            progRingSM.IsActive = true;
            GridSmEcat.IsEnabled = false;
            try
            {
                if (SelectedDevice > -1 && Communication.commType == CommType.COMM_ECAT && Communication.Connected)
                {
                    if (SelectedDevice > -1)
                    {
                        await Task.Run(() => 
                        {
                            CommunicationSOEM.EcSmRequestStateBoot(Communication.Devices[SelectedDevice] as PCS);
                        });
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
            progRingSM.IsActive = false;
            GridSmEcat.IsEnabled = true;
        }
        
        /// <summary>
        /// Request Init state from the ECAT state machine
        /// </summary>
        private async void Button_Click_Request_SM_Init(object sender, RoutedEventArgs e)
        {
            GridSmEcat.IsEnabled = false;
            try
            {
                if (SelectedDevice > -1
                    && Communication.commType == CommType.COMM_ECAT && Communication.Connected)
                {
                    await Task.Run(() =>
                    {
                        CommunicationSOEM.EcSmRequestState(Communication.Devices[SelectedDevice] as PCS, EC_SM.EC_STATE_INIT);
                    });
                }
            }
            catch(Exception err)
            {
                MessageBox.Show(err.ToString());
            }
           
            GridSmEcat.IsEnabled = true;
        }

        /// <summary>
        /// Request PreOp state from the ECAT state machine
        /// </summary>
        private async void Button_Click_Request_SM_PreOp(object sender, RoutedEventArgs e)
        {
            GridSmEcat.IsEnabled = false;
            try
            {
                if (SelectedDevice > -1
                    && Communication.commType == CommType.COMM_ECAT && Communication.Connected)
                {
                    await Task.Run(() =>
                    {
                        CommunicationSOEM.EcSmRequestState(Communication.Devices[SelectedDevice] as PCS, EC_SM.EC_STATE_PRE_OP);
                    });
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
            GridSmEcat.IsEnabled = true;
        }

        /// <summary>
        /// Request SafeOp state from the ECAT state machine
        /// </summary>
        private async void Button_Click_Request_SM_SafeOp(object sender, RoutedEventArgs e)
        {
            GridSmEcat.IsEnabled = false;
            try
            {
                if (SelectedDevice > -1
                    && Communication.commType == CommType.COMM_ECAT && Communication.Connected)
                {
                    await Task.Run(() =>
                    {
                        CommunicationSOEM.EcSmRequestState(Communication.Devices[SelectedDevice] as PCS, EC_SM.EC_STATE_SAFE_OP);
                    });
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
            GridSmEcat.IsEnabled = true;
        }

        /// <summary>
        /// Request Oper state from the ECAT state machine
        /// </summary>
        private async void Button_Click_Request_SM_Oper(object sender, RoutedEventArgs e)
        {
            GridSmEcat.IsEnabled = false;
            try
            {
                if (SelectedDevice > -1
                    && Communication.commType == CommType.COMM_ECAT && Communication.Connected)
                {
                    await Task.Run(() => 
                    {
                        CommunicationSOEM.EcSmRequestState(Communication.Devices[SelectedDevice] as PCS, EC_SM.EC_STATE_OPER);
                    });
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
            GridSmEcat.IsEnabled = true;
        }

        /// <summary>
        /// Function to make a firmware update
        /// </summary>
        private async void Start_FW_Update_Button_Click(object sender, RoutedEventArgs e)
        {
            int ret = 0;
            uint foe_password = 0;

            string pw_ret;

            if( SelectedDevice < 0 || Communication.commType != CommType.COMM_ECAT )
            {
                MessageBox.Show("No device is selected or present.");
                return;
            }
            try
            {
                bttnFwUpdate.IsEnabled = false;
                GridSmEcat.IsEnabled = false;
                progRingSFwUpdate.IsActive = true;
                
                //if ((Communication.Devices[SelectedDevice] as PCS).EcStateMachine == EC_SM.EC_STATE_BOOT)
                {
                    //SMButtons.IsEnabled = false;

                    string fw_full_file_path = "";
                    string fw_file_name = "";

                    Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
                    {
                        Tag = "Select a firmware file",
                        DefaultExt = "",
                        Filter = "binary files (*.bin)|*.bin|ihex files (*.hex)|*.hex"
                    };

                    /* Display OpenFileDialog by calling ShowDialog method */
                    bool? result = dlg.ShowDialog();

                    /* Get the selected file name and display in a TextBox */ 
                    if (result == true)
                    {
                        fw_full_file_path = dlg.FileName;
                        fw_file_name = dlg.SafeFileName;
                    }
                    else
                    {
                        progRingSFwUpdate.IsActive = false;
                        bttnFwUpdate.IsEnabled = true;
                        GridSmEcat.IsEnabled = true;
                        return;
                    }

                    do
                    {
                        /* Ask por password of the Firmware update */
                        pw_ret = await this.ShowInputAsync("Password", "Please enter password (hex)");

                        if (pw_ret == null)
                        {
                            progRingSFwUpdate.IsActive = false;
                            bttnFwUpdate.IsEnabled = true;
                            GridSmEcat.IsEnabled = true;
                            return;
                        }

                        foe_password = 0;
                        if (pw_ret.Length == 0)
                        {
                            MessageBox.Show("No password was typed in. Try again!");
                        }

                    } while (pw_ret.Length == 0);
                    
                    try
                    {
                        foe_password = Convert.ToUInt32(pw_ret, 16);
                    }
                    catch (Exception)
                    {
                        await this.ShowMessageAsync("Bad input", "Please enter a valid hexadecimal number");
                        progRingSFwUpdate.IsActive = false;
                        bttnFwUpdate.IsEnabled = true;
                        GridSmEcat.IsEnabled = true;
                        return;
                    }

                    /* Request Init and then Boot state from the ECAT state machine */
                    await Task.Run(() => { CommunicationSOEM.EcSmRequestState(Communication.Devices[SelectedDevice] as PCS, EC_SM.EC_STATE_INIT); });
                    await Task.Run(() => { CommunicationSOEM.EcSmRequestStateBoot(Communication.Devices[SelectedDevice] as PCS); });

                    if ((Communication.Devices[SelectedDevice] as PCS).EcStateMachine == EC_SM.EC_STATE_BOOT)
                    {
                        /* Perform the firmware update */
                        await Task.Run(() => 
                        {
                            ret = CommunicationSOEM.DoFirmwareUpdate(SelectedDevice + 1, foe_password, fw_full_file_path, fw_file_name);
                        });
                        
                        if (ret > 0) /* If ret is greater than 0 the write was succesful, otherwise an error message will be displayed */
                        {
                            await this.ShowMessageAsync("Success!", "Write of file successful! The device is resetting. This should take a few seconds.\nReset is complete when an LED is on. Afterwards you can scan and connect again.");
                            scanned_devices.Clear();
                            Disconnect();
                        }
                        else
                        {
                            switch (ret)
                            {
                                case -5:
                                    await this.ShowMessageAsync("FoE Error", "Something went wrong");
                                    break;
                                case -10:
                                    await this.ShowMessageAsync("FoE Error: File Not Found (0x8001)", "");
                                    break;
                                case -11:
                                    await this.ShowMessageAsync("FoE Error: Access Denied (0x8002)", "Access denied. The password did not match.");
                                    break;
                                case -12:
                                    await this.ShowMessageAsync("FoE Error: Disk Full (0x8003)", "");
                                    break;
                                case -13:
                                    await this.ShowMessageAsync("FoE Error: Illegal (0x8004)", "An FoE operation did not conclude properly. Please restart the drive and try again.");
                                    break;
                                case -14:
                                    await this.ShowMessageAsync("FoE Error: Packet number wrong (0x8005)", "");
                                    break;
                                case -15:
                                    await this.ShowMessageAsync("FoE Error: File already exists (0x8006)", "");
                                    break;
                                case -16:
                                    await this.ShowMessageAsync("FoE Error: No user (0x8007)", "");
                                    break;
                                case -17:
                                    await this.ShowMessageAsync("FoE Error: Bootstrap only (0x8008)", "");
                                    break;
                                case -18:
                                    await this.ShowMessageAsync("FoE Error: Not in Bootstrap (0x8009)", "");
                                    break;
                                case -19:
                                    await this.ShowMessageAsync("FoE Error: No rights (0x800A)", "");
                                    break;
                                case -20:
                                    await this.ShowMessageAsync("FoE Error: Program error (0x800B)", "Program Error. Please power drive off and on and try again.");
                                    break;
                                case -21:
                                    await this.ShowMessageAsync("FoE Error: Wrong Checksum (0x800C)", "");
                                    break;
                                case -22:
                                    await this.ShowMessageAsync("FoE Error: Invalid firmware file (0x800D)", "");
                                    break;
                                default:
                                    await this.ShowMessageAsync("Error", "Something went wrong.");
                                    break;
                            }
                        }
                        
                    }
                    else
                    {
                        await this.ShowMessageAsync("Incorrect state", "Boot mode was not achieved. Please try again.");
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
            progRingSFwUpdate.IsActive = false;
            bttnFwUpdate.IsEnabled = true;
            GridSmEcat.IsEnabled = true;
        }
        
        // Button event for testing stuff
        private void Button_Click(object sender, RoutedEventArgs e) 
        {
            try
            {
                if (ContentScope_Second.DataContext == null)
                {
                    AddScopeToSplitWindow();
                }
                else
                {
                    RemoveScopeToSplitWindow();
                }
                
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }

        private void buttonSplitScope_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ContentScope_Second.DataContext == null)
                {
                    AddScopeToSplitWindow();
                }
                else
                {
                    RemoveScopeToSplitWindow();
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }

        private void AddScopeToSplitWindow()
        {
            ContentScope.DataContext = null;
            ContentScope_Second.DataContext = _scopeUserControl;
            GridX.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            GridX.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
        }

        private void RemoveScopeToSplitWindow()
        {
            ContentScope_Second.DataContext = null;
            ContentScope.DataContext = _scopeUserControl;
            GridX.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            GridX.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Auto);
        }
    }

    

    /// <summary>
    /// Items for the Hamburger Menu of the scope
    /// </summary>
    public class HambMenuItemScope : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public PackIconMaterial Icon { get; set; }
        public ComboBox Combo { get; set; }
        public CheckBox ChckBx { get; set; }
        public Button Bttn { get; set; }
        public Type PageType { get; set; }
        public Visibility Vis { get; set; }
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged("Name");
            }
        }
        private SolidColorBrush _iconColor;
        public SolidColorBrush IconColor
        {
            get { return _iconColor; }
            set
            {
                _iconColor = value;
                OnPropertyChanged("IconColor");
            }
        }
        private List<HambMenuItemScope> _items;
        public List<HambMenuItemScope> Items
        {
            get { return _items; }
            set
            {
                _items = value;
                OnPropertyChanged("Items");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public static List<HambMenuItemScope> GetOptionsItems()
        {
            var items = new List<HambMenuItemScope>
            {
                new HambMenuItemScope() { Icon = new PackIconMaterial(), Name = "OptionItem1" }
            };
            return items;
        }
    }
    
    /// <summary>
    /// ViewModel containing the elements of the Scope Hamburger Menu used for its DataContext
    /// </summary>
    public class HambViewModelScope : INotifyPropertyChanged
    {
        private ObservableCollection<HambMenuItemScope> _items;
        public ObservableCollection<HambMenuItemScope> Items
        {
            get { return _items; }
            set
            {
                _items = value;
                OnPropertyChanged("Items");
            }
        }

        private ObservableCollection<HambMenuItemScope> _options;
        public ObservableCollection<HambMenuItemScope> Options
        {
            get { return _options; }
            set
            {
                _options = value;
                OnPropertyChanged("Options");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
    
    /// <summary>
    /// Items for the Hamburger Menu of the CoE Dictionary
    /// </summary>
    public class HambMenuItemDict : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public PackIconMaterial Icon { get; set; }
        public Visibility Vis { get; set; }
        public TextBox TxtBox { get; set; }
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged("Name");
            }
        }
        private SolidColorBrush _color;
        public SolidColorBrush Color
        {
            get { return _color; }
            set
            {
                _color = value;
                OnPropertyChanged("Color");
            }
        }

        public ObservableCollection<HambMenuItemDict> GetItems()
        {
            var items = new ObservableCollection<HambMenuItemDict>
            {
                new HambMenuItemDict() { Id = 0, Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.Refresh }, Name = "Refresh selected (F5)", Vis = Visibility.Hidden, Color = new SolidColorBrush((IntecColors.white)) },
                new HambMenuItemDict() { Id = 5, Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.Refresh }, Name = "Refresh all (F6)", Vis = Visibility.Hidden, Color = new SolidColorBrush((IntecColors.white)) },
                new HambMenuItemDict() { Id = 1, Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.FileFind }, Name = "Find", Vis = Visibility.Visible, TxtBox = new TextBox(){ Width=192, FontSize=14, VerticalContentAlignment=System.Windows.VerticalAlignment.Center }, Color = new SolidColorBrush((IntecColors.white)) },
                new HambMenuItemDict() { Id = 2, Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.Numeric }, Name = "Rotate numeric type", Vis = Visibility.Hidden, Color = new SolidColorBrush((IntecColors.white)) },
                new HambMenuItemDict() { Id = 3, Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.Download }, Name = "Save parameters", Vis = Visibility.Hidden, Color = new SolidColorBrush((IntecColors.white)) },
                //new HambMenuItemDict() { Id = 4, Icon = new PackIconMaterial() { Kind = PackIconMaterialKind.Upload }, Name = "Load defaults", Vis = Visibility.Hidden, Color = new SolidColorBrush((new IntecColors().white)) },
            };
            return items;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public static List<HambMenuItemDict> GetOptionsItems()
        {
            var items = new List<HambMenuItemDict>
            {
                new HambMenuItemDict() { Icon = new PackIconMaterial(), Name = "OptionItem1" }
            };
            return items;
        }
        
    }
    
    /// <summary>
    /// Selector for the stype of certain groups in a DataGrid
    /// </summary>
    public class GroupItemStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            Style s;

            CollectionViewGroup group = item as CollectionViewGroup;
            Window window = Application.Current.MainWindow;

            if (!group.IsBottomLevel)
            {
                s = window.FindResource("FirstLevel") as Style;
            }
            else
            {
                s = window.FindResource("SecondLevel") as Style;
            }

            return s;
        }
    }
    
}