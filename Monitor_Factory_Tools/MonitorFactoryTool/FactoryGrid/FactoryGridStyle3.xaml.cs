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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ONYX_DataType;
using System.Text.RegularExpressions;
using static MonitorFactoryTool.MainWindow;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using MReplyP = ONYX_DataType.MonitorReplyPackage;
using MonitorFactoryTool.Pages;

namespace MonitorFactoryTool.FactoryGrid
{
    /// <summary>
    /// FactoryGridStyle3.xaml 的互動邏輯
    /// </summary>
    public partial class FactoryGridStyle3 : BaseGridStyle
    {
        private string TYPE;
        private string DESCRIPTION;
        private string RLENGTH;
        List<commandProperty> commandProperties;
        private uint colorMaxValue;
        //public FactoryGridStyle3(string gridName, string type, string vccode, string description, string length, string rlength)
        //{
        //    InitializeComponent();
        //    label.Content = gridName;
        //    textbox.Text = "255";
        //    // Item is selected from Command List
        //    var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        //    var selectedItem = mainWindow.comboCommand.SelectedItem as Item;

        //    if (selectedItem != null)
        //    {
        //        // Save each item's data value
        //        // If name is Gamma, the data value is below :
        //        // data0 : 00, data1: FF, data2: 01, data3: FF, data4: 02, data5: FF
        //        var comboBoxData = new Dictionary<string, List<string>>();
        //        comboBoxData[selectedItem.Name] = selectedItem.DataValues;
        //        foreach (var kvp in comboBoxData)
        //        {
        //            // kvp.Key 是 name，kvp.Value 是對應的 dataValues
        //            LoadDataValuesIntoComboBox(kvp.Key, kvp.Value);
        //        }
        //        VCCODE =  Convert.ToByte(vccode, 16);
        //        DESCRIPTION = description;
        //        TYPE = type;
        //        LENGTH = Convert.ToByte(length, 16);
        //        RLENGTH = rlength;
        //    };
        //}
        public FactoryGridStyle3(string gridName, string type, string vccode, string description, string length, string rlength, List<string> dataValues)
        {
            InitializeComponent();
            label.Content = gridName;
            textbox.Text = "255";

            // 直接使用傳入的 dataValues，不再依賴 MainWindow
            if (dataValues != null && dataValues.Count > 0)
            {
                LoadDataValuesIntoComboBox(gridName, dataValues);
            }

            VCCODE = Convert.ToByte(vccode, 16);
            DESCRIPTION = description;
            TYPE = type;
            LENGTH = Convert.ToByte(length, 16);
            RLENGTH = rlength;
        }
        private void LoadDataValuesIntoComboBox(string name, List<string> dataValues)
        {
            combobox.Items.Clear();
            // Create a list to save each color maxvalue
            commandProperties = new List<commandProperty>();

            for (int i = 0; i < dataValues.Count; i += 2)
            {
                //combobox.Items.Add(new ComboBoxItem { Content = dataValues[i] });
                var commandProperty = new commandProperty()
                {
                    color = dataValues[i],
                    maxvalue = dataValues[i + 1],
                    tempvalue = 0
                };
                commandProperties.Add(commandProperty);
            }
            combobox.ItemsSource = commandProperties;
            combobox.DisplayMemberPath = "color";
            combobox.SelectedValuePath = "color";
            //foreach (var data in dataValues)
            //{
            //    combobox.Items.Add(new ComboBoxItem { Content = data });
            //}

            if (combobox.Items.Count > 0)
            {
                combobox.SelectedIndex = 0;
            }
        }

        private void buttonDown_Click(object sender, RoutedEventArgs e)
        {
            int textBoxValueDown;
            bool isNumber = int.TryParse(textbox.Text, out textBoxValueDown);

            if (isNumber && textBoxValueDown > 0)
            {
                textBoxValueDown--;
                textbox.Text = textBoxValueDown.ToString();

            }
        }

        private void buttonUp_Click(object sender, RoutedEventArgs e)
        {
            int textBoxValueUp;
            bool isNumber = int.TryParse(textbox.Text, out textBoxValueUp);

            if (isNumber && textBoxValueUp >= 0 && textBoxValueUp < colorMaxValue)
            {
                textBoxValueUp++;
                textbox.Text = textBoxValueUp.ToString();
            }
        }

        private void combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Item is selected from Command Grid
            var selectedItem = combobox.SelectedItem as commandProperty;
            if (combobox.SelectedItem != null)
            {
                colorMaxValue = Convert.ToUInt16(selectedItem.maxvalue, 16);
                textbox.Text = selectedItem.tempvalue.ToString();
            }
        }

        public class commandProperty
        {
            public string color { get; set; }
            public string maxvalue { get; set; }
            public int tempvalue { get; set; }
        }

        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var selectedItem = combobox.SelectedItem as commandProperty;
            if (textbox.Text == " ")
            {
                textbox.Text = 0.ToString();
            }

