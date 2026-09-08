using MonitorFactoryTool.Pages;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static MonitorFactoryTool.FactoryGrid.FactoryGridStyle3;
using static System.Net.Mime.MediaTypeNames;
using MReplyP = ONYX_DataType.MonitorReplyPackage;

namespace MonitorFactoryTool.FactoryGrid
{
    /// <summary>
    /// FactoryGridStyle4.xaml 的互動邏輯
    /// </summary>
    public partial class FactoryGridStyle4 : BaseGridStyle
    {
        private string TYPE;
        private string DESCRIPTION;
        private string RLENGTH;
        //public FactoryGridStyle4(string gridName, string type, string vccode, string description, string length, string rlength)
        //{
        //    InitializeComponent();
        //    label.Content = gridName;
        //    textbox.Text = "NA";
        //    VCCODE = Convert.ToByte(vccode, 16);
        //    DESCRIPTION = description;
        //    TYPE = type;
        //    LENGTH = Convert.ToByte(length, 16);
        //    RLENGTH = rlength;
        //}
        public FactoryGridStyle4(string gridName, string type, string vccode, string description, string length, string rlength, List<string> dataValues)
        {
            InitializeComponent();
            label.Content = gridName;
            textbox.Text = "NA";
            VCCODE = Convert.ToByte(vccode, 16);
            DESCRIPTION = description;
            TYPE = type;
            LENGTH = Convert.ToByte(length, 16);
            RLENGTH = rlength;
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
            return Convert.ToInt16(RLENGTH);
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
        private const byte OPCODE = (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND;
        private byte VCCODE;

        public override byte[] GetCommandByteArray()
        {
            byte[] GetCommandArray = { DEST_ADDRESS, SOURCE_ADDRESS, LENGTH, FACTORY_MODE, OPCODE, 0x07, VCCODE, 0x00, 0x00};
            return GetCommandArray;
        }
        public override byte[] LoopCommandByteArray()
        {
            byte[] GetCommandArray = { DEST_ADDRESS, SOURCE_ADDRESS, LENGTH, FACTORY_MODE, OPCODE, 0x07, VCCODE, 0x00, 0x00};
            return GetCommandArray;
        }
        public override void getReplyAndUpdateUI(byte[] commandByteArr)
        {     
            if((bool)Factory.isMCCSmode == true)
            {

            }
            else
            {
                // Put reply to package 
                MReplyP monitorReplyPackage = new MReplyP(commandByteArr);
                if (monitorReplyPackage.OPCode == OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND && monitorReplyPackage.Status == Status.STATUS_SUCCESS)
                {
                    switch (monitorReplyPackage.VCCode)
                    {
                        case VCCode.ONYX_FCODE_12_GET_SERIAL_NUMBER:
                            textbox.Text = System.Text.Encoding.ASCII.GetString(monitorReplyPackage.Data);
                            break;
                        case VCCode.ONYX_FCODE_13_GET_FIRMWARE_VERSION:
                            textbox.Text = System.Text.Encoding.ASCII.GetString(monitorReplyPackage.Data);
                            break;
                        case VCCode.ONYX_FCODE_14_GET_TEMPERATURE:
                            Debug.WriteLine("Temperature data 0 is " + Convert.ToInt16(monitorReplyPackage.Data[0].ToString("X2"), 16).ToString());
                            Debug.WriteLine("Temperature data 1 is " + Convert.ToInt16(monitorReplyPackage.Data[1].ToString("X2"), 16).ToString());
                            double temperature;
                            if ((monitorReplyPackage.Data[0] >> 7) == 0x0)
                            {
                                temperature = (double)((int)(monitorReplyPackage.Data[0] << 8 | monitorReplyPackage.Data[1])) / 128.000;
                            }
                            else
                            {
                                temperature = (double)((int)(monitorReplyPackage.Data[0] << 8 | monitorReplyPackage.Data[1]) - 65536) / 128.000;
                            }
                            
                            Debug.WriteLine("Temperature is " + temperature.ToString());
                            textbox.Text = temperature.ToString();
                            break;
                    }
                    
                }
            }
            
        }
    }
}
