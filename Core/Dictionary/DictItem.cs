using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace EtherCAT_Master.Core.Dictionary
{
    public class DictItem : INotifyPropertyChanged
    {
        public ushort Index { get; set; }
        public byte Subindex { get; set; }
        public string IdxWithName { get; set; }
        public string Name { get; set; }
        public int Length { get; set; }
        public bool Signed { get; set; }
        public string ObjType { get; set; }
        public string DictStruct { get; set; }
        public uint TimeStamp { get; set; }

        private string _type;
        public string Type
        {
            get { return _type; }
            set
            {
                _type = value.Truncate(6);
            }
        }

        private string _access;
        public string Access
        {
            get { return _access; }
            set
            {
                _access = value;
                OnPropertyChanged("Access");
            }
        }
        private object _value;
        public object Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged("Value");
            }
        }
        private string _value_display;
        public string ValueDisplay
        {
            get { return _value_display; }
            set
            {
                _value_display = value;
                OnPropertyChanged("ValueDisplay");
            }
        }
        private bool _expanded_first_level;
        public bool ExpandedFirstLevel
        {
            get { return _expanded_first_level; }
            set
            {
                _expanded_first_level = value;
                OnPropertyChanged("ExpandedFirstLevel");
            }
        }
        private bool _expanded_second_level;
        public bool ExpandedSecondLevel
        {
            get { return _expanded_second_level; }
            set
            {
                _expanded_second_level = value;
                OnPropertyChanged("ExpandedSecondLevel");
            }
        }
        private NUM_TYPE _numeric_type;
        public NUM_TYPE NumericType
        {
            get { return _numeric_type; }
            set
            {
                _numeric_type = value;
                OnPropertyChanged("NumericType");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class DictItemViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<DictItem> ListOfCoE { get; set; }
        public ICollectionView CollectionView { get; set; }
        public ObservableCollection<GroupDescription> GrpDesc { get; set; }
        private bool _expanded_first_level;
        public bool ExpandedFirstLevel
        {
            get { return _expanded_first_level; }
            set
            {
                _expanded_first_level = value;
                OnPropertyChanged("ExpandedFirstLevel");
            }
        }
        private bool _expanded_second_level;
        public bool ExpandedSecondLevel
        {
            get { return _expanded_second_level; }
            set
            {
                _expanded_second_level = value;
                OnPropertyChanged("ExpandedSecondLevel");
            }
        }

        public DictItemViewModel(ObservableCollection<DictItem> listOfCoE)
        {
            ExpandedFirstLevel = false;
            ExpandedSecondLevel = false;
            ListOfCoE = listOfCoE;
            CollectionView = CollectionViewSource.GetDefaultView(listOfCoE);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("DictStruct");
            CollectionView.GroupDescriptions.Add(groupDescription);
            CollectionView.GroupDescriptions.Add(new PropertyGroupDescription("IdxWithName"));

            GrpDesc = CollectionView.GroupDescriptions;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
