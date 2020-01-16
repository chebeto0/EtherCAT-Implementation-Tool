using EtherCAT_Master.Core.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EtherCAT_Master.Core.Controls
{
    /// <summary>
    /// Interaction logic for EtherCatControl.xaml
    /// </summary>
    public partial class EtherCatControl : UserControl
    {

        public MainWindow MW;

        public EtherCatControl(MainWindow mw)
        {
            InitializeComponent();

            MW = mw;
        }

        private void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            MW.Disconnect_Shutdown();
        }

        private void Button_ScanDevices(object sender, RoutedEventArgs e)
        {
            try
            {
                byte[] buf = new byte[1024];
                int flag = 0;

                MW.Communication = new CommunicationSOEM(this);

                ButtonScan.DataContext = MW.Communication;
                ButtonDisconnect.DataContext = MW.Communication;

                MW.ChooseComm.DataContext = MW.Communication;

                if ((MW.Communication.SlaveCount = (MW.Communication as CommunicationSOEM).EcConnectExtern(0, MW.AdapterNumber, buf)) > 0)
                {
                    string ret = Encoding.ASCII.GetString(buf);
                    var ret_splitstring = ret.Split('*');

                    MW.scanned_devices.Clear();

                    for (int i = 0; i < ret_splitstring.Length - 1; i++)
                    {
                        MW.scanned_devices.Add(ret_splitstring[i]);
                    }

                    MW.Communication.Devices.Clear();

                    MW.Communication.Connected = true;
                    for (int i = 1; i <= MW.Communication.SlaveCount; i++)
                    {
                        MW.Communication.AsyncRead(i, 0x1018, 0x01);
                        if ((uint)MW.ObjectDictionary.GetItem(0x1018, 0x01).Value == 0x29) /* Intec Motor */
                        {
                            MW.Communication.Devices.Add(new PCS(MW.Communication, i, MW.scanned_devices[i - 1], MW.ObjectDictionary));

                            if (flag < 0)
                            {
                                flag = i - 1;
                            }
                        }
                        else
                        {
                            MW.Communication.Devices.Add(new OtherVendorDevice(MW.Communication, i, MW.scanned_devices[i - 1]));
                        }
                    }

                    dataGridDevices.ItemsSource = MW.Communication.Devices;

                    dataGridDevices.SelectedIndex = flag;

                    (MW.Communication as CommunicationSOEM).StartPdoExchangeTask();

                    MW.Communication.Connected = true;
                    MW.MyTimer1.Start();

                }
                else
                {
                    MessageBox.Show("Warning.\nNo EtherCAT devices found. Make sure that your EtheCAT device is powered on and connected to the right Network Interface Controller (NIC), and that the right NIC is selected under the \"General\" tab.");
                }
                
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString());
            }
        }
    }

}
    
