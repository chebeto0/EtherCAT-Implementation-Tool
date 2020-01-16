using EtherCAT_Master.Core.Dictionary;
using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace EtherCAT_Master.Core.Controls
{

    public class HaltBitPvmConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool _haltBit)
            {
                if (_haltBit) /* Zustand: Operation Enabled */
                {
                    return new PackIconMaterial() { Kind = PackIconMaterialKind.Play };
                }
                else
                {
                    return new PackIconMaterial() { Kind = PackIconMaterialKind.Pause };
                }
            }
            else
            {
                return new PackIconMaterial() { Kind = PackIconMaterialKind.Play };
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class BoolInvertConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Converter to set the background of the textbox of a dictionary item depending on its access type.
    /// </summary>
    public class DictionaryReadOnlyBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string access = (string)value;

            if (access == "RW")
            {
                return new SolidColorBrush(Color.FromRgb(0xff, 0xff, 0xff));
            }
            else if (access == "RO")
            {
                return new SolidColorBrush(Color.FromRgb(0xf0, 0xf0, 0xf0));
            }
            else if (access == "RWG")
            {
                return new SolidColorBrush(IntecColors.green);
            }
            else if (access == "RWR")
            {
                return new SolidColorBrush(IntecColors.light_red);
            }
            else
            {
                return new SolidColorBrush(Color.FromRgb(0xff, 0xff, 0xff));
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Converter to boolean for the access type of a dictionary object
    /// If the value is "RW" it will convert to true, otherwise to false
    /// </summary>
    public class DictionaryReadOnlyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string access = (string)value;
            return access.Substring(0, 2) == "RW";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class EcStateToStringConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is EC_SM ec_sm_state)
            {
                switch (ec_sm_state)
                {
                    case EC_SM.EC_SM_NA:
                        return "N/A";
                    case EC_SM.EC_SM_ERROR:
                        return "Error";
                    case EC_SM.EC_STATE_BOOT:
                        return "Boot";
                    case EC_SM.EC_STATE_INIT:
                        return "Init";
                    case EC_SM.EC_STATE_PRE_OP:
                        return "PreOp";
                    case EC_SM.EC_STATE_SAFE_OP:
                        return "SafeOp";
                    case EC_SM.EC_STATE_OPER:
                        return "Oper";
                    default:
                        return "-";
                }
            }

            return "-";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class NumericTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            NUM_TYPE type = (NUM_TYPE)value;
            string s_type;

            switch (type)
            {
                case NUM_TYPE.DEC:
                    s_type = "DEC";
                    break;
                case NUM_TYPE.HEX:
                    s_type = "HEX";
                    break;
                case NUM_TYPE.BIN:
                    s_type = "BIN";
                    break;
                default:
                    s_type = "";
                    break;
            }

            return s_type;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Converter for DataGrid elements to group certain elements
    /// </summary>
    public class GroupSizeToExpanderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            CollectionViewGroup grp = (CollectionViewGroup)value;
            return grp.Items.Count() > 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class StatuswordColorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ushort state = (ushort)value;

            if ((state & 0x006F) == 0x0027) /* Zustand: Operation Enabled */
            {
                return new SolidColorBrush(IntecColors.green);
            }
            else if ((state & 0x004F) == 0x0008)  /* Zustand: Fault */
            {
                return new SolidColorBrush(IntecColors.light_red);
            }
            else
            {
                return new SolidColorBrush(IntecColors.yellow);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
