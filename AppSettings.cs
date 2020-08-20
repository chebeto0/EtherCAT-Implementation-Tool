using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace EtherCAT_Master
{
    public class AppSettings
    {

        public int SliderMaxVelocity { get; set; }
        public int SliderStepVelocity { get; set; }
        public int SliderMaxAcceleration { get; set; }
        public int SliderStepAcceleration { get; set; }
        public int SliderMaxDeceleration { get; set; }
        public int SliderStepDeceleration { get; set; }
        public int SliderMaxPosition { get; set; }
        public int SliderStepPosition { get; set; }
        public double DriveSequenceTimeMinimum { get; set; }
        public double DriveSequenceTimeInterval { get; set; }

        private int _numberNetworkAdapter;
        public int NumberNetworkAdapter
        {
            get { return _numberNetworkAdapter; }
            set
            {
                if (_numberNetworkAdapter != value)
                {
                    _numberNetworkAdapter = value;
                    configCollection["number_network_adapter"].Value = _numberNetworkAdapter.ToString();
                    Save();
                }
            }
        }
        private string _lastOpenPyScript;
        public string LastOpenPyScript
        {
            get { return _lastOpenPyScript; }
            set
            {
                if (_lastOpenPyScript != value)
                {
                    _lastOpenPyScript = value;
                    configCollection["last_open_py_script"].Value = _lastOpenPyScript;
                    Save();
                }
            }
        }

        private Configuration configManager;
        private KeyValueConfigurationCollection configCollection;


        public AppSettings()
        {
            configManager = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configCollection = configManager.AppSettings.Settings;

            DriveSequenceTimeMinimum = Convert.ToDouble(configCollection["drice_sequence_time_minimum"].Value);
            DriveSequenceTimeInterval = Convert.ToDouble(configCollection["drice_sequence_time_interval"].Value);
            SliderMaxVelocity = Convert.ToInt32(configCollection["slider_max_vel"].Value);
            SliderStepVelocity = Convert.ToInt32(configCollection["slider_step_vel"].Value);
            SliderMaxAcceleration = Convert.ToInt32(configCollection["slider_max_acc"].Value);
            SliderStepAcceleration = Convert.ToInt32(configCollection["slider_step_acc"].Value);
            SliderMaxDeceleration = Convert.ToInt32(configCollection["slider_max_dec"].Value);
            SliderStepDeceleration = Convert.ToInt32(configCollection["slider_step_dec"].Value);
            SliderMaxPosition = Convert.ToInt32(configCollection["slider_max_pos"].Value);
            SliderStepPosition = Convert.ToInt32(configCollection["slider_step_pos"].Value);

            NumberNetworkAdapter = Convert.ToInt32(configCollection["number_network_adapter"].Value);

            LastOpenPyScript = configCollection["last_open_py_script"].Value;

            configManager.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configManager.AppSettings.SectionInformation.Name);
        }

        public void Save()
        {
            configManager.Save(ConfigurationSaveMode.Modified);
        }


    }
}
