using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EtherCAT_Master.Core.Communication
{
    public enum CommType
    {
        COMM_OFF  = 0,
        COMM_ECAT = 1,
        COMM_UDP  = 2,
    }

    public abstract class CommunicationBase : INotifyPropertyChanged, IDisposable
    {
        public CommType commType = CommType.COMM_OFF;
        private bool    _disposed = false;

        public ObservableCollection<object> Devices = new ObservableCollection<object>();
        public MultimediaTimer MmTimer = new MultimediaTimer();

        protected readonly object comm_locker = new object();
        

        private bool _connected = false;
        public bool Connected
        {
            get { return _connected; }
            set
            {
                _connected = value;
                OnPropertyChanged("Connected");
            }
        }

        private bool _writeFlag = false;
        public bool WriteFlag
        {
            get { return _writeFlag; }
            set
            {
                _writeFlag = value;
                OnPropertyChanged("WriteFlag");
            }
        }

        public int SlaveCount { get; set; }

        protected MainWindow MW;
        
        public CommunicationBase()
        {
        }
        
        /// <summary>
        /// Used for asynchronous reading of objects. ie SDO communication or non process data object
        /// </summary>
        /// <param name="slave_number">Position of the slave in an EtherCAT network. Only used for direct EtherCAT</param>
        /// <param name="index">Index of the object to read</param>
        /// <param name="subindex">Subindex of the object to read</param>
        public abstract void AsyncRead(int slave_number, ushort index, byte subindex);

        /// <summary>
        /// Used for asynchronous writing of objects. ie SDO communication or non process data object.
        /// </summary>
        /// <param name="slave_number">Position of the slave in an EtherCAT network. Only used for direct EtherCAT</param>
        /// <param name="index">Index of the object to write</param>
        /// <param name="subindex">Subindex of the object to write</param>
        /// <param name="value">Value to write into the object. Use typecast for the type of data when using this value</param>
        public abstract void AsyncWrite(int slave_number, ushort index, byte subindex, object value);
        
        /// <summary>
        /// "Disconnects" the Communication object from the device.
        /// </summary>
        public abstract void Disconnect();
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged( string name )
        {
            PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( name ));
        }

        /// <summary>
        /// Disposes instance of the object.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                if (Connected)
                {
                    Disconnect();
                }
                //if (disposing)
                //{
                //}
            }
            _disposed = true;
            GC.SuppressFinalize(this);
        }

    }
}
