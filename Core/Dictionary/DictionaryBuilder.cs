using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Xml.Linq;
using System.Windows.Media;
using System.Windows.Input;
using System.Globalization;
using System.Windows.Controls;
using System.Text;

namespace EtherCAT_Master.Core.Dictionary
{

    public enum NUM_TYPE
    {
        DEC = 1,
        HEX = 2,
        BIN = 3,
    }

    public class DictionaryBuilder
    {
        private string idxsubidx;
        private ushort idx;
        private byte subidx;
        private string name;
        private string type;
        private string access;
        private int bit_size;

        public Dictionary<string, DictItem> dictOfCoE = new Dictionary<string, DictItem>();
        public ObservableCollection<DictItem> listOfCoE = new ObservableCollection<DictItem>();
        public DictItemViewModel DictViewModel;

        public Dictionary<string, string> simpleDict = new Dictionary<string, string>();
        private bool _disposed;

        public DictionaryBuilder(string file)
        {
            idx = 0;
            subidx = 0;
            idxsubidx = MakeKey(idx, subidx);
            name = "";
            type = "";
            access = "";
            bit_size = 0;
            MakeDictionaryFromXml(file);
            DictViewModel = new DictItemViewModel(listOfCoE);
            BuildSimpleDict();
        }

        /// <summary>
        /// Build the dictionary of CoE from the ESI file
        /// This is basically a parser of the ESI file which is a mess because the darn ESI file is a mess too.
        /// I cannot explain all it does but basically it reads all the Elements in the ESI files called Object and get their information and add them to the dictionary.
        /// If the object has subitems it reads the datatype objects from the esi file because the information is there, and if the object is an array it does something else involving another datatype object.
        /// </summary>
        /// <param name="file">Full path to the ESI file</param>
        private void MakeDictionaryFromXml(string file)
        {
            var _dict_struct = "";
            var doc = XDocument.Load(file);
            var objects = doc.Descendants("Object");
            var data_types = doc.Descendants("DataType");
            var rootNodes = doc.DescendantNodes().OfType<XElement>();
            List<XElement> allItems = rootNodes.ToList();

            foreach (var obj in objects)
            {
                idx = Convert.ToUInt16(obj.Element("Index").Value.Substring(2), 16);
                name = obj.Element("Name").Value;
                type = obj.Element("Type").Value;
                bit_size = Convert.ToInt32(obj.Element("BitSize").Value);

                if (idx < 0x2000)
                {
                    _dict_struct = "1000 - 1FFF  Communication profile area";
                }
                else if (idx >= 0x2000 && idx < 0x6000)
                {
                    _dict_struct = "2000 - 5FFF  Manufacturer-specific profile area";
                }
                else
                {
                    _dict_struct = "6000 - FFFF  Standardized profile area";
                }

                if (obj.Elements("Info").Count() == 0)
                {
                    subidx = 0;
                    idxsubidx = MakeKey(idx, subidx);
                    access = obj.Element("Flags").Element("Access").Value.ToUpper();

                    dictOfCoE.Add(idxsubidx, new DictItem() { Index = idx, Subindex = subidx, IdxWithName = idx + " " + name, Name = name, NumericType = NUM_TYPE.DEC, Type = type, Access = access, Length = bit_size / 8, Signed = (type[0] != 'U'), ObjType = "one_obj", DictStruct = _dict_struct, ValueDisplay = "" });
                    listOfCoE.Add(dictOfCoE[idxsubidx]);
                }
                else if (obj.Element("Info").Elements("SubItem").Count() == 0)
                {
                    subidx = 0;
                    idxsubidx = MakeKey(idx, subidx);
                    access = obj.Element("Flags").Element("Access").Value.ToUpper();

                    dictOfCoE.Add(idxsubidx, new DictItem() { Index = idx, Subindex = subidx, IdxWithName = idx + " " + name, Name = name, NumericType = NUM_TYPE.DEC, Type = type, Access = access, Length = bit_size / 8, Signed = (type[0] != 'U'), ObjType = "one_obj", DictStruct = _dict_struct, ValueDisplay = "" });
                    listOfCoE.Add(dictOfCoE[idxsubidx]);
                }
                else
                {
                    string _idx_with_name = string.Format("{0:X4}   {1}", idx, name);
                    string match = "";
                    string dt_name;
                    foreach (var dt in data_types)
                    {
                        ushort idx_dt = 0;
                        if (dt.Elements("SubItem").Count() > 0)
                        {
                            idx_dt = Convert.ToUInt16(dt.Element("Name").Value.Substring(2), 16);
                        }

                        if (dt.Elements("Name").Count() > 0)
                        {
                            dt_name = dt.Element("Name").Value;
                        }
                        else
                        {
                            continue;
                        }
                        
                        if (idx == idx_dt)
                        {
                            Regex arr_regex = new Regex("DT[0-9a-fA-F]+ARR");
                            Match m;

                            List<XElement> subs = dt.Elements("SubItem").ToList();

                            for (byte i = 0; i < subs.Count(); i++)
                            {
                                type = subs[i].Element("Type").Value;
                                m = arr_regex.Match(type);

                                if (!m.Success)
                                {
                                    subidx = Convert.ToByte(subs[i].Element("SubIdx").Value);
                                    idxsubidx = MakeKey(idx, subidx);
                                    name = subs[i].Element("Name").Value;
                                    type = subs[i].Element("Type").Value;
                                    bit_size = Convert.ToInt32(subs[i].Element("BitSize").Value);
                                    access = subs[i].Element("Flags").Element("Access").Value.ToUpper();
                                    dictOfCoE.Add(idxsubidx, new DictItem() { Index = idx, Subindex = subidx, IdxWithName = _idx_with_name, Name = name, NumericType = NUM_TYPE.DEC, Type = type, Access = access, Length = bit_size / 8, Signed = (type[0] != 'U'), ObjType = "sub_obj", DictStruct = _dict_struct, ValueDisplay = "" });
                                    listOfCoE.Add(dictOfCoE[idxsubidx]);
                                }
                                else
                                {
                                    match = m.Groups[0].Value;
                                    access = subs[i].Element("Flags").Element("Access").Value.ToUpper();
                                }
                            }
                        }
                        else if (match == dt_name)
                        {
                            List<XElement> subs = obj.Element("Info").Elements("SubItem").ToList();
                            int num_elem = Convert.ToInt32(dt.Element("ArrayInfo").Element("Elements").Value);

                            for (byte i = 1; i <= num_elem; i++)
                            {
                                subidx = i;
                                idxsubidx = MakeKey(idx, subidx);
                                name = subs[i].Element("Name").Value;
                                type = dt.Element("BaseType").Value;
                                bit_size = Convert.ToInt32(dt.Element("BitSize").Value) / num_elem;
                                dictOfCoE.Add(idxsubidx, new DictItem() { Index = idx, Subindex = subidx, IdxWithName = _idx_with_name, Name = name, NumericType = NUM_TYPE.DEC, Type = type, Access = access, Length = bit_size / 8, Signed = (type[0] != 'U'), ObjType = "sub_obj", DictStruct = _dict_struct, ValueDisplay = "" });
                                listOfCoE.Add(dictOfCoE[idxsubidx]);
                            }
                            break;
                        }
                    }
                }
            }
        }

