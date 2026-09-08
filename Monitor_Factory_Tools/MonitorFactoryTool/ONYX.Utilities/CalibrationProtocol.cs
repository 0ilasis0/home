using OnyxSensor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ONYX.Utilities;
//using OfficeOpenXml.Interfaces.Drawing.Text;
using System.Threading;
using FDTI_Factory_i2c;
using ONYX_DataType;
using System.Diagnostics;
using System.Net.NetworkInformation;
using MReplyP = ONYX_DataType.MonitorReplyPackage;
using MonitorFactoryTool.Pages;
using System.Windows;
using System.Windows.Controls;

namespace MonitorFactoryTool.ONYX.Utilities
{
    public class CalibrationProtocol
    {
        private const int DELAY_TIME = 100;
        private bool getvcp = false;

        private byte m_redGain;
        private byte m_greenGain;
        private byte m_blueGain;
        private I2CController i2cController;

        static byte AppStatus = 0;
        //>>wu add 250707
        public bool UniformitySupported { get; }
        public int EntryBits { get; }
        public int EntryCount { get; }
        public int LutResolutionBits { get; }
        public int LutResolution { get; }

        public sealed class PanelDetailInfo //wu add 250707
        {
            public bool UniformitySupported { get; }
            public int EntryBits { get; }
            public int EntryCount { get; }
            public int LutResolutionBits { get; }
            public int LutResolution { get; }

            public PanelDetailInfo(bool uniformitySupported,
                                   int entryBits, int entryCount,
                                   int lutBits, int lutResolution)
            {
                UniformitySupported = uniformitySupported;
                EntryBits = entryBits;
                EntryCount = entryCount;
                LutResolutionBits = lutBits;
                LutResolution = lutResolution;
            }
        }
        public PanelDetailInfo panelInfo;
        //<<wu add 250707
        //public List<string> SupportColorTemp = new List<string>();  //存Command 回傳支援的CT
        //public List<string> SupportGamma = new List<string>();      //存Command 回傳支援的Gamma 

        public interface IProtocolTrigger
        {
            void Next();
        }
        public List<string> SupportColorTemp { get; } = new List<string>();
        public List<byte> SupportColorTempCodes { get; } = new List<byte>();
        public List<string> SupportGamma { get; } = new List<string>();
        public List<byte> SupportGammaCodes { get; } = new List<byte>();


        private IProtocolTrigger mTrigger; // 可選：事件通知用

        public CalibrationProtocol(I2CController controller)
        {
            this.i2cController = controller;
        }