            if (combobox.SelectedItem != null)
            {
                try
                {
                    selectedItem.tempvalue = int.Parse(textbox.Text);
                    if (selectedItem.tempvalue > Convert.ToInt16(colorMaxValue))
                    {
                        selectedItem.tempvalue = Convert.ToInt16(colorMaxValue);
                    }
                    else if (selectedItem.tempvalue < 0)
                    {
                        selectedItem.tempvalue = 0;
                    }
                    textbox.Text = selectedItem.tempvalue.ToString();
                }
                catch
                {
                    MessageBox.Show("請輸入數字");
                    textbox.Text = 0.ToString();
                }
            }
        }
        public override int SetRlength()
        {
            return 1;
        }
        public override int GetRlength()
        {
            return Convert.ToInt16(RLENGTH);
        }
        public override int LoopRlength()
        {
            return 1;
        }
        public override string GetType()
        {
            return TYPE;
        }
        public override string GetDescription()
        {
            return DESCRIPTION;
        }
        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                 Command Defination                                         #####################
        //###################################################################################################################################
        //###################################################################################################################################      
        private const byte DEST_ADDRESS = (byte)Destination.ONYX_MONITOR_CLIENT_ADDR; //0x37 is 7 bit address, 0x6E is 8bit address
        private const byte SOURCE_ADDRESS = (byte)Source.ONYX_MONITOR_HOST_ADDR;
        private byte LENGTH;
        private const byte FACTORY_MODE = (byte)FactoryMode.ONYX_FACTORY_CMD_C0;
        private byte OPCODE = (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND;
        private byte VCCODE;
        public override byte[] GetCommandData()
        {
            List<byte> Data = new List<byte>();
            foreach (var item in commandProperties)
            {
                Data.Add(Convert.ToByte(item.color));
                Data.Add((byte)item.tempvalue);
            }
            return Data.ToArray();
        }
        public override byte[] SetCommandByteArray()
        {
            OPCODE = (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND;
            byte[] tempCommandArray = { DEST_ADDRESS, SOURCE_ADDRESS, LENGTH, FACTORY_MODE, OPCODE, 0x07, VCCODE };
            List<byte> Data = new List<byte>();
            foreach (var item in commandProperties)
            {
                Data.Add(Convert.ToByte(item.color));
                Data.Add(Convert.ToByte(item.tempvalue));
            }
            byte[] dataArray = Data.ToArray();

            byte[] SetCommandArray = new byte[tempCommandArray.Length + dataArray.Length];
            tempCommandArray.CopyTo(SetCommandArray, 0);
            dataArray.CopyTo(SetCommandArray, tempCommandArray.Length);
            return SetCommandArray;
        }
        public override byte[] GetCommandByteArray()
        {
            OPCODE = (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND;
            byte[] GetCommandArray = { DEST_ADDRESS, SOURCE_ADDRESS, 0x86, FACTORY_MODE, OPCODE, 0x07, VCCODE, 0x00, 0x00 };
            return GetCommandArray;
        }
        public override byte[] LoopCommandByteArray()
        {
            byte[] tempCommandArray = { DEST_ADDRESS, SOURCE_ADDRESS, LENGTH, FACTORY_MODE, OPCODE, 0x07, VCCODE };
            List<byte> Data = new List<byte>();
            foreach (var item in commandProperties)
            {
                Data.Add(Convert.ToByte(item.color));
                Data.Add(Convert.ToByte(item.tempvalue));
            }
            byte[] dataArray = Data.ToArray();

            byte[] SetCommandArray = new byte[tempCommandArray.Length + dataArray.Length];
            tempCommandArray.CopyTo(SetCommandArray, 0);
            dataArray.CopyTo(SetCommandArray, tempCommandArray.Length);
            return SetCommandArray;
        }
        public override void getReplyAndUpdateUI(byte[] commandByteArr)
        {
            if ((bool)Factory.isMCCSmode == true)
            {

            }
            else
            {
                MReplyP monitorReplyPackage = new MReplyP(commandByteArr);
                if (monitorReplyPackage.OPCode == OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND && monitorReplyPackage.Status == Status.STATUS_SUCCESS)
                {
                    for (int i = 0; i < Convert.ToInt16(RLENGTH); i++)
                    {
                        commandProperties[i].tempvalue = Convert.ToInt16(monitorReplyPackage.Data[i].ToString("X2"), 16);
                    }
                    var selectedItem = combobox.SelectedItem as commandProperty;
                    if (combobox.SelectedItem != null)
                    {
                        textbox.Text = selectedItem.tempvalue.ToString();
                    }
                }
            }
        }

        //260224 Wu add for loading script
        public override string GetCurrentSetting()
        {
            if (commandProperties == null) return "";
            // 將所有顏色的 tempvalue 用逗號串接: 120,255,100
            return string.Join(",", commandProperties.Select(x => x.tempvalue));
        }

        public override void RestoreSetting(string setting)
        {
            if (string.IsNullOrEmpty(setting) || commandProperties == null) return;

            string[] vals = setting.Split(',');
            for (int i = 0; i < vals.Length && i < commandProperties.Count; i++)
            {
                if (int.TryParse(vals[i], out int val))
                {
                    commandProperties[i].tempvalue = val;
                }
            }

            var selectedItem = combobox.SelectedItem as commandProperty;
            if (selectedItem != null)
            {
                textbox.Text = selectedItem.tempvalue.ToString();
            }
        }

    }
}
