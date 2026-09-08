using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
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
using ONYX_DataType;
using static MonitorFactoryTool.MainWindow;
using static MonitorFactoryTool.Pages.Factory;
using MReplyP = ONYX_DataType.MonitorReplyPackage;
using System.Windows.Markup;
using MonitorFactoryTool.Pages;

namespace MonitorFactoryTool.FactoryGrid
{
    /// <summary>
    /// FactoryGridStyle1.xaml 的互動邏輯
    /// </summary>
    public partial class FactoryGridStyle1 : BaseGridStyle
    {
        private string DESCRIPTION;
        private string TYPE;
        private string RLENGTH;
        //public FactoryGridStyle1(string name, string type, string vccode, string description, string length, string rlength)
        //{
        //    InitializeComponent();
        //    label.Content = name;
        //    var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        //    var selectedItem = mainWindow.comboCommand.SelectedItem as Item;

        //    if (selectedItem != null)
        //    {
        //        // Save each item's data value
        //        // If name is Gamma, the data value is below :
        //        // data0 : 00, data1: 12, data2: 14, data3: 16, data4: 18, data5: 20
        //        var comboBoxData = new Dictionary<string, List<string>>();
        //        comboBoxData[selectedItem.Name] = selectedItem.DataValues;
        //        foreach (var kvp in comboBoxData)
        //        {
        //            // kvp.Key 是 name，kvp.Value 是對應的 dataValues
        //            LoadDataValuesIntoComboBox(kvp.Key, kvp.Value);
        //        }
        //        VCCODE = Convert.ToByte(vccode, 16);
        //        DESCRIPTION = description;
        //        TYPE = type;
        //        LENGTH = Convert.ToByte(length, 16);
        //        RLENGTH = rlength;
        //    };
        //}
        public FactoryGridStyle1(string name, string type, string vccode, string description, string length, string rlength, List<string> dataValues)
        {
            InitializeComponent();
            label.Content = name;

            // 直接使用傳入的 dataValues，不再依賴 MainWindow
            if (dataValues != null && dataValues.Count > 0)
            {
                LoadDataValuesIntoComboBox(name, dataValues);
            }

            VCCODE = Convert.ToByte(vccode, 16);
            DESCRIPTION = description;
            TYPE = type;
            LENGTH = Convert.ToByte(length, 16);
            RLENGTH = rlength;
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

        // Link to Factory Page
        private Factory factoryPage;


        private void LoadDataValuesIntoComboBox(string name, List<string> dataValues)
        {
            combobox.Items.Clear();
            foreach (var data in dataValues)
            {
                combobox.Items.Add(new ComboBoxItem { Content = data, Name = $"item{data}" });
            }

            if (combobox.Items.Count > 0)
            {
                combobox.SelectedIndex = 0;
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
        public override byte[] SetCommandByteArray()
        {
            byte Data = Convert.ToByte(combobox.Text, 16);
            OPCODE = (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND;
            byte[] SetCommandArray = { DEST_ADDRESS, SOURCE_ADDRESS, LENGTH, FACTORY_MODE, OPCODE, 0x07, VCCODE, 0x00, Data };

            if ((bool)Factory.isMCCSmode == true)
            {
                Console.WriteLine("MCCS mode: " + Factory.isMCCSmode);


                byte MCCS_VCCODE;
                switch (VCCODE)
                {
                    case 0x07:
                        MCCS_VCCODE = 0x14;
                        switch (Data)
                        {
                            case 0x41:               //ONYX 6500k:41:
                                Data = 0x05;         //MCCS 6500k:05
                                SetCommandArray = new byte[] { DEST_ADDRESS, SOURCE_ADDRESS, 0x84, 0x03, MCCS_VCCODE, 0x00, Data };
                                break;

                            case 0x4B:               //ONYX 7500k:4B
                                Data = 0x06;         //MCCS 7500k:06
                                SetCommandArray = new byte[] { DEST_ADDRESS, SOURCE_ADDRESS, 0x84, 0x03, MCCS_VCCODE, 0x00, Data };
                                break;

                            case 0x5D:               //ONYX 9300k:5D
                                Data = 0x08;         //MCCS 9300k:08
                                SetCommandArray = new byte[] { DEST_ADDRESS, SOURCE_ADDRESS, 0x84, 0x03, MCCS_VCCODE, 0x00, Data };
                                break;
                        }
                        break;
                }
            }
            return SetCommandArray;
        }
        public override byte[] GetCommandByteArray()
        {
            //byte Data = Convert.ToByte(combobox.Text, 16);
            byte Data = 0x07;
            OPCODE = (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND;
            byte[] GetCommandArray = { DEST_ADDRESS, SOURCE_ADDRESS, LENGTH, FACTORY_MODE, OPCODE, 0x07, VCCODE, 0x00, 0x00 };

            //byte[] GetCommandArray = new byte[] { DEST_ADDRESS, SOURCE_ADDRESS, 0x82, 0x01, Data };
            Console.WriteLine("Data=" + Data);

            if ((bool)Factory.isMCCSmode == true)
            {
                byte MCCS_VCCODE;
                switch (VCCODE)
                {
                    case 0x07:                   //ONYX Color Temp:07
                        MCCS_VCCODE = 0x14;      //MCCS Color Temp:14
                        GetCommandArray = new byte[] { DEST_ADDRESS, SOURCE_ADDRESS, 0x82, 0x01, MCCS_VCCODE }; ///01 Set VCP Feature COMMAND
                        break;


                    //case 0x41:
                    //    Data = 0x05;
                    //    GetCommandArray = new byte[] { DEST_ADDRESS, SOURCE_ADDRESS, 0x82, 0x01, MCCS_VCCODE }; ///01 Set VCP Feature COMMAND
                    //    break;

                    //case 0x4B:
                    //    Data = 0x06;
                    //    GetCommandArray = new byte[] { DEST_ADDRESS, SOURCE_ADDRESS, 0x82, 0x01, MCCS_VCCODE }; ///01 Set VCP Feature COMMAND
                    //    break;

                    //case 0x5D:               //ONYX 9300k:5D
                    //    Data = 0x08;         //MCCS 9300k:08
                    //    GetCommandArray = new byte[] { DEST_ADDRESS, SOURCE_ADDRESS, 0x82, 0x01, MCCS_VCCODE }; ///01 Set VCP Feature COMMAND
                    //    break;
                }

            }




            return GetCommandArray;
        }
        public override byte[] LoopCommandByteArray()
        {
            byte Data = Convert.ToByte(combobox.Text, 16);
            byte[] SetCommandArray = { DEST_ADDRESS, SOURCE_ADDRESS, LENGTH, FACTORY_MODE, OPCODE, 0x07, VCCODE, 0x00, Data };
            return SetCommandArray;
        }

        public override void getReplyAndUpdateUI(byte[] commandByteArr)
        {
            if((bool)Factory.isMCCSmode == true)
            {

            }
            else
            {
                MReplyP monitorReplyPackage = new MReplyP(commandByteArr);
                if (monitorReplyPackage.OPCode == OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND && monitorReplyPackage.Status == Status.STATUS_SUCCESS)
                {
                    var comboBoxItem = combobox.Items.OfType<ComboBoxItem>().FirstOrDefault(x => x.Content.ToString() == monitorReplyPackage.Data[0].ToString("X2"));
                    int index = combobox.Items.IndexOf(comboBoxItem);
                    combobox.SelectedIndex = index;
                }
            }
            
        }

        //260224 Wu add for loading script
        public override string GetCurrentSetting()
        {
            return combobox.Text;
        }

        public override void RestoreSetting(string setting)
        {
            if (!string.IsNullOrEmpty(setting))
            {
                foreach (ComboBoxItem item in combobox.Items)
                {
                    if (item.Content.ToString() == setting)
                    {
                        combobox.SelectedItem = item;
                        break;
                    }
                }
            }
        }
    }
}