        public bool SetRedGain(byte gain)
        {
            /////Set RED Command
            byte[] SetREDCommandArray =
                       { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_0F_SET_RGB_GAIN, 0x00, gain };
            byte retval_red, retryCount_red = 0;

            do
            {

                retval_red = i2cController.DDCCI_Send_Command(SetREDCommandArray, null, false);
                Console.WriteLine("RGB Red: " + BitConverter.ToString(SetREDCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_red}, retryCount = {++retryCount_red}");  // Record retry times
            } while (retval_red != 0 && retryCount_red <= 10);
            return true;
        }

        public bool SetGreenGain(byte gain)
        {
            /////Set Green Command
            byte[] SetGreenCommandArray =
                       { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_0F_SET_RGB_GAIN, 0x01, gain };
            byte retval_green, retryCount_green = 0;

            do
            {

                retval_green = i2cController.DDCCI_Send_Command(SetGreenCommandArray, null, false);
                Console.WriteLine("RGB Green: " + BitConverter.ToString(SetGreenCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_green}, retryCount = {++retryCount_green}");  // Record retry times
            } while (retval_green != 0 && retryCount_green <= 10);
            return true;
        }

        public bool SetBlueGain(byte gain)
        {

            /////Set Blue Command
            byte[] SetBlueCommandArray =
                       { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_0F_SET_RGB_GAIN , 0x02, gain };
            byte retval_blue, retryCount_blue = 0;

            do
            {
                retval_blue = i2cController.DDCCI_Send_Command(SetBlueCommandArray, null, false);
                Console.WriteLine("RGB Blue: " + BitConverter.ToString(SetBlueCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_blue}, retryCount = {++retryCount_blue}");  // Record retry times
            } while (retval_blue != 0 && retryCount_blue <= 10);
            return true;
        }

        public async Task SetFactoryRGB(byte rGain, byte gGain, byte bGain, int cct)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;

            byte ctByte = GetColorTempByte(cct);

            i2cController = new I2CController(mainWindow.myFtdiDevice);

            AppStatus = i2cController.I2C_ConfigureMpsse();
            AppStatus = i2cController.DDCCI_Null_Message();// zh 250212

            byte[] SetFactoryRGBGain_RED_CommandArray =
               { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x87,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_65_SPECIAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_06_SET_Factory_RGB_Gain, ctByte, 0x00,rGain };
            byte retval_Set_Factory_RGB_Gain_RED, retryCountSet_Factory_RGB_Gain_RED = 0;

            do
            {
                retval_Set_Factory_RGB_Gain_RED = i2cController.DDCCI_Send_Command(SetFactoryRGBGain_RED_CommandArray, null, false);
                Console.WriteLine("Set_Factory_RGB_Gain Red Command array: " + BitConverter.ToString(SetFactoryRGBGain_RED_CommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_Set_Factory_RGB_Gain_RED}, retryCount = {++retryCountSet_Factory_RGB_Gain_RED}");  // Record retry times
            } while (retval_Set_Factory_RGB_Gain_RED != 0 && retryCountSet_Factory_RGB_Gain_RED <= 10);
            Console.WriteLine("Set_Factory_RGB_Gain_RED Command 已經更新 !!!");
            await Task.Delay(100);


            byte[] SetFactoryRGBGain_GREEN_CommandArray =
  { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x87,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_65_SPECIAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_06_SET_Factory_RGB_Gain, ctByte, 0x01,gGain };
            byte retval_Set_Factory_RGB_Gain_GREEN, retryCountSet_Factory_RGB_Gain_GREEN = 0;

            do
            {
                retval_Set_Factory_RGB_Gain_GREEN = i2cController.DDCCI_Send_Command(SetFactoryRGBGain_GREEN_CommandArray, null, false);
                Console.WriteLine("Set_Factory_RGB_Gain Green Command array: " + BitConverter.ToString(SetFactoryRGBGain_GREEN_CommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_Set_Factory_RGB_Gain_GREEN}, retryCount = {++retryCountSet_Factory_RGB_Gain_GREEN}");  // Record retry times
            } while (retval_Set_Factory_RGB_Gain_GREEN != 0 && retryCountSet_Factory_RGB_Gain_GREEN <= 10);
            Console.WriteLine("Set_Factory_RGB_Gain_GREEN Command 已經更新 !!!");

            await Task.Delay(100);



            byte[] SetFactoryRGBGain_BLUE_CommandArray =
  { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x87,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_65_SPECIAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_06_SET_Factory_RGB_Gain, ctByte, 0x02,bGain };
            byte retval_Set_Factory_RGB_Gain_BLUE, retryCountSet_Factory_RGB_Gain_BLUE = 0;

            do
            {
                retval_Set_Factory_RGB_Gain_BLUE = i2cController.DDCCI_Send_Command(SetFactoryRGBGain_BLUE_CommandArray, null, false);
                Console.WriteLine("Set_Factory_RGB_Gain Command Blue array: " + BitConverter.ToString(SetFactoryRGBGain_BLUE_CommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_Set_Factory_RGB_Gain_BLUE}, retryCount = {++retryCountSet_Factory_RGB_Gain_BLUE}");  // Record retry times
            } while (retval_Set_Factory_RGB_Gain_BLUE != 0 && retryCountSet_Factory_RGB_Gain_BLUE <= 10);
            Console.WriteLine("Set_Factory_RGB_Gain Command_RED 已經更新 !!!");

            Console.WriteLine($"[CalibProcess] RGB Gain 校正完成 ({cct}K)");
        }



        public async Task ResetCTandGamma()
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            i2cController = new I2CController(mainWindow.myFtdiDevice);

            AppStatus = i2cController.I2C_ConfigureMpsse();
            AppStatus = i2cController.DDCCI_Null_Message();// zh 250212
            byte[] SetGammaOffCommandArray =
                       { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_08_SET_GAMMA_DICOM, 0x00, 0x00 };
            byte retval_Set_gamma_off, retryCount_Set_gamma_off = 0;

            do
            {
                retval_Set_gamma_off = i2cController.DDCCI_Send_Command(SetGammaOffCommandArray, null, false);
                Console.WriteLine("Set Gamma Off Command array: " + BitConverter.ToString(SetGammaOffCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_Set_gamma_off}, retryCount = {++retryCount_Set_gamma_off}");  // Record retry times
            } while (retval_Set_gamma_off != 0 && retryCount_Set_gamma_off <= 10);
            Console.WriteLine("Gamma 已經 設為OFF!!!");
            // MessageBox.Show("Gamma 已經 設為OFF!!!");

            await Task.Delay(100);

            byte[] SetColorTempUserCommandArray =
                   { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_07_SET_COLOR_TMEP, 0x00, 0x00 };
            byte retval_Set_CT_User, retryCount_Set__CT_User = 0;

            do
            {
                retval_Set_CT_User = i2cController.DDCCI_Send_Command(SetColorTempUserCommandArray, null, false);
                Console.WriteLine("Set Gamma Off Command array: " + BitConverter.ToString(SetColorTempUserCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_Set_CT_User}, retryCount = {++retryCount_Set__CT_User}");  // Record retry times
            } while (retval_Set_CT_User != 0 && retryCount_Set__CT_User <= 10);
            Console.WriteLine("Color Temp 已經設為USER!!!");
            // MessageBox.Show("Color Temp 已經設為USER,Gamma 已經 設為OFF !!!");

        }



        public async Task ResetCTandRGB128()
        {
            ///做完calibration 後要CT user RGB 128
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            i2cController = new I2CController(mainWindow.myFtdiDevice);

            AppStatus = i2cController.I2C_ConfigureMpsse();
            AppStatus = i2cController.DDCCI_Null_Message();// zh 250212


            await Task.Delay(100);

            byte[] SetColorTempUserCommandArray =
                   { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_07_SET_COLOR_TMEP, 0x00, 0x00 };
            byte retval_Set_CT_User, retryCount_Set__CT_User = 0;

            do
            {
                retval_Set_CT_User = i2cController.DDCCI_Send_Command(SetColorTempUserCommandArray, null, false);
                Console.WriteLine("Set Gamma Off Command array: " + BitConverter.ToString(SetColorTempUserCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_Set_CT_User}, retryCount = {++retryCount_Set__CT_User}");  // Record retry times
            } while (retval_Set_CT_User != 0 && retryCount_Set__CT_User <= 10);
            Console.WriteLine("Color Temp 已經設為USER!!!");
            // MessageBox.Show("Color Temp 已經設為USER,Gamma 已經 設為OFF !!!");

            await Task.Delay(100);
            SetRedGain(128);
            SetGreenGain(128);
            SetBlueGain(128);
            Console.WriteLine("RGB 已經設為USER!!!");
        }





        public async Task GetColoetempDetailAndGammaDetail()
        {
            SupportColorTemp.Clear();
            SupportColorTempCodes.Clear();
            SupportGamma.Clear();
            SupportGammaCodes.Clear();

            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            i2cController = new I2CController(mainWindow.myFtdiDevice);

            AppStatus = i2cController.I2C_ConfigureMpsse();
            AppStatus = i2cController.DDCCI_Null_Message();// zh 250212

            ///Get monitor Color Temp support
            byte[] GetColorTempDetailCommandArray =
                       { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.  ONYX_CMD_75_SPECIAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_07_GET_COLOR_TEMP_DETAIL , 0x00, 0x00 };
            byte retval_Get_ColorTemp_Detail, retryCount_Get_ColorTemp_Detail = 0;

            do
            {
                retval_Get_ColorTemp_Detail = i2cController.DDCCI_Send_Command(GetColorTempDetailCommandArray, getColotTempReplySupportDetail, false);
                Console.WriteLine("Get Color Temp Detail Command array: " + BitConverter.ToString(GetColorTempDetailCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_Get_ColorTemp_Detail}, retryCount = {++retryCount_Get_ColorTemp_Detail}");  // Record retry times
            } while (retval_Get_ColorTemp_Detail != 0 && retryCount_Get_ColorTemp_Detail <= 10);

            await Task.Delay(100);
            ///Get monitor Gamma support
            byte[] GetGammapDetailCommandArray =
                      { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.  ONYX_CMD_75_SPECIAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_08_GET_GAMMA_DETAIL , 0x00, 0x00 };
            byte retval_Get_Gamma_Detail, retryCount_Get_Gamma_Detail = 0;

            do
            {
                retval_Get_Gamma_Detail = i2cController.DDCCI_Send_Command(GetGammapDetailCommandArray, getGammaReplySupportDetail, false);
                Console.WriteLine("Get Gamma Detail Command array: " + BitConverter.ToString(GetGammapDetailCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_Get_Gamma_Detail}, retryCount = {++retryCount_Get_Gamma_Detail}");  // Record retry times
            } while (retval_Get_Gamma_Detail != 0 && retryCount_Get_Gamma_Detail <= 10);
        }
        public void getColotTempReplySupportDetail(byte[] commandByteArr)
        {
            MReplyP monitorReplyPackage = new MReplyP(commandByteArr);
            if (monitorReplyPackage.OPCode == OPCode.ONYX_CMD_75_SPECIAL_GET_COMMAND &&
               monitorReplyPackage.Status == Status.STATUS_SUCCESS &&
               monitorReplyPackage.VCCode == VCCode.ONYX_FCODE_07_GET_COLOR_TEMP_DETAIL)
            {
                string hexString = BitConverter.ToString(monitorReplyPackage.Data).Replace("-", "");
                string ColorTempReply = HexToAscii(hexString);

                string CT1 = monitorReplyPackage.Data[monitorReplyPackage.Data.Length - 3].ToString("X2");
                string CT2 = monitorReplyPackage.Data[monitorReplyPackage.Data.Length - 2].ToString("X2");
                string CT3 = monitorReplyPackage.Data[monitorReplyPackage.Data.Length - 1].ToString("X2");

                Dictionary<string, string> ctMap = new Dictionary<string, string>
        {
            { "36", "5400" },
            { "41", "6500" },
            { "5D", "9300" },
            { "00", "Unsupported" }
        };

                foreach (var ctHex in new[] { CT1, CT2, CT3 })
                {
                    if (ctMap.TryGetValue(ctHex, out var ctText) && ctText != "Unsupported")
                    {
                        SupportColorTemp.Add(ctText);
                        SupportColorTempCodes.Add(Convert.ToByte(ctHex, 16));
                    }
                }

                if (SupportColorTemp.Count > 0)
                {

                    Console.WriteLine("Supported Color Temps: " + string.Join(", ", SupportColorTemp.Select(s => s + "K")));
                }

                else
                    Console.WriteLine("No supported color temperatures (all unsupported or unknown).");
            }
            RefreshListBoxInfo();

        }

        public void getGammaReplySupportDetail(byte[] commandByteArr)
        {
            MReplyP monitorReplyPackage = new MReplyP(commandByteArr);
            if (monitorReplyPackage.OPCode == OPCode.ONYX_CMD_75_SPECIAL_GET_COMMAND &&
               monitorReplyPackage.Status == Status.STATUS_SUCCESS &&
               monitorReplyPackage.VCCode == VCCode.ONYX_FCODE_08_GET_GAMMA_DETAIL)
            {
                string hexString = BitConverter.ToString(monitorReplyPackage.Data).Replace("-", "");
                string GammaReply = HexToAscii(hexString);

                string Gamma1 = monitorReplyPackage.Data[monitorReplyPackage.Data.Length - 5].ToString("X2");
                string Gamma2 = monitorReplyPackage.Data[monitorReplyPackage.Data.Length - 4].ToString("X2");
                string Gamma3 = monitorReplyPackage.Data[monitorReplyPackage.Data.Length - 3].ToString("X2");
                string Gamma4 = monitorReplyPackage.Data[monitorReplyPackage.Data.Length - 2].ToString("X2");
                string Gamma5 = monitorReplyPackage.Data[monitorReplyPackage.Data.Length - 1].ToString("X2");


                Dictionary<string, string> GammaMap = new Dictionary<string, string>
        {
            { "12", "1.8" },
            { "14", "2.0" },
            { "16", "2.2" },
            { "18", "2.4" },
            { "1A", "2.6" },
            { "20", "DICOM" }
        };

                foreach (var gmHex in new[] { Gamma1, Gamma2, Gamma3, Gamma4, Gamma5 })
                {
                    if (GammaMap.TryGetValue(gmHex, out var gmText))
                    {
                        SupportGamma.Add(gmText);
                        SupportGammaCodes.Add(Convert.ToByte(gmHex, 16));
                    }
                }

                if (SupportGamma.Count > 0)
                {
                    Console.WriteLine("Supported Gamma: " + string.Join(", ", SupportGamma));
                }
                else
                    Console.WriteLine("No supported Gammas (all unsupported or unknown).");
            }

            RefreshListBoxInfo();

        }


        /// 只顯示支援的gamma ct 搭配 顯示在blackwindow 中的list s
        private void RefreshListBoxInfo()
        {
            var blackWindow = Application.Current.Windows
                .OfType<BlackWindow>()
                .FirstOrDefault(w => w.IsVisible);
            if (blackWindow == null) return;

            var list = blackWindow.listBoxInfo;
            list.Items.Clear();

            // 固定兩項
            list.Items.Add("Contrast:");
            list.Items.Add("Uniformity:");

            // 支援的 Color Temp
            foreach (var ct in SupportColorTemp)
                list.Items.Add($"Color Temp {ct}K:");

            // 支援的 Gamma × Color Temp
            foreach (var gmText in SupportGamma)
            {
                bool isDicom = string.Equals(gmText, "DICOM", StringComparison.OrdinalIgnoreCase);

                foreach (var ct in SupportColorTemp)
                {
                    if (isDicom)
                        list.Items.Add($"DICOM {ct}K:");
                    else
                        list.Items.Add($"Gamma {gmText} {ct}K:");
                }
            }


        }




        public async Task SetCTonly(byte ctByte)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            i2cController = new I2CController(mainWindow.myFtdiDevice);

            AppStatus = i2cController.I2C_ConfigureMpsse();
            AppStatus = i2cController.DDCCI_Null_Message();// zh 250212


            byte[] SetColorTempUserCommandArray =
                   { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_07_SET_COLOR_TMEP, 0x00, ctByte };
            byte retval_Set_CT_User, retryCount_Set__CT_User = 0;

            do
            {
                retval_Set_CT_User = i2cController.DDCCI_Send_Command(SetColorTempUserCommandArray, null, false);
                Console.WriteLine("Set Gamma Off Command array: " + BitConverter.ToString(SetColorTempUserCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_Set_CT_User}, retryCount = {++retryCount_Set__CT_User}");  // Record retry times
            } while (retval_Set_CT_User != 0 && retryCount_Set__CT_User <= 10);
            Console.WriteLine($"Color Temp 已經設為 {ctByte} !!!");
            await Task.Delay(100);
        }





        public async Task SetBrightnessMax()
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            i2cController = new I2CController(mainWindow.myFtdiDevice);

            AppStatus = i2cController.I2C_ConfigureMpsse();
            AppStatus = i2cController.DDCCI_Null_Message();// zh 250212



            byte[] SetBrightnessMaxCommandArray =
                   { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_0A_SET_BRIGHTNESS, 0x00, 0x64 };
            byte retval_SetBrightnessMax, retryCount_SetBrightnessMa = 0;

            do
            {
                retval_SetBrightnessMax = i2cController.DDCCI_Send_Command(SetBrightnessMaxCommandArray, null, false);
                Console.WriteLine("Set Brightness Max array: " + BitConverter.ToString(SetBrightnessMaxCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_SetBrightnessMax}, retryCount = {++retryCount_SetBrightnessMa}");  // Record retry times
            } while (retval_SetBrightnessMax != 0 && retryCount_SetBrightnessMa <= 10);
            Console.WriteLine($"Brightness 已經設定為Max  !!!");
            await Task.Delay(100);
        }



        public async Task SetGammaOnly(byte gmByte)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            i2cController = new I2CController(mainWindow.myFtdiDevice);



            AppStatus = i2cController.I2C_ConfigureMpsse();
            AppStatus = i2cController.DDCCI_Null_Message();// zh 250212


            byte[] SetGammaCommandArray =
                   { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_08_SET_GAMMA_DICOM, 0x00, gmByte };
            byte retval_Set_GAMMA_User, retryCount_Set_GAMMA_User = 0;

            do
            {
                retval_Set_GAMMA_User = i2cController.DDCCI_Send_Command(SetGammaCommandArray, null, false);
                Console.WriteLine("Set Gamma ColorTemp array: " + BitConverter.ToString(SetGammaCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_Set_GAMMA_User}, retryCount = {++retryCount_Set_GAMMA_User}");  // Record retry times
            } while (retval_Set_GAMMA_User != 0 && retryCount_Set_GAMMA_User <= 10);

            Console.WriteLine($"Gamma 已經設為 0x{gmByte:X2}  !!!");
            await Task.Delay(100);
        }




        //public async Task SetGammaColorTemp(byte ctByte, byte gmByte)
        //{
        //    var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        //    i2cController = new I2CController(mainWindow.myFtdiDevice);



        //    AppStatus = i2cController.I2C_ConfigureMpsse();
        //    AppStatus = i2cController.DDCCI_Null_Message();// zh 250212



        //    byte[] SetColorTempUserCommandArray =
        //         { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
        //            (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_07_SET_COLOR_TMEP, 0x00, 0x36 };
        //    byte retval_Set_CT_User, retryCount_Set__CT_User = 0;

        //    do
        //    {
        //        retval_Set_CT_User = i2cController.DDCCI_Send_Command(SetColorTempUserCommandArray);
        //        Console.WriteLine("Set Gamma Off Command array: " + BitConverter.ToString(SetColorTempUserCommandArray).Replace("-", " "));
        //        Debug.WriteLine($"ret val = {retval_Set_CT_User}, retryCount = {++retryCount_Set__CT_User}");  // Record retry times
        //    } while (retval_Set_CT_User != 0 && retryCount_Set__CT_User <= 10);
        //    Console.WriteLine($"Color Temp 已經設為 {ctByte} !!!");
        //    await Task.Delay(100);


        //    byte[] SetGammaCommandArray =
        //           { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
        //            (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_08_SET_GAMMA_DICOM, 0x00, 0x12 };
        //    byte retval_Set_GAMMA_User, retryCount_Set_GAMMA_User = 0;

        //    do
        //    {
        //        retval_Set_GAMMA_User = i2cController.DDCCI_Send_Command(SetGammaCommandArray);
        //        Console.WriteLine("Set Gamma ColorTemp array: " + BitConverter.ToString(SetGammaCommandArray).Replace("-", " "));
        //        Debug.WriteLine($"ret val = {retval_Set_GAMMA_User}, retryCount = {++retryCount_Set_GAMMA_User}");  // Record retry times
        //    } while (retval_Set_GAMMA_User != 0 && retryCount_Set_GAMMA_User <= 10);

        //    Console.WriteLine($"ColorTemp 已經設為 0x{ctByte:X2}, Gamma 已經設為 0x{gmByte:X2}  !!!");
        //    await Task.Delay(100);
        //}

        //zh251208 add>
        public async Task GammaErase()
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            i2cController = new I2CController(mainWindow.myFtdiDevice);

            AppStatus = i2cController.I2C_ConfigureMpsse();
            AppStatus = i2cController.DDCCI_Null_Message();



            byte[] GammaEraseCommandArray =
                   { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_65_SPECIAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_0A_SET_GAMMA_ERASE, 0x00, 0x01 };
            byte retval_GammaErase, retryCount_GammaErase = 0;

            do
            {
                retval_GammaErase = i2cController.DDCCI_Send_Command(GammaEraseCommandArray, null, false);
                Console.WriteLine("Gamma erase array: " + BitConverter.ToString(GammaEraseCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_GammaErase}, retryCount = {++retryCount_GammaErase}");  // Record retry times
            } while (retval_GammaErase != 0 && retryCount_GammaErase <= 10);
            Console.WriteLine($"Gamma 已經清除  !!!");
            await Task.Delay(100);
        }
        public async Task GammaErase_i1display() //0615 ian add  for Erase i1_Measurement Data
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            i2cController = new I2CController(mainWindow.myFtdiDevice);

            AppStatus = i2cController.I2C_ConfigureMpsse();
            AppStatus = i2cController.DDCCI_Null_Message();



            byte[] GammaEraseCommandArray =
                   { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_65_SPECIAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_0A_SET_GAMMA_ERASE, 0x00, 0x02 };
            byte retval_GammaErase, retryCount_GammaErase = 0;

            do
            {
                retval_GammaErase = i2cController.DDCCI_Send_Command(GammaEraseCommandArray, null, false);
                Console.WriteLine("GammaErase_i1display : " + BitConverter.ToString(GammaEraseCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_GammaErase}, retryCount = {++retryCount_GammaErase}");  // Record retry times
            } while (retval_GammaErase != 0 && retryCount_GammaErase <= 10);
            Console.WriteLine($"GammaErase_i1display 已經清除  !!!");
            await Task.Delay(500);
        }

        public async Task ResetAll()
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            i2cController = new I2CController(mainWindow.myFtdiDevice);

            AppStatus = i2cController.I2C_ConfigureMpsse();
            AppStatus = i2cController.DDCCI_Null_Message();// zh 250212

            byte[] ResetAllCommandArray =
                   { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_03_RESET_FACTORY, 0x00, 0x01 };
            byte retval_ResetAll, retryCount_ResetAll = 0;
            do
            {
                retval_ResetAll = i2cController.DDCCI_Send_Command(ResetAllCommandArray, null, false);
                Console.WriteLine("ResetAll array: " + BitConverter.ToString(ResetAllCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_ResetAll}, retryCount = {++retryCount_ResetAll}");  // Record retry times
            } while (retval_ResetAll != 0 && retryCount_ResetAll <= 10);
            Console.WriteLine($"Reset All  !!!");
            await Task.Delay(100);
        }

        //zh251208 add<














        static string HexToAscii(string hex)
        {
            string ascii = "";
            for (int i = 0; i < hex.Length; i += 2)
            {
                // 取兩個十六進制字符，轉換為數值，然後轉換為 ASCII 字符
                ascii += (char)Convert.ToInt32(hex.Substring(i, 2), 16);
            }
            return ascii;
        }


        public byte GetColorTempByte(int cct)
        {
            //供 factory rgb gain 裡面的 CT 用

            if (cct == 5400)
                return 0x36;
            else if (cct == 6500)
                return 0x41;
            else if (cct == 9300)
                return 0x5D;
            else
                return 0x00;
        }
        public byte GetGammaByte(double gamma)
        {
            if (gamma == 1.8)
                return 0x12;
            else if (gamma == 2.0)
                return 0x14;
            else if (gamma == 2.2)
                return 0x16;
            else if (gamma == 2.4)
                return 0x18;
            else if (gamma == 2.6)
                return 0x1A;
            else
                return 0x20;

        }


        public async Task<PanelDetailInfo> GetPanelDetailAsync()
        {
            PanelDetailInfo info = null;

            /////Get Doing Uniformity
            byte[] GetDoingUniformityCommandArray =
                       { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_75_SPECIAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_09_GET_PANEL_DETAIL , 0x00, 0x00 };
            byte retval_GetDoingUniformity, retryCount_GetDoingUniformity = 0;

            do
            {
                retval_GetDoingUniformity = i2cController.DDCCI_Send_Command(GetDoingUniformityCommandArray, raw =>
                {
                    info = getPanelDetailReplySupportDetail(raw);
                    panelInfo = info;
                });

                await Task.Delay(100);
                Console.WriteLine("Get Doing Uniformity: " + BitConverter.ToString(GetDoingUniformityCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_GetDoingUniformity}, retryCount = {++retryCount_GetDoingUniformity}");  // Record retry times
            } while (retval_GetDoingUniformity != 0 && retryCount_GetDoingUniformity <= 10);

            return info;   // 可能為 null

        }
        public CalibrationProtocol.PanelDetailInfo getPanelDetailReplySupportDetail(byte[] commandByteArr)
        {
            string PanelDetailJudge = "";
            MReplyP monitorReplyPackage = new MReplyP(commandByteArr);
            if (monitorReplyPackage.OPCode == OPCode.ONYX_CMD_75_SPECIAL_GET_COMMAND &&
               monitorReplyPackage.Status == Status.STATUS_SUCCESS &&
               monitorReplyPackage.VCCode == VCCode.ONYX_FCODE_09_GET_PANEL_DETAIL)
            {
                PanelDetailJudge = monitorReplyPackage.Data[monitorReplyPackage.Data.Length - 3].ToString("X2");
            }
            //data 三個
            byte data0 = monitorReplyPackage.Data[0];                   // Uniformity
            byte data1 = monitorReplyPackage.Data[1];                   // Entry bits
            byte data2 = monitorReplyPackage.Data[2];                   // Resolution bits

            bool uniformity = data0 == 0xFF;

            int entryBits = data1;                  // 08 or 0A
            int entryCount = 1 << entryBits;         // 256  1024

            int lutBits = data2;                  // 0C  or 0E
            int lutResolution = 1 << lutBits;           // 4096 or  16384
            Console.WriteLine("Panel Detail reply package: " + PanelDetailJudge);
            if (PanelDetailJudge == "FF")
            {
                Console.WriteLine("[getPanelDetailReplySupportDetail]  entryBits: " + entryBits + "   entryCount: " + entryCount);
                Console.WriteLine("[getPanelDetailReplySupportDetail]  lutBits: " + lutBits + "   lutResolution: " + lutResolution);

                Console.WriteLine("要做Uniformity");

            }
            else
            {
                Console.WriteLine("[getPanelDetailReplySupportDetail]  entryBits: " + entryBits + "   entryCount: " + entryCount);
                Console.WriteLine("[getPanelDetailReplySupportDetail]  lutBits: " + lutBits + "   lutResolution: " + lutResolution);

                Console.WriteLine("不要做Uniformity");
            }


            return new CalibrationProtocol.PanelDetailInfo(
                         uniformity,
                         entryBits, entryCount,
                         lutBits, lutResolution);



        }


    }
}
