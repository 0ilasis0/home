using ONYX_DataType;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
using System.Xml.Linq;
using static MonitorFactoryTool.FactoryGrid.FactoryGridStyle3;
using static MonitorFactoryTool.MainWindow;
using static MonitorFactoryTool.Pages.Factory;
using MReplyP = ONYX_DataType.MonitorReplyPackage;
using FDTI_Factory_i2c;
using static FDTI_Factory_i2c.I2CController;
using MonitorFactoryTool.Pages;


namespace MonitorFactoryTool.FactoryGrid
{
    public partial class FactoryGridStyle2 : BaseGridStyle
    {
        private uint minValue;
        private uint maxValue;
        private string TYPE;
        private string DESCRIPTION;
        private string RLENGTH;

        //240915 for I2C
        private I2CController i2cController => I2CController.Instance; ///

        //public FactoryGridStyle2(string gridName, string type, string vccode, string description, string length, string rlength)
        //{
        //    InitializeComponent();
        //    label.Content = gridName;

        //    var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        //    var selectedItem = mainWindow.comboCommand.SelectedItem as Item;

        //    if (selectedItem != null)
        //    {
        //        // Save each item's data value
        //        // If name is Audio Volume, the data value is below :
        //        // data0 : 00, data1: 64
        //        var comboBoxData = new Dictionary<string, List<string>>();
        //        comboBoxData[selectedItem.Name] = selectedItem.DataValues;
        //        foreach (var kvp in comboBoxData)
        //        {
        //            // kvp.Key 是 name，kvp.Value 是對應的 dataValues
        //            LoadDataValuesIntoTextBox(kvp.Key, kvp.Value);
        //        }
        //        VCCODE = Convert.ToByte(vccode, 16);
        //        DESCRIPTION = description;
        //        TYPE = type;
        //        LENGTH = Convert.ToByte(length, 16);
        //        RLENGTH = rlength;
        //    };
        //}

        public FactoryGridStyle2(string gridName, string type, string vccode, string description, string length, string rlength, List<string> dataValues)
        {
            InitializeComponent();
            label.Content = gridName;

            // 直接使用傳入的 dataValues，不再依賴 MainWindow
            if (dataValues != null && dataValues.Count >= 2)
            {
                LoadDataValuesIntoTextBox(gridName, dataValues);
            }

            VCCODE = Convert.ToByte(vccode, 16);
            DESCRIPTION = description;
            TYPE = type;
            LENGTH = Convert.ToByte(length, 16);
            RLENGTH = rlength;
        }

        private void buttonDown_Click(object sender, RoutedEventArgs e)
        {
            int textBoxValueDown;
            bool isNumber = int.TryParse(textbox.Text, out textBoxValueDown);

            if (isNumber && textBoxValueDown > 0 && textBoxValueDown <= maxValue)
            {
                textBoxValueDown--;
                textbox.Text = textBoxValueDown.ToString();
            }
        }
        private void LoadDataValuesIntoTextBox(string name, List<string> dataValues)
        {
            minValue = Convert.ToUInt16(dataValues[0], 16);
            maxValue = Convert.ToUInt16(dataValues[1], 16);
            textbox.Text = maxValue.ToString();
        }

        private void buttonUp_Click(object sender, RoutedEventArgs e)
        {
            int textBoxValueUp;
            bool isNumber = int.TryParse(textbox.Text, out textBoxValueUp);

            if (isNumber && textBoxValueUp >= 0 && textBoxValueUp < maxValue)
            {
                textBoxValueUp++;
                textbox.Text = textBoxValueUp.ToString();
            }
        }
        private void textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int caretPosition = textbox.CaretIndex;
            string newText = new string(textbox.Text.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(newText))
            {
                textbox.Text = "0";
                textbox.CaretIndex = 1;
                return;
            }

            if (int.TryParse(newText, out int tempValue))
            {
                tempValue = Math.Max(Convert.ToInt32(minValue), Math.Min(tempValue, Convert.ToInt32(maxValue)));
                textbox.Text = tempValue.ToString();
            }
            else
            {
                textbox.Text = "0";
            }

            textbox.CaretIndex = Math.Min(caretPosition, textbox.Text.Length);
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

        // Link to Factory Page
        private Factory factoryPage;

        private string data_class; //記錄textbox 數值

