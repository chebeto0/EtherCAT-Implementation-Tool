using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Net;
using MahApps.Metro.Controls.Dialogs;
using EtherCAT_Master.Core.Communication;
using EtherCAT_Master.Core.Dictionary;

namespace EtherCAT_Master.Core.Controls
{
    /// <summary>
    /// Interaction logic for UdpCommControl.xaml
    /// </summary>
    public partial class UdpCommControl : UserControl
    {
        
        public MainWindow MW;
        
        public string Ipaddress { get; private set; }

        public UdpCommControl(MainWindow mw)
        {
            InitializeComponent();

            MW = mw;
            
            ipTextBox.FirstSegment.Text = "192";
            ipTextBox.SecondSegment.Text = "168";
            ipTextBox.ThirdSegment.Text = "1";
            ipTextBox.LastSegment.Text = "10";

            Ipaddress = ipTextBox.Address;

            //checkboxEoeRw.DataContext = MW.Communication ;
        }

        private async void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            buttonConnect.IsEnabled = false;
            progRingConnect.IsActive = true;
            
            Ipaddress = ipTextBox.Address;

            bool ConnectedSuccess = false;

            try
            {
                bool pingSuccess = false;

                /* I think I did this on an awaited thread so that the progress ring will work while pinging */
                await Task.Run(() =>
                {
                    if (!MW.Communication.Connected)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            if (PingHost(Ipaddress))
                            {
                                pingSuccess = true;
                                break;
                            }
                            else if (i == 3)
                            {
                                MessageBox.Show("UNREACHABLE: Host address unreachable.");
                                pingSuccess = false;
                            }
                        }
                    }
                    else
                    {
                        pingSuccess = false;
                    }
                });

                if (pingSuccess)
                {
                    ConnectedSuccess = CheckIfCommPossibleWithPCS(Ipaddress);
                }

                if (ConnectedSuccess)
                {
                    buttonConnect.Content = "Disconnect";
                    buttonConnect.Background = new SolidColorBrush(IntecColors.blue);
                }
                else
                {
                    MW.Communication.Disconnect();
                    buttonConnect.Content = "Connect";
                    buttonConnect.Background = new SolidColorBrush(IntecColors.bg_grey);
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
            
            buttonConnect.IsEnabled = true;
            progRingConnect.IsActive = false;
        }

        private bool CheckIfCommPossibleWithPCS(string ipaddress)
        {
            MW.Communication = new CommunicationUDP(this, checkboxEoeRw);
            MW.SelectedDevice = 0;
            MW.ChooseComm.DataContext = MW.Communication;
            MW.Communication.AsyncRead(1, 0x1018, 0x01);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (MW.ObjectDictionary.dictOfCoE[DictionaryBuilder.MakeKey(0x1018, 0x01)].Value == null)
            {
                if (sw.ElapsedMilliseconds > 5000)
                {
                    throw new TimeoutException("A timeout occured. No UDP connection was achieved.");
                }
            }

            if ( (uint)MW.ObjectDictionary.GetItem(0x1018, 0x01).Value == 0x29 )  /* Intec Motor */
            {
                dataGridDevices.ItemsSource = MW.Communication.Devices;
                dataGridDevices.SelectedIndex = 0;

                MW.MyTimer1.Start();

                return true;
            }

            return false;
        }
        
        /// <summary>
        /// Send a Ping to the given adress and see if is pingable
        /// </summary>
        /// <param name="nameOrAddress">IP addres</param>
        /// <returns></returns>
        private bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }

            return pingable;
        }

        private async void checkboxEoeRw_Checked(object sender, RoutedEventArgs e)
        {
            string pw_ret = await MW.ShowInputAsync("Password", "Warning: only procede if you know what you are doing.\nPlease enter password!");
            if (pw_ret == null)
            {
                MW.Communication.WriteFlag = false;
                (sender as CheckBox).IsChecked = false;
                return;
            }
            if (pw_ret == "29")
            {
                MW.Communication.WriteFlag = true;
                //(sender as CheckBox).IsChecked = true;

            }
            else
            {
                MW.Communication.WriteFlag = false;
                (sender as CheckBox).IsChecked = false;
            }
        }

        private void checkboxEoeRw_Unchecked(object sender, RoutedEventArgs e)
        {
            MW.Communication.WriteFlag = false;
        }


    }


}