        /* builds a very simplified dictionary that contains the labels used in combo boxes, for the scope for instance */
        private void BuildSimpleDict()
        {
            bool flag = false;
            
            foreach (string key in dictOfCoE.Keys)
            {
                ushort idx = 0;
                switch (dictOfCoE[key].ObjType)
                {
                    case "one_obj":
                        idx = this.dictOfCoE[key].Index;
                        if (idx >= 0x2000 & idx <= 0x3000 | idx >= 0x6000 & idx <= 0x7000)
                        {
                            simpleDict.Add(key, key + "    " + dictOfCoE[key].Name);
                        }
                        break;
                    case "header_obj":
                        if (flag)
                        {
                            flag = false;
                        }
                        flag = true;
                        break;
                    case "sub_obj":
                        idx = dictOfCoE[key].Index;
                        if (idx >= 0x2000 & idx <= 0x3000 | idx >= 0x6000 & idx <= 0x7000)
                        {
                            simpleDict.Add(key, key + "    " + dictOfCoE[key].Name);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Gets an item from the object dictionary with the index and subindex
        /// </summary>
        /// <param name="index">Index of the object</param>
        /// <param name="subindex">Subindex of the object</param>
        /// <returns>Item from the object dictionary</returns>
        public DictItem GetItem(ushort index, byte subindex)
        {
            return dictOfCoE[MakeKey(index, subindex)];
        }

        /// <summary>
        /// Makes the Keys used in the object dictionary which is made if a string with
        /// the indey and subindex of an object with the form "XXXX : XX"
        /// </summary>
        /// <param name="idx">Index of the object</param>
        /// <param name="subidx">Subindex of the object</param>
        /// <returns>string with the format of the OD key</returns>
        public static string MakeKey(ushort idx, byte subidx)
        {
            return string.Format("{0:X4} : {1:X2}", idx, subidx).ToUpper();
        }

        /// <summary>
        /// Formats the Display string of the given dictionary item 
        /// depending if if it is decimal, binary or hexadecimal; or if it is a string
        /// </summary>
        /// <param name="item">item of type DictItem that needs the Display value to be formated</param>
        public static void FormatDisplayString(DictItem item)
        {
            if (item.Type == "STRING")
            {
                var len = (item.Value as byte[]).TakeWhile(b => b != 0).Count();
                item.ValueDisplay = Encoding.ASCII.GetString(item.Value as byte[], 0, len);
                return;
            }

            if (item.NumericType == NUM_TYPE.DEC)
            {
                item.ValueDisplay = string.Format("{0:N0}", item.Value, CultureInfo.CurrentCulture);
            }
            else if (item.NumericType == NUM_TYPE.HEX)
            {
                switch (item.Type)
                {
                    case "SINT":
                        item.ValueDisplay = string.Format("{0:X2}", (sbyte)item.Value);
                        break;
                    case "USINT":
                        item.ValueDisplay = string.Format("{0:X2}", (byte)item.Value);
                        break;
                    case "INT":
                        item.ValueDisplay = string.Format("{0:X2} {1:X2}", (byte)((short)item.Value >> 8), (byte)((short)item.Value >> 0));
                        break;
                    case "UINT":
                        item.ValueDisplay = string.Format("{0:X2} {1:X2}", (byte)((ushort)item.Value >> 8), (byte)((ushort)item.Value >> 0));
                        break;
                    case "DINT":
                        item.ValueDisplay = string.Format("{0:X2} {1:X2} {2:X2} {3:X2}", (byte)((int)item.Value >> 24), (byte)((int)item.Value >> 16), (byte)((int)item.Value >> 8), (byte)((int)item.Value >> 0));
                        break;
                    case "UDINT":
                        item.ValueDisplay = string.Format("{0:X2} {1:X2} {2:X2} {3:X2}", (byte)((uint)item.Value >> 24), (byte)((uint)item.Value >> 16), (byte)((uint)item.Value >> 8), (byte)((uint)item.Value >> 0));
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (item.NumericType == NUM_TYPE.BIN)
            {
                switch (item.Type)
                {
                    case "SINT":
                        item.ValueDisplay = Regex.Replace(Convert.ToString(Convert.ToSByte(item.Value), 2).PadLeft(8, '0'), @".{4}", @"$0 ");
                        break;
                    case "USINT":
                        item.ValueDisplay = Regex.Replace(Convert.ToString(Convert.ToByte(item.Value), 2).PadLeft(8, '0'), @".{4}", @"$0 ");
                        break;
                    case "INT":
                        item.ValueDisplay = Regex.Replace(Convert.ToString((short)item.Value, 2).PadLeft(16, '0'), @".{4}", @"$0 ");
                        break;
                    case "UINT":
                        item.ValueDisplay = Regex.Replace(Convert.ToString((ushort)item.Value, 2).PadLeft(16, '0'), @".{4}", @"$0 ");
                        break;
                    case "DINT":
                        item.ValueDisplay = Regex.Replace(Convert.ToString((int)item.Value, 2).PadLeft(32, '0'), @".{4}", @"$0 ");
                        break;
                    case "UDINT":
                        item.ValueDisplay = Regex.Replace(Convert.ToString((uint)item.Value, 2).PadLeft(32, '0'), @".{4}", @"$0 ");
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

        }

        public static void SetValueFromDisplayString(DictItem item)
        {
            if (item.Type == "STRING")
            {
                item.Value = Encoding.ASCII.GetBytes(item.ValueDisplay);
                return;
            }

            if (item.NumericType == NUM_TYPE.DEC)
            {
                var styles = NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign;
                switch (item.Type)
                {
                    case "SINT":
                        item.Value = sbyte.Parse(item.ValueDisplay, styles, CultureInfo.CurrentCulture);
                        break;
                    case "USINT":
                        item.Value = byte.Parse(item.ValueDisplay, styles, CultureInfo.CurrentCulture);
                        break;
                    case "INT":
                        item.Value = short.Parse(item.ValueDisplay, styles, CultureInfo.CurrentCulture);
                        break;
                    case "UINT":
                        item.Value = ushort.Parse(item.ValueDisplay, styles, CultureInfo.CurrentCulture);
                        break;
                    case "DINT":
                        item.Value = int.Parse(item.ValueDisplay, styles, CultureInfo.CurrentCulture);
                        break;
                    case "UDINT":
                        item.Value = uint.Parse(item.ValueDisplay, styles, CultureInfo.CurrentCulture);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (item.NumericType == NUM_TYPE.HEX)
            {
                switch (item.Type)
                {
                    case "SINT":
                        item.Value = sbyte.Parse(Regex.Replace(item.ValueDisplay, @"\s+", ""), NumberStyles.HexNumber);
                        break;
                    case "USINT":
                        item.Value = byte.Parse(Regex.Replace(item.ValueDisplay, @"\s+", ""), NumberStyles.HexNumber);
                        break;
                    case "INT":
                        item.Value = short.Parse(Regex.Replace(item.ValueDisplay, @"\s+", ""), NumberStyles.HexNumber);
                        break;
                    case "UINT":
                        item.Value = ushort.Parse(Regex.Replace(item.ValueDisplay, @"\s+", ""), NumberStyles.HexNumber);
                        break;
                    case "DINT":
                        item.Value = int.Parse(Regex.Replace(item.ValueDisplay, @"\s+", ""), NumberStyles.HexNumber);
                        break;
                    case "UDINT":
                        item.Value = uint.Parse(Regex.Replace(item.ValueDisplay, @"\s+", ""), NumberStyles.HexNumber);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (item.NumericType == NUM_TYPE.BIN)
            {
                switch (item.Type)
                {
                    case "SINT":
                        item.Value = Convert.ToSByte(Regex.Replace(item.ValueDisplay, @"\s+", ""), 2);
                        break;
                    case "USINT":
                        item.Value = Convert.ToByte(Regex.Replace(item.ValueDisplay, @"\s+", ""), 2);
                        break;
                    case "INT":
                        item.Value = Convert.ToInt16(Regex.Replace(item.ValueDisplay, @"\s+", ""), 2);
                        break;
                    case "UINT":
                        item.Value = Convert.ToUInt16(Regex.Replace(item.ValueDisplay, @"\s+", ""), 2);
                        break;
                    case "DINT":
                        item.Value = Convert.ToInt32(Regex.Replace(item.ValueDisplay, @"\s+", ""), 2);
                        break;
                    case "UDINT":
                        item.Value = Convert.ToUInt32(Regex.Replace(item.ValueDisplay, @"\s+", ""), 2);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

        }

        /// <summary>
        /// Changes the format of the displayed values between decimal and hexadecimal only if they have been changed already once
        /// </summary>
        public static void ChangeDecHexDisplayValuesAllSelectedItems(DataGrid dataGridDictionary)
        {
            foreach (DictItem item in dataGridDictionary.SelectedItems)
            {
                ChangeDecHexDisplayValuesSingleItem(item);
            }
        }

        /// <summary>
        /// Changes the numeric type of an object and then calls a function to Format the Display String to correspond to the numeric Type
        /// </summary>
        public static void ChangeDecHexDisplayValuesSingleItem(DictItem item)
        {
            if (item.NumericType == NUM_TYPE.DEC)
                item.NumericType = NUM_TYPE.HEX;
            else if (item.NumericType == NUM_TYPE.HEX)
                item.NumericType = NUM_TYPE.BIN;
            else if (item.NumericType == NUM_TYPE.BIN)
                item.NumericType = NUM_TYPE.DEC;

            if (item.ValueDisplay != "" && item.Type.Substring(0, 3) != "STR")
            {
                FormatDisplayString(item);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                //if (disposing)
                //{
                //}
            }
            _disposed = true;
            GC.SuppressFinalize(this);
        }


    }
    
    
}