        public override byte[] SetCommandByteArray()
        {
            byte Data = Convert.ToByte(int.Parse(textbox.Text));
            OPCODE = (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND;

            byte[] SetCommandArray = { DEST_ADDRESS, SOURCE_ADDRESS, LENGTH, FACTORY_MODE, OPCODE, 0x07, VCCODE, 0x00, Data };

            if ((bool)Factory.isMCCSmode == true)
            {
                byte MCCS_VCCODE;
                switch (VCCODE)
                {
                    case 0x0A:
                        MCCS_VCCODE = 0x10;   /////0x10 :Brightness
                        SetCommandArray = new byte[] { DEST_ADDRESS, SOURCE_ADDRESS, 0x84, 0x03, MCCS_VCCODE, 0x00, Data }; ///03 Set VCP Feature COMMAND
                        break;

                    case 0x0B:
                        MCCS_VCCODE = 0x12;  /////0x12 :Contrast
                        SetCommandArray = new byte[] { DEST_ADDRESS, SOURCE_ADDRESS, 0x84, 0x03, MCCS_VCCODE, 0x00, Data };
                        break;
                }
            }
            return SetCommandArray;
        }




        public override byte[] GetCommandByteArray()
        {
            byte Data = Convert.ToByte(int.Parse(textbox.Text));
            OPCODE = (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND;

            byte[] GetCommandArray = { DEST_ADDRESS, SOURCE_ADDRESS, LENGTH, FACTORY_MODE, OPCODE, 0x07, VCCODE, 0x00, 0x00 };

            if ((bool)Factory.isMCCSmode == true)  /////判斷目前是不是有勾選MCCS Mode
            {
                byte MCCS_VCCODE;
                switch (VCCODE)
                {
                    case 0x0A:                   //ONYX Brightness:0A
                        MCCS_VCCODE = 0x10;      //MCCS Brightness:10   ////測試時先轉成MCCS Mode 指令
                        GetCommandArray = new byte[] { DEST_ADDRESS, SOURCE_ADDRESS, 0x82, 0x01, MCCS_VCCODE }; ///01 Set VCP Feature COMMAND
                        break;

                    case 0x0B:                     //ONYX Contrast:0B
                        MCCS_VCCODE = 0x12;        //MCCS Contrast:12
                        GetCommandArray = new byte[] { DEST_ADDRESS, SOURCE_ADDRESS, 0x82, 0x01, MCCS_VCCODE };
                        break;
                }
            }
            return GetCommandArray;
        }


        public override byte[] LoopCommandByteArray()
        {
            byte Data = Convert.ToByte(int.Parse(textbox.Text));
            byte[] SetCommandArray = { DEST_ADDRESS, SOURCE_ADDRESS, LENGTH, FACTORY_MODE, OPCODE, 0x07, VCCODE, 0x00, Data };

            if ((bool)Factory.isMCCSmode == true)
            {
                byte MCCS_VCCODE;
                switch (VCCODE)
                {
                    case 0x0A:
                        MCCS_VCCODE = 0x10;      /////brightness
                        SetCommandArray = new byte[] { DEST_ADDRESS, SOURCE_ADDRESS, 0x84, 0x03, MCCS_VCCODE, 0x00, Data }; ///03 Set VCP Feature COMMAND
                        break;

                    case 0x0B:
                        MCCS_VCCODE = 0x12;       /////contrast
                        //SetCommandArray = new byte[] { DEST_ADDRESS, SOURCE_ADDRESS, 0x84, 0x03, MCCS_VCCODE, 0x00, Data };

                        ////下面這行實際是MCCS Get Command 的形式，目前因測試先將Contrast  設定成 Get Command 的形式，只是變數名稱用SetCommandArray
                        SetCommandArray = new byte[] { DEST_ADDRESS, SOURCE_ADDRESS, 0x82, 0x01, MCCS_VCCODE };
                        break;
                }
            }
            return SetCommandArray;
        }



        public override void getReplyAndUpdateUI(byte[] commandByteArr)
        {
            if (commandByteArr == null)
            {
                Console.WriteLine("commandByteArr 是null!");
            }

            if ((bool)Factory.isMCCSmode == true)
            {
                ///////以下都是MCCS標準///////

                if (commandByteArr != null)
                {
                    string hexString = BitConverter.ToString(commandByteArr);
                    Console.WriteLine("回傳的commandByteArr: " + hexString);
                }

                if (commandByteArr != null && commandByteArr.Length >= 2)
                {
                    try
                    {
                        data_class = Convert.ToInt16(commandByteArr[commandByteArr.Length - 2].ToString("X2"), 16).ToString();
                    }
                    catch (Exception ex)
                    {
                        // 記錄錯誤用
                        Console.WriteLine($"Error processing commandByteArr: {ex.Message}");
                    }
                }
                else
                {
                    // 記錄陣列長度不足的情況
                    Console.WriteLine("commandByteArr is null or has insufficient length");
                }
                Console.WriteLine("MCCS的 reply 數值是: " + data_class);
                textbox.Text = data_class;              

            }
            else
            {
                MReplyP monitorReplyPackage = new MReplyP(commandByteArr);

                Console.WriteLine("monitorReplyPackage" + monitorReplyPackage.ToString());

                if (monitorReplyPackage.OPCode == OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND && monitorReplyPackage.Status == Status.STATUS_SUCCESS)
                {
                    
                    if ((byte)monitorReplyPackage.VCCode == this.VCCODE)
                    {
                        textbox.Text = Convert.ToInt16(monitorReplyPackage.Data[0].ToString("X2"), 16).ToString();
                    }

                }
                Console.WriteLine("getReply: " + Convert.ToInt16(monitorReplyPackage.Data[0].ToString("X2"), 16).ToString());
            }
        }

        //260224 Wu add for loading script
        public override string GetCurrentSetting()
        {
            return textbox.Text;
        }

        public override void RestoreSetting(string setting)
        {
            if (!string.IsNullOrEmpty(setting))
            {
                textbox.Text = setting;
            }
        }
    }
}
