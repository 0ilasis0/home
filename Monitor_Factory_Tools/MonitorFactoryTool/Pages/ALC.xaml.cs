using FDTI_Factory_i2c;
using ONYX_DataType;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MReplyP = ONYX_DataType.MonitorReplyPackage;
using Excel = Microsoft.Office.Interop.Excel;
using RadioButton = System.Windows.Controls.RadioButton;
using TextBox = System.Windows.Controls.TextBox;
using OnyxSensor;
using System.Net.NetworkInformation;
using OfficeOpenXml.Style.Dxf;
using MonitorFactoryTool.Pages;
using System.Windows.Threading;
using MonitorFactoryTool.ONYX.Utilities;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System.Threading;
using System.Xml.Linq;



namespace MonitorFactoryTool
{
    /// <summary>
    /// Page1.xaml 的互動邏輯
    /// </summary>
    public partial class ALC : Page
    {
        //private I2CController i2cController;

        private I2CController_STM i2cControllerStm = null;


        private double temperature = 0;
        private int record_time = 0;
        private int overtimes = 0;
        private int level = 0;
        private int count = 0;
        public List<double> luminanceDiff_Array = new List<double>(); // 32 亮度差異

        public SensorBase SELECTED_SENSOR = null;

        private bool isLogging = false;

        private byte curr_PWM = 0xFF; //Derek

        private ushort curr_PWM_full = 0;   // 250905 wu add for 10/12-bit PWM
        const byte ALC_EXTERNAL_MASK = 0x80; //250909 wu add for check internal/external
        const byte ALC_MODE_MASK = 0x0F;


        private byte ALC_MODE_SELECTED = 0x00;
        private int ALC_MODE_NUM = 0;
        private int ALC_MODE_TARGET_NITS = 250;
        private int ALC_MODE_CORRECT_TIME = 0;
        private int ALC_MODE_COUNT = 1;
        private bool ALC_MODE_ON_OFF = false;
        private bool ALC_MODE_CONNECTION = true;  //250909 wu add for internal/external      true:external false:internal
        private double ALC_SENSOR_LUMINANCE = 0;
        private double[] ALC_MODE_OFFSET = { 0, 0, 0, 0, 0 };
        private double[] alc_lut = {
            1.0215, 1.0215, 1.0215, 1.0215, 1.0215, 1.0215, 1.0215, 1.0215, 1.0267, 1.0244,	// 0 ~ 4.5 °C
	        1.0194, 1.0149, 1.0201, 1.0246, 1.0263, 1.028, 1.0298, 1.0259, 1.0285,  1.0251,	// 5 ~ 9.5 °C
	        1.0233, 1.0254, 1.0203, 1.0221, 1.0203, 1.0207, 1.0194, 1.0218, 1.0198, 1.0210, // 10 ~ 14.5 °C
	        1.0197, 1.0161, 1.0127, 1.0109, 1.0137, 1.0088, 1.0081, 1.0117, 1.0092, 1.0078, // 15 ~ 19.5 °C
	        1.0099, 1.0071, 1.0028, 1.0028, 1.0029, 1.0039, 1.0039, 1.0016, 1.0001, 1.0011, // 20 ~ 24.5 °C

	        1.0000, 0.9970, 0.9946, 0.9909, 0.9944, 0.9953, 0.9959, 0.9919, 0.9916, 0.9915, // 25 ~ 29.5 °C
	        0.9915, 0.99, 0.9925, 0.9907, 0.9887, 0.9888, 0.9929, 0.9871, 0.9903, 0.9891,   // 30 ~ 34.5 °C
	        0.9886, 0.9891, 0.9883, 0.9881, 0.9872, 0.9848, 0.9856, 0.9895, 0.9886, 0.9879, // 35 ~ 39.5 °C
	        0.9928, 0.9965, 0.9975, 0.9948, 0.9981, 0.9981, 0.9964, 0.9996, 0.9984, 1.0051, // 40 ~ 44.5 °C
	        1.0117, 1.0101, 1.0128, 1.0139, 1.0122, 1.0151, 1.0119, 1.0164, 1.0213, 1.0278, // 45 ~ 49.5 °C

	        1.0335, 1.0348, 1.0389, 1.0395, 1.039, 1.043, 1.0512, 1.0592, 1.0612, 1.0633,   // 50 ~ 54.5 °C
	        1.0667, 1.0695, 1.0740, 1.0732, 1.0769, 1.0832, 1.0985, 1.1025, 1.1053, 1.1084, // 55 ~ 59.5 °C
	        1.1133                                                                          // 60 °C
        };

        private readonly SensorBase _sensor;
        private readonly I2CController _i2c;
        public Action<SensorMeasureYxy_t> UpdateUIAction;
        private CancellationTokenSource _cts;       //  CancellationTokenSource 用來控制當blackwindow exit 按下去要關閉背景task 

        public ALC()
        {
            InitializeComponent();
            RadiobuttonExternal.IsEnabled = false;
            RadiobuttonInternal.IsEnabled = false;
        }

        public class alcMeasItem
        {
            public string Time { get; set; }
            public string Temperature { get; set; }
            public string Luminance { get; set; }
            public string Status { get; set; }
            public string Level { get; set; }
            public string Center { get; set; }
            public string Corner { get; set; }
            public string Difference { get; set; }
            public string Descript { get; set; }



            public override string ToString()  /////250203 wu 
            {
                return $"{Time}, {Temperature}, {Luminance}, {Status}";
            }
        }

        private void ALCMode_Checked(object sender, RoutedEventArgs e)  ///////wu add 綁定ALC radio button
        {
            CheckALCModeAndValue();
        }


        private (string Mode, string Value) CheckALCModeAndValue()
        {
            // 建立 RadioButton 與 TextBox 的對應關係
            var alcDict = new Dictionary<RadioButton, (string Mode, TextBox TextBox)>
            {
                { buttonALC1, ("ALC1", textBoxALC1Data) },
                { buttonALC2, ("ALC2", textBoxALC2Data) },
                { buttonALC3, ("ALC3", textBoxALC3Data) },
                { buttonALC4, ("ALC4", textBoxALC4Data) },
                { buttonALC5, ("ALC5", textBoxALC5Data) }
            };


            var selected = alcDict.FirstOrDefault(kvp => kvp.Key.IsChecked == true);  // selected RadioButton (ALC Mode)

            if (selected.Key != null)
            {
                ALC_MODE_ON_OFF = false;
                string alcMode = selected.Value.Mode;
                string alcValue = selected.Value.TextBox.Text;
                switch (alcMode)
                {
                    case "ALC1":
                        ALC_MODE_SELECTED = 0x01;
                        break;
                    case "ALC2":
                        ALC_MODE_SELECTED = 0x02;
                        break;
                    case "ALC3":
                        ALC_MODE_SELECTED = 0x03;
                        break;
                    case "ALC4":
                        ALC_MODE_SELECTED = 0x04;
                        break;
                    case "ALC5":
                        ALC_MODE_SELECTED = 0x05;
                        break;
                    default:
                        ALC_MODE_SELECTED = 0x00;
                        break;
                }
                Console.WriteLine($"Choose：{alcMode}, Mode Value：{alcValue}");
                return (alcMode, alcValue);
            }
            else
            {
                Console.WriteLine("Choose ALC Mode !!!");
                return ("None", "N/A");
            }
        }


        // Create thread timer for ALC on/off
        private System.Threading.Timer threadingTimer;
        private void StartTimer()
        {
            threadingTimer = new System.Threading.Timer(new System.Threading.TimerCallback(ThreadMethod), null, 1000, 3000);
        }
        private void StopTimer()
        {
            threadingTimer.Dispose();
        }
        private void ThreadMethod(object a)
        {
            this.Dispatcher.Invoke(new System.Action(() =>
            {

                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                // i2cController = new I2CController(mainWindow.myFtdiDevice);
                byte ret_val, retry_Count = 0;
                if (!ALC_MODE_ON_OFF)
                {
                    //byte[] SetAlcModeCommandArray =
                    //{ (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    //(byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_15_SET_ALC_MODE, 0x00, ALC_MODE_SELECTED };

                    if (mainWindow.comboModelPort.Text.Contains("I2C"))
                    {
                        if (ALC_MODE_CONNECTION == true) //external: stm32
                        {
                            var i2cControllerStm = new I2CController_STM(mainWindow.myFtdiDevice);
                            retry_Count = 0;

                            byte[] SetAlcModeCommandArray =
                  { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_15_SET_ALC_MODE, 0x00, ALC_MODE_SELECTED };



                            do { ret_val = i2cControllerStm.DDCCI_Send_Command(SetAlcModeCommandArray); }
                            while (ret_val != 0 && retry_Count++ <= 10);
                        }
                        else
                        {

                            var i2cController = new I2CController(mainWindow.myFtdiDevice);
                            retry_Count = 0;

                            byte[] SetAlcModeCommandArray =
                  { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_15_SET_ALC_MODE, 0x00, ALC_MODE_SELECTED };


                            do { ret_val = i2cController.DDCCI_Send_Command(SetAlcModeCommandArray); }
                            while (ret_val != 0 && retry_Count++ <= 10);
                        }
                        if (ret_val == 0) ALC_MODE_ON_OFF = true;
                    }
                }

                //byte[] GetTemperatureCommandArray =
                //    { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                //    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_14_GET_TEMPERATURE, 0x00, 0x00 };

                if (mainWindow.comboModelPort.Text.Contains("I2C"))
                {
                    if (ALC_MODE_CONNECTION == true) //external: stm32
                    {
                        var i2cControllerStm = new I2CController_STM(mainWindow.myFtdiDevice);
                        retry_Count = 0;
                        do
                        {
                            byte[] GetTemperatureSTMCommandArray =
            { (byte)Destination.ONYX_MONITOR_STM_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_14_GET_TEMPERATURE, 0x00, 0x00 };

                            Console.WriteLine("Get Temp array(STM): " + BitConverter.ToString(GetTemperatureSTMCommandArray).Replace("-", " "));
                            ret_val = i2cControllerStm.DDCCI_Send_Command(GetTemperatureSTMCommandArray, getTemperature_STM);
                            Debug.WriteLine($"ret val = {ret_val}, retryCount = {++retry_Count}");
                            //if(retry_Count>10)
                            //{
                            //    System.Windows.MessageBox.Show("GET 溫度 tretry ccount >10!!!");
                            //}
                        } while (ret_val != 0 && retry_Count <= 10);
                    }
                    else
                    {
                        byte[] GetTemperatureCommandArray =
            { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_14_GET_TEMPERATURE, 0x00, 0x00 };


                        var i2cController = new I2CController(mainWindow.myFtdiDevice);
                        retry_Count = 0;
                        do
                        {
                            ret_val = i2cController.DDCCI_Send_Command(GetTemperatureCommandArray, getTemperature);
                            Debug.WriteLine($"ret val = {ret_val}, retryCount = {++retry_Count}");
                        } while (ret_val != 0 && retry_Count <= 10);
                    }
                }

                // get ALC mode standard**
                var (mode, valueStr) = CheckALCModeAndValue();
                if (!int.TryParse(valueStr, out int standardValue))
                {
                    Debug.WriteLine($"Mode {mode} value \"{valueStr}\" is in valid");
                    return;
                }

                // Get Diff Rate, remove"%" //
                string diffRateStr = textBoxDiffRate.Text.Replace("%", "").Trim();
                if (!double.TryParse(diffRateStr, out double diffRate))
                {
                    Debug.WriteLine($"Diff Rate \"{textBoxDiffRate.Text}\" is in valid");
                    return;
                }

                // **get i1d3 measure Y(Luminance)**
                bool devIsConnect = SELECTED_SENSOR.MeasYxyz(out SensorMeasureYxy_t Meas_Yxyz_1);
                if (!devIsConnect)
                {
                    Console.WriteLine("Please check i1d3 device connection");
                    return;
                }
                double measuredY_1 = Meas_Yxyz_1.Y;
                Console.WriteLine($"1   Measured Luminance (Y) = {measuredY_1} nits");
                string lumY_1 = measuredY_1.ToString();


                double adjustedUpValue = standardValue * (1 + (diffRate / 100.0));   // Up Limit
                double adjustedDownValue = standardValue * (1 - (diffRate / 100.0)); // Down Limit

                string status = ((measuredY_1 >= adjustedDownValue) && (measuredY_1 <= adjustedUpValue)) ? "Pass" : "Over";

                if (status == "Over")
                {
                    overtimes++;
                }
                labelOverTimesCount.Content = overtimes;
                var item = new alcMeasItem { Time = $"{record_time}", Temperature = temperature.ToString(), Luminance = $"{measuredY_1:F4} nits", Status = status };
                record_time++;
                listBoxTempLum.Items.Add(item);
                listBoxTempLum.ScrollIntoView(item);
            }));
        }
        // Create thread timer for ALC correct
        private System.Threading.Timer threadingTimerCorrect;
        private void StartTimer_ALC_Correct()
        {
            threadingTimerCorrect = new System.Threading.Timer(new System.Threading.TimerCallback(ThreadMethod_ALC_Correct), null, 1000, 3000);
        }
        private void StopTimer_ALC_Correct()
        {
            threadingTimerCorrect.Dispose();
        }

        private void ThreadMethod_ALC_Correct(object a)
        {

            this.Dispatcher.Invoke(new System.Action(() =>
            {
                // Select target luminance for each ALC mode
                switch (ALC_MODE_COUNT)
                {
                    case 1:
                        ALC_MODE_SELECTED = 0x01;
                        buttonALC1.IsChecked = true;
                        if (int.TryParse(textBoxALC1Data.Text, out ALC_MODE_TARGET_NITS))
                            Debug.WriteLine($"Correcting ALC Mode {ALC_MODE_COUNT} ...... ({ALC_MODE_CORRECT_TIME})");
                        break;
                    case 2:
                        ALC_MODE_SELECTED = 0x02;
                        buttonALC2.IsChecked = true;
                        if (int.TryParse(textBoxALC2Data.Text, out ALC_MODE_TARGET_NITS))
                            Debug.WriteLine($"Correcting ALC Mode {ALC_MODE_COUNT} ...... ({ALC_MODE_CORRECT_TIME})");
                        break;
                    case 3:
                        ALC_MODE_SELECTED = 0x03;
                        buttonALC3.IsChecked = true;
                        if (int.TryParse(textBoxALC3Data.Text, out ALC_MODE_TARGET_NITS))
                            Debug.WriteLine($"Correcting ALC Mode {ALC_MODE_COUNT}  ...... ({ALC_MODE_CORRECT_TIME})");
                        break;
                    case 4:
                        ALC_MODE_SELECTED = 0x04;
                        buttonALC4.IsChecked = true;
                        if (int.TryParse(textBoxALC4Data.Text, out ALC_MODE_TARGET_NITS))
                            Debug.WriteLine($"Correcting ALC Mode {ALC_MODE_COUNT}  ...... ({ALC_MODE_CORRECT_TIME})");
                        break;
                    case 5:
                        ALC_MODE_SELECTED = 0x05;
                        buttonALC5.IsChecked = true;
                        if (int.TryParse(textBoxALC5Data.Text, out ALC_MODE_TARGET_NITS))
                            Debug.WriteLine($"Correcting ALC Mode {ALC_MODE_COUNT}  ...... ({ALC_MODE_CORRECT_TIME})");
                        break;
                    default:
                        ALC_MODE_SELECTED = 0x00;
                        break;
                }

                // Set i2c controller for sending command to AD board 
                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                //  i2cController = new I2CController(mainWindow.myFtdiDevice);

                // Close ALC mode 
                byte[] SetAlcModeCommandArray =
                    { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_15_SET_ALC_MODE, 0x00, 0x00 };
                byte ret_val, retry_Count = 0;
                if (mainWindow.comboModelPort.Text.Contains("I2C"))
                {
                    if (ALC_MODE_CONNECTION == true) //external: stm32
                    {
                        var i2cControllerStm = new I2CController_STM(mainWindow.myFtdiDevice);
                        retry_Count = 0;
                        do
                        {
                            ret_val = i2cControllerStm.DDCCI_Send_Command(SetAlcModeCommandArray);
                            Debug.WriteLine($"ret val = {ret_val}, retryCount = {++retry_Count}");
                        } while (ret_val != 0 && retry_Count <= 10);
                    }
                    else
                    {
                        var i2cController = new I2CController(mainWindow.myFtdiDevice);
                        retry_Count = 0;
                        do
                        {
                            ret_val = i2cController.DDCCI_Send_Command(SetAlcModeCommandArray);
                            Debug.WriteLine($"ret val = {ret_val}, retryCount = {++retry_Count}");
                        } while (ret_val != 0 && retry_Count <= 10);
                    }

                }

                //// Read Panel Temperature 
                //byte[] GetTemperatureCommandArray =
                //    { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                //    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_14_GET_TEMPERATURE, 0x00, 0x00 };
                retry_Count = 0;
                if (mainWindow.comboModelPort.Text.Contains("I2C"))
                {

                    if (ALC_MODE_CONNECTION == true) //external: stm32
                    {
                        var i2cControllerStm = new I2CController_STM(mainWindow.myFtdiDevice);
                        retry_Count = 0;
                        byte[] GetTemperatureCommandArray =
                 { (byte)Destination.ONYX_MONITOR_STM_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_14_GET_TEMPERATURE, 0x00, 0x00 };
                        retry_Count = 0;

                        do
                        {
                            ret_val = i2cControllerStm.DDCCI_Send_Command(GetTemperatureCommandArray, getTemperature_STM);
                            Debug.WriteLine($"ret val = {ret_val}, retryCount = {++retry_Count}");
                        } while (ret_val != 0 && retry_Count <= 10);
                    }
                    else
                    {
                        var i2cController = new I2CController(mainWindow.myFtdiDevice);
                        retry_Count = 0;
                        byte[] GetTemperatureCommandArray =
                { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_14_GET_TEMPERATURE, 0x00, 0x00 };

                        do
                        {
                            ret_val = i2cController.DDCCI_Send_Command(GetTemperatureCommandArray, getTemperature);
                            Debug.WriteLine($"ret val = {ret_val}, retryCount = {++retry_Count}");
                        } while (ret_val != 0 && retry_Count <= 10);
                    }
                }

                // Read Color Sensor RGB count 
                //byte[] GetALC_SensorCommandArray =
                //    { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                //    (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_75_SPECIAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_03_GET_ALC_SENSOR, 0x00, 0x00 };
                retry_Count = 0;
                if (mainWindow.comboModelPort.Text.Contains("I2C"))
                {

                    if (ALC_MODE_CONNECTION == true) //external: stm32
                    {

                        var i2cControllerStm = new I2CController_STM(mainWindow.myFtdiDevice);
                        retry_Count = 0;
                        byte[] GetALC_SensorCommandArray =
        { (byte)Destination.ONYX_MONITOR_STM_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_75_SPECIAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_03_GET_ALC_SENSOR, 0x00, 0x00 };

                        do
                        {
                            ret_val = i2cControllerStm.DDCCI_Send_Command(GetALC_SensorCommandArray, getALCRGB);
                            Debug.WriteLine($"ret val = {ret_val}, retryCount = {++retry_Count}");
                        } while (ret_val != 0 && retry_Count <= 10);
                    }
                    else
                    {
                        var i2cController = new I2CController(mainWindow.myFtdiDevice);
                        retry_Count = 0;
                        byte[] GetALC_SensorCommandArray =
                            { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                            (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_75_SPECIAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_03_GET_ALC_SENSOR, 0x00, 0x00 };

                        do
                        {
                            ret_val = i2cController.DDCCI_Send_Command(GetALC_SensorCommandArray, getALCRGB);
                            Debug.WriteLine($"ret val = {ret_val}, retryCount = {++retry_Count}");
                        } while (ret_val != 0 && retry_Count <= 10);
                    }
                }

                // **get i1d3 measure Y(Luminance)**
                bool devIsConnect = SELECTED_SENSOR.MeasYxyz(out SensorMeasureYxy_t Meas_Yxyz);
                if (!devIsConnect)
                {
                    Console.WriteLine("Please check i1d3 device connection");
                    return;
                }
                double luminanceOffset = Meas_Yxyz.Y - ALC_SENSOR_LUMINANCE;
                Console.WriteLine($"Offset = {luminanceOffset} nits");

                // Calculate luminance difference between panel center and ALC mode target luminance 
                // If difference is larger than 0, then correct the panel
                double lumDiff = Math.Abs(Meas_Yxyz.Y - ALC_MODE_TARGET_NITS);
                if (lumDiff != 0)
                {
                    //// Get panel current PWM value
                    //byte[] GetOffsetCommandArray =
                    //{ (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    //(byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_75_SPECIAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_02_GET_ALC_CORRECT, 0x00, 0x00};


                    retry_Count = 0;
                    if (mainWindow.comboModelPort.Text.Contains("I2C"))
                    {
                        if (ALC_MODE_CONNECTION == true) //external: stm32
                        {
                            // Get panel current PWM value
                            byte[] GetOffsetCommandArray =
                            { (byte)Destination.ONYX_MONITOR_STM_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_75_SPECIAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_02_GET_ALC_CORRECT, 0x00, 0x00};


                            var i2cControllerStm = new I2CController_STM(mainWindow.myFtdiDevice);
                            retry_Count = 0;
                            do
                            {
                                Console.WriteLine("Get PWM array(STM): " + BitConverter.ToString(GetOffsetCommandArray).Replace("-", " "));
                                ret_val = i2cControllerStm.DDCCI_Send_Command(GetOffsetCommandArray, getPWM);
                                Debug.WriteLine($"ret val = {ret_val}, retryCount = {++retry_Count}");
                            } while (ret_val != 0 && retry_Count <= 10);
                        }
                        else
                        {
                            // Get panel current PWM value
                            byte[] GetOffsetCommandArray =
                            { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_75_SPECIAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_02_GET_ALC_CORRECT, 0x00, 0x00};



                            var i2cController = new I2CController(mainWindow.myFtdiDevice);
                            retry_Count = 0;
                            do
                            {
                                ret_val = i2cController.DDCCI_Send_Command(GetOffsetCommandArray, getPWM);
                                Debug.WriteLine($"ret val = {ret_val}, retryCount = {++retry_Count}");
                            } while (ret_val != 0 && retry_Count <= 10);
                        }
                    }
                    //     Debug.WriteLine($"Current PWM is {curr_PWM}"); //Derek 8 bit
                    Debug.WriteLine($"Current PWM (10 bits) is {curr_PWM_full}"); //Wu  10 bit

                    // Calculate PWM difference, if 1 PWM value is approximately equal to 1 nit
                    int count = 0;
                    if (Meas_Yxyz.Y - ALC_MODE_TARGET_NITS > 0)
                    {
                        // count = Convert.ToInt16(curr_PWM) - (int)lumDiff; //Derek 8 bit

                        count = curr_PWM_full - (int)Math.Abs(lumDiff);  //Wu  10 bit
                        Debug.WriteLine($"PWM ------------ {count}");


                    }
                    else
                    {
                        //  count = Convert.ToInt16(curr_PWM) + (int)lumDiff; //Derek 8 bit

                        count = curr_PWM_full + (int)Math.Abs(lumDiff); //Wu  10 bit
                        Debug.WriteLine($"PWM ++++++++++++ {count}");

                    }
                    //if (count > 255) count = 255;
                    //else if (count < 0) count = 0;
                    if (count > 1023) count = 1023;
                    if (count < 0) count = 0;

                    double factor = ALC_MODE_TARGET_NITS / Meas_Yxyz.Y;
                    count = (int)(curr_PWM_full * factor);


                    // curr_PWM = Convert.ToByte(count); //Derek 8 bit
                    curr_PWM_full = (ushort)(count); //Wu  10 bit

                    // Debug.WriteLine($"Test Test Test {curr_PWM}");//Derek 8 bit
                    Debug.WriteLine($"Test Test Test {curr_PWM_full}"); //Wu  10 bit 

                    // Send ALC correct command to set PWM for each ALC mode 
                    //byte[] SetOffsetCommandArray =
                    //{ (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    //(byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_65_SPECIAL_SET_COMMAND, 0x07, 0x02, ALC_MODE_SELECTED, curr_PWM, 0x00, 0x00, 0x00 };

                    //Wu  250905 add for 10 bit

                    byte pwmLo = (byte)(count & 0xFF);
                    byte pwmHi = (byte)((count >> 8) & 0xFF);
                    //Wu  250905 add for 10 bit

                    retry_Count = 0;
                    if (mainWindow.comboModelPort.Text.Contains("I2C"))
                    {

                        if (ALC_MODE_CONNECTION == true)   //external: stm32
                        {
                            byte[] SetOffsetCommandArray =
                 { (byte)Destination.ONYX_MONITOR_STM_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x87,    //wu 250910 modify to 87
                    (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_65_SPECIAL_SET_COMMAND, 0x07, 0x02, ALC_MODE_SELECTED, pwmLo, pwmHi };


                            var i2cControllerStm = new I2CController_STM(mainWindow.myFtdiDevice);
                            retry_Count = 0;
                            do
                            {
                                Console.WriteLine("Set Offset array(STM): " + BitConverter.ToString(SetOffsetCommandArray).Replace("-", " "));
                                Debug.WriteLine($"Sending PWM: count={count}, pwmLo={pwmLo:X2}, pwmHi={pwmHi:X2}");

                                ret_val = i2cControllerStm.DDCCI_Send_Command(SetOffsetCommandArray);
                                Debug.WriteLine($"ret val = {ret_val}, retryCount = {++retry_Count}");
                            } while (ret_val != 0 && retry_Count <= 10);
                        }
                        else
                        {
                            byte[] SetOffsetCommandArray =
                 { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_65_SPECIAL_SET_COMMAND, 0x07, 0x02, ALC_MODE_SELECTED, pwmLo, pwmHi, 0x00, 0x00 };


                            var i2cController = new I2CController(mainWindow.myFtdiDevice);
                            retry_Count = 0;
                            do
                            {
                                Console.WriteLine(" Set Offset array(STM): " + BitConverter.ToString(SetOffsetCommandArray).Replace("-", " "));
                                ret_val = i2cController.DDCCI_Send_Command(SetOffsetCommandArray);
                                Debug.WriteLine($"ret val = {ret_val}, retryCount = {++retry_Count}");
                            } while (ret_val != 0 && retry_Count <= 10);
                        }
                    }

                }
                string correct_status = (lumDiff != 0) ? "Correct" : "Keep";
                var item_correct = new alcMeasItem { Descript = $"{correct_status}", Center = $"{Meas_Yxyz.Y:F4}", Corner = $"{ALC_SENSOR_LUMINANCE:F4}", Difference = $"{luminanceOffset:F4}" };
                listBoxCenterCorner.Items.Add(item_correct);
                listBoxCenterCorner.ScrollIntoView(item_correct);

                ALC_MODE_CORRECT_TIME++;

                // Correct times for each ALC mode 
                if (ALC_MODE_CORRECT_TIME > 2 && ALC_MODE_COUNT <= ALC_MODE_NUM)
                {
                    ALC_MODE_CORRECT_TIME = 0;
                    ALC_MODE_COUNT++;
                }

                // If ALC mode have been corrected, stop the timer and reset variable
                // Then update correct status 
                if (ALC_MODE_COUNT > ALC_MODE_NUM)
                {
                    ALC_MODE_CORRECT_TIME = 0;
                    ALC_MODE_COUNT = 1;
                    StopTimer_ALC_Correct();
                    buttonCorrect.IsEnabled = true;
                    buttonCorrect.Content = "Correct";
                    labelCorrectYes.Content = "Yes";
                    var item_alc_correct_descript = new alcMeasItem
                    {
                        Descript =
                        $"Finish\n" +
                        $"ALC Correct",
                        Corner = "---",
                        Center = "---",
                        Difference = "---"
                    };
                    listBoxCenterCorner.Items.Add(item_alc_correct_descript);
                    listBoxCenterCorner.ScrollIntoView(item_alc_correct_descript);
                }
            }));
        }

        private void buttonOn_Click(object sender, RoutedEventArgs e)
        {
            record_time = 0;
            ALC_MODE_ON_OFF = false;
            StartTimer();
        }
        private void buttonOff_Click(object sender, RoutedEventArgs e)
        {
            StopTimer();
            ALC_MODE_ON_OFF = false;
        }
        public void getTemperature(byte[] commandByteArr)
        {
            MReplyP monitorReplyPackage = new MReplyP(commandByteArr);
            if (monitorReplyPackage.OPCode == OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND && monitorReplyPackage.Status == Status.STATUS_SUCCESS)
            {
                switch (monitorReplyPackage.VCCode)
                {
                    case VCCode.ONYX_FCODE_14_GET_TEMPERATURE:
                        Debug.WriteLine("Temperature data 0 is " + Convert.ToInt16(monitorReplyPackage.Data[0].ToString("X2"), 16).ToString());
                        Debug.WriteLine("Temperature data 1 is " + Convert.ToInt16(monitorReplyPackage.Data[1].ToString("X2"), 16).ToString());

                        if ((monitorReplyPackage.Data[0] >> 7) == 0x0)
                        {
                            temperature = (double)((int)(monitorReplyPackage.Data[0] << 8 | monitorReplyPackage.Data[1])) / 128.000;
                        }
                        else
                        {
                            temperature = (double)((int)(monitorReplyPackage.Data[0] << 8 | monitorReplyPackage.Data[1]) - 65536) / 128.000;
                        }
                        Debug.WriteLine("Temperature is " + temperature.ToString());

                        break;
                }
            }
        }

        private void getTemperature_STM(byte[] reply)
        {
            double temperatureValue = 0; // 用來儲存解析出的溫度值
            bool parseSuccess = false;

            MReplyP monitorReplyPackage = new MReplyP(reply);
            if (monitorReplyPackage.OPCode == OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND && monitorReplyPackage.Status == Status.STATUS_SUCCESS)
            {

                if (monitorReplyPackage.VCCode == VCCode.ONYX_FCODE_14_GET_TEMPERATURE)
                {
                    Debug.WriteLine("Temperature data 0 is " + Convert.ToInt16(monitorReplyPackage.Data[0].ToString("X2"), 16).ToString());
                    Debug.WriteLine("Temperature data 1 is " + Convert.ToInt16(monitorReplyPackage.Data[1].ToString("X2"), 16).ToString());

                    if ((monitorReplyPackage.Data[0] >> 7) == 0x0)
                    {
                        temperature = (double)((int)(monitorReplyPackage.Data[0] << 8 | monitorReplyPackage.Data[1])) / 128.000;
                    }
                    else
                    {
                        temperature = (double)((int)(monitorReplyPackage.Data[0] << 8 | monitorReplyPackage.Data[1]) - 65536) / 128.000;
                    }
                    parseSuccess = true;
                    //  Debug.WriteLine("Temperature is " + temperature.ToString());
                    Debug.WriteLine("STM32 Temperature parsed: " + temperature.ToString());

                }
            }
            //if (parseSuccess)
            //{
            //    // 使用 Dispatcher 安全地從背景執行緒更新 UI
            //    this.Dispatcher.Invoke(() =>
            //    {
            //        var item = new alcMeasItem
            //        {
            //            Time = $"{record_time}",
            //            Temperature = $"{temperatureValue:F4} °C (STM32)",
            //            Luminance = "---",
            //            Status = "OK"
            //        };
            //        record_time++;
            //        listBoxTempLum.Items.Add(item);
            //        listBoxTempLum.ScrollIntoView(item);
            //    });
            //}
        }

        private void buttonListExport_Click(object sender, RoutedEventArgs e)
        {
            if (buttonListExport.IsEnabled)
            {
                // Configure open file dialog box
                var dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.FileName = "ALC_output"; // Default file name
                dialog.DefaultExt = ".xlsx"; // Default file extension
                dialog.Filter = "(.xlsx)|*.xlsx"; // Filter files by extension

                // Show open file dialog box
                bool? result = dialog.ShowDialog();

                // Process open file dialog box results
                if (result == true)
                {
                    // Open document
                    string filename = dialog.FileName; // This filename contains the full archive path          
                    string filePath = filename;//zh

                    //string currentTime = DateTime.Now.ToString("yyyy/MM/dd/HH:mm:ss");

                    // Create a new Excel application
                    Excel.Application excelapp = new Excel.Application();
                    // Open Excel file
                    Excel.Workbook workbook = excelapp.Workbooks.Add();
                    Excel.Worksheet worksheet = workbook.Worksheets[1];

                    // Adjust worksheet colum width 
                    worksheet.Columns["A"].ColumnWidth = 10;

                    // Write title
                    try
                    {
                        Excel.Range cell_number = worksheet.Cells[1, 1];
                        cell_number.Value = "number";
                        Excel.Range cell_temperature = worksheet.Cells[1, 2];
                        cell_temperature.Value = "temperature";
                        Excel.Range cell_nits = worksheet.Cells[1, 3];
                        cell_nits.Value = "nits";
                        Excel.Range cell_pass = worksheet.Cells[1, 4];
                        cell_pass.Value = "pass";

                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("Error: Writing title failed");
                    }
                    try
                    {
                        for (int i = 0; i < listBoxTempLum.Items.Count; i++)
                        {
                            //string[] parts = listBoxTempLum.Items[i].ToString().Split(new[] { ",     " }, StringSplitOptions.None);
                            string[] parts = listBoxTempLum.Items[i].ToString().Split(',');

                            // 初始化欄位
                            string number = "", temperature = "", nits = "", pass = "";

                            //zh add
                            number = parts[0];
                            temperature = parts[1];
                            nits = parts[2];
                            pass = parts[3];


                            Excel.Range cell_number = worksheet.Cells[i + 2, 1];
                            cell_number.Value = number;
                            Excel.Range cell_temperature = worksheet.Cells[i + 2, 2];
                            cell_temperature.Value = temperature;
                            Excel.Range cell_nits = worksheet.Cells[i + 2, 3];
                            cell_nits.Value = nits;
                            Excel.Range cell_pass = worksheet.Cells[i + 2, 4];
                            cell_pass.Value = pass;

                        }
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("Error: Writing data failed");
                    }

                    try
                    {
                        workbook.SaveAs($"{filePath}");
                        workbook.Close();
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("Please close the file and try again");
                    }

                    excelapp.Quit();

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelapp);
                }
            }
        }
        private void buttonCorrectExport_Click(object sender, RoutedEventArgs e)
        {
            if (buttonCorrectExport.IsEnabled)
            {
                // Configure open file dialog box
                var dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.FileName = "ALC_output"; // Default file name
                dialog.DefaultExt = ".xlsx"; // Default file extension
                dialog.Filter = "(.xlsx)|*.xlsx"; // Filter files by extension

                // Show open file dialog box
                bool? result = dialog.ShowDialog();

                // Process open file dialog box results
                if (result == true)
                {
                    // Open document
                    string filename = dialog.FileName; // This filename contains the full archive path          
                    string filePath = filename;//zh

                    //string currentTime = DateTime.Now.ToString("yyyy/MM/dd/HH:mm:ss");

                    // Create a new Excel application
                    Excel.Application excelapp = new Excel.Application();
                    // Open Excel file
                    Excel.Workbook workbook = excelapp.Workbooks.Add();
                    Excel.Worksheet worksheet = workbook.Worksheets[1];

                    // Adjust worksheet colum width 
                    worksheet.Columns["A"].ColumnWidth = 10;

                    // Write title
                    try
                    {
                        Excel.Range cell_level = worksheet.Cells[1, 1];
                        cell_level.Value = "Level";
                        Excel.Range cell_center = worksheet.Cells[1, 2];
                        cell_center.Value = "Center";
                        Excel.Range cell_corner = worksheet.Cells[1, 3];
                        cell_corner.Value = "Corner";
                        Excel.Range cell_diff = worksheet.Cells[1, 4];
                        cell_diff.Value = "Difference";
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("Error: Writing title failed");
                    }
                    try
                    {
                        for (int i = 0; i < 32; i++)
                        {
                            if (listBoxCenterCorner.Items[i] is alcMeasItem item)
                            {
                                worksheet.Cells[i + 2, 1].Value = item.Level;
                                worksheet.Cells[i + 2, 2].Value = item.Center;
                                worksheet.Cells[i + 2, 3].Value = item.Corner;
                                worksheet.Cells[i + 2, 4].Value = item.Difference;
                            }
                        }
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("Error: Writing data failed");
                    }

                    try
                    {
                        workbook.SaveAs($"{filePath}");
                        workbook.Close();
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("Please close the file and try again");
                    }

                    excelapp.Quit();

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelapp);
                }
            }
        }
        private async void buttonCorrect_Click(object sender, RoutedEventArgs e)
        {
            //260112 wu add to store i1 measure data

            // =========================================================================================
            // 初始化與檢查
            // =========================================================================================

            var mainWin = (MainWindow)System.Windows.Application.Current.MainWindow;
            var calibrationPage = mainWin.Calibration.Content as Calibration;
            //var blackWindow = System.Windows.Application.Current.Windows.OfType<BlackWindow>().FirstOrDefault(w => w.IsVisible);
            var controller = mainWin?.GetI2CController();
            var protocol = new CalibrationProtocol(controller);

            // blackwindow 中的 exit button 確認 有沒有按下去
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;


            var blackWindow = await EnsureBlackWindowAsync(BlackWindowSource.Verification); // 或 Calibration
            if (blackWindow == null)
            {
                System.Windows.MessageBox.Show("取消選擇螢幕，無法開啟 BlackWindow");
                return;
            }


            // 鎖定 UI
            buttonCorrect.IsEnabled = false;
            blackWindow.textblockStatus.Text = "Starting i1 Golden Measurement...";
            blackWindow.buttonStart.IsEnabled = false;

            // 準備 Log 容器 (用於最後存檔)
            StringBuilder logBuilder = new StringBuilder();
            void LogAndPrint(string msg)
            {
                Console.WriteLine(msg);
                Debug.WriteLine(msg);
                logBuilder.AppendLine(msg);
            }

            // 建立量測用的色塊 (WhiteBox)
            Rectangle whiteBox = blackWindow.CreateCenteredWhiteBox();
            blackWindow.canvasRoot.Children.Add(whiteBox);


            // i1 Golden Measurement 專用 Log
            // 使用 Temperature 欄位顯示 Level / Gray / CCT
            void Addi1goldenLumLog(
                string status,
                double y,
                string levelInfo   // 例如 "Lv128", "Gray 64", "6500K"
            )
            {
                string displayLevel = levelInfo;
                string displayType = "";


                if (levelInfo.Contains("_"))
                {
                    var parts = levelInfo.Split(new char[] { '_' }, 2);
                    displayLevel = parts[0];

                    if (parts.Length > 1)
                        displayType = parts[1];
                }
                else
                {
                    if (!string.IsNullOrEmpty(status))
                    {
                        displayType = status;
                    }
                }


                var item = new alcMeasItem
                {
                    // Level
                    Descript = displayLevel,

                    // r_raw / g_raw / Black
                    Center = displayType,

                    // 亮度
                    Corner = $"{y:F4}"
                };

                listBoxCenterCorner.Items.Add(item);
                listBoxCenterCorner.ScrollIntoView(item);
            }
            
            
            try
            {
                LogAndPrint("========== Start Golden Sample Measurement ==========");
                // 在這裡加入 Erase 指令！測量前先清空 i1display Flash 區塊
                LogAndPrint("正在清除 AD Board 上的 i1display 舊資料...");
                await protocol.GammaErase_i1display();
                LogAndPrint("清除完成，開始測量...");
                // =========================================================================================
                // Part 1: 量測 Native R/G/B (Gain 128)
                // 目的: 產生 s_i1_rY, s_i1_gY, s_i1_bY
                // =========================================================================================
                LogAndPrint("\n開始測量 red/green/blue gamma 曲線 (已扣除黑點)...");
                LogAndPrint("量測 Native Panel");

                // 1. 重置為 Native 狀態 (Gain 128, Max Brightness)
                await protocol.ResetCTandRGB128();
                await protocol.ResetCTandGamma();
                await protocol.SetBrightnessMax();
                await Task.Delay(1000); // 等待亮度穩定

                // 2. 量測全黑點 (Native Black)
                whiteBox.Fill = Brushes.Black;
                blackWindow.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
                await Task.Delay(1000);

                SELECTED_SENSOR.MeasYxyz(out SensorMeasureYxy_t overallBlackMeasurement);
                double blackPointY = overallBlackMeasurement.Y;
                LogAndPrint($"Native Black Y: {blackPointY:F4}");
                Addi1goldenLumLog("Native_Black", blackPointY, "Black");

                int sampleNum = 32;

                // 準備三個 List 暫存數據，因為要等迴圈跑完才能整包傳送
                List<int> nativeDataR = new List<int>();
                List<int> nativeDataG = new List<int>();
                List<int> nativeDataB = new List<int>();

                // 3. 開始掃描 R, G, B
                for (int i = 0; i < sampleNum; i++)
                {

                    int level = (i * 255) / (sampleNum - 1);
                    byte bLevel = (byte)level;

                    // --- Measure Red ---
                    whiteBox.Fill = new SolidColorBrush(Color.FromRgb(bLevel, 0, 0));
                    blackWindow.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
                    await Task.Delay(300);

                    SELECTED_SENSOR.MeasYxyz(out SensorMeasureYxy_t r_raw);
                    ushort valR = (ushort)(r_raw.Y * 100.0);
                    nativeDataR.Add(valR);
                    LogAndPrint($"[Hex] Native_R Lv{level}: 0x{valR & 0xFF:X2} 0x{(valR >> 8) & 0xFF:X2} (Y={r_raw.Y:F2})");

                    // double rLum = Math.Max(0, r_raw.Y - blackPointY); // 計算用

                    Addi1goldenLumLog("", r_raw.Y, $"Level {level}_r_raw");
                    // Log for Python Parser 
                    LogAndPrint($"Level {level} r_raw : x={r_raw.x:F4}, y={r_raw.y:F4}, Y={r_raw.Y:F4}");
                    calibrationPage?.AddCalibrationLog($"Level {level} r_raw", r_raw.x.ToString("F4"), r_raw.y.ToString("F4"), r_raw.Y.ToString("F4"));

                    UpdateUIAction?.Invoke(r_raw); // 更新 UI 數值

                    // --- Measure Green ---
                    whiteBox.Fill = new SolidColorBrush(Color.FromRgb(0, bLevel, 0));
                    blackWindow.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
                    await Task.Delay(300);

                    SELECTED_SENSOR.MeasYxyz(out SensorMeasureYxy_t g_raw);
                    ushort valG = (ushort)(g_raw.Y * 100.0);
                    nativeDataG.Add(valG);
                    LogAndPrint($"[Hex] Native_G Lv{level}: 0x{valG & 0xFF:X2} 0x{(valG >> 8) & 0xFF:X2} (Y={g_raw.Y:F2})");
                    LogAndPrint($"Level {level} g_raw : x={g_raw.x:F4}, y={g_raw.y:F4}, Y={g_raw.Y:F4}");
                    Addi1goldenLumLog("", g_raw.Y, $"Level {level}_g_raw");
                    calibrationPage?.AddCalibrationLog($"Level {level} g_raw", g_raw.x.ToString("F4"), g_raw.y.ToString("F4"), g_raw.Y.ToString("F4"));

                    UpdateUIAction?.Invoke(g_raw);

                    // --- Measure Blue ---
                    whiteBox.Fill = new SolidColorBrush(Color.FromRgb(0, 0, bLevel));
                    blackWindow.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
                    await Task.Delay(300);

                    SELECTED_SENSOR.MeasYxyz(out SensorMeasureYxy_t b_raw);
                    ushort valB = (ushort)(b_raw.Y * 100.0);
                    nativeDataB.Add(valB);
                    LogAndPrint($"[Hex] Native_B Lv{level}: 0x{valB & 0xFF:X2} 0x{(valB >> 8) & 0xFF:X2} (Y={b_raw.Y:F2})");
                    LogAndPrint($"Level {level} b_raw : x={b_raw.x:F4}, y={b_raw.y:F4}, Y={b_raw.Y:F4}");
                    calibrationPage?.AddCalibrationLog($"Level {level} b_raw", b_raw.x.ToString("F4"), b_raw.y.ToString("F4"), b_raw.Y.ToString("F4"));


                    Addi1goldenLumLog("", b_raw.Y, $"Level {level}_b_raw");
                    UpdateUIAction?.Invoke(b_raw);
                }
                
                // Native 迴圈結束，開始傳送資料
                byte ctCodeForNative = 0x00; // 為了讓 FW 判斷為 CTMode 0

                LogAndPrint($"→ 寫入 AD Board: Native_R (Target: CT3, Ch0)...");
                await Board_Store_I1_Data_Async(nativeDataR, ctCodeForNative, 0, "Native_R");

                LogAndPrint($"→ 寫入 AD Board: Native_R (Target: CT3, Ch1)...");
                await Board_Store_I1_Data_Async(nativeDataG, ctCodeForNative, 1, "Native_G");

                LogAndPrint($"→ 寫入 AD Board: Native_R (Target: CT3, Ch2)...");
                await Board_Store_I1_Data_Async(nativeDataB, ctCodeForNative, 2, "Native_B");
                ////存NativeRGB.txt
                GlobalCalibData.Native_R = nativeDataR.ToArray();//0615 ian add 
                GlobalCalibData.Native_G = nativeDataG.ToArray();
                GlobalCalibData.Native_B = nativeDataB.ToArray();
                List<string> outputLines = new List<string>();

                // 1. 修改標題列：第一欄改為 Level (灰階值)
                outputLines.Add("Level\tR\tG\tB");

                // 2. 利用迴圈將 32 階的資料寫入
                for (int i = 0; i < GlobalCalibData.Native_R.Length; i++)
                {

                    int level = i;

                    // 把 cctValues[i] 換成 level
                    string line = $"{level}\t{GlobalCalibData.Native_R[i]}\t{GlobalCalibData.Native_G[i]}\t{GlobalCalibData.Native_B[i]}";
                    outputLines.Add(line);
                }

                // 3. 寫入檔案
                string nativePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NativeRGB.txt");
                System.IO.File.WriteAllLines(nativePath, outputLines);
                // =========================================================================================
                // Part 2: 量測  Gamma (5400K, 6500K, 9300K)
                // 目的: 產生 s_i1_fixY_5400, s_i1_fixY_6500, s_i1_fixY_9300
                // =========================================================================================

                var targets = new List<(int cct, byte r, byte g, byte b, byte targetCh, byte ctCode)>
        {
            (5400, 128, 126, 124, 0,0x36), // 5400K -> 映射到 Ch 0 (模擬 Red)
            (6500, 125, 128, 126, 1,0x41), // 6500K -> 映射到 Ch 1 (模擬 Green)
            (9300, 104, 115, 129, 2,0x5D)  // 9300K -> 映射到 Ch 2 (模擬 Blue)
        };

                byte cctBaseChannel = 3;
                byte ctCodeForCCT = 0x36;
                for (int tIdx = 0; tIdx < targets.Count; tIdx++)
                {
                    var t = targets[tIdx];
                    byte currentCh = (byte)(cctBaseChannel + tIdx); // ch = 3, 4, 5

                    LogAndPrint($"\n[{t.cct}K] 量測 32 階灰階開始");
                    calibrationPage?.AddCalibrationLog($"Start Measure {t.cct}K", "", "", "");
                    blackWindow.textblockStatus.Text = $"Measuring {t.cct}K...";

                    await Task.Delay(500);


                    whiteBox.Fill = Brushes.Black;
                    blackWindow.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
                    await Task.Delay(300); // 稍微久一點確保黑穩定

                    SELECTED_SENSOR.MeasYxyz(out SensorMeasureYxy_t resBlack);
                    double currentCctBlackY = resBlack.Y;
                    // LogAndPrint($"[{t.cct}K] Black Y: {currentCctBlackY:F4}");
                    Addi1goldenLumLog($"{t.cct}K", resBlack.Y, "Black");

                    List<int> cctData = new List<int>();


                    for (int lvl = 0; lvl < sampleNum; lvl++)
                    {
                        int gray = (lvl * 255) / (sampleNum - 1);
                        byte g = (byte)gray;

                        whiteBox.Fill = new SolidColorBrush(Color.FromRgb(g, g, g));
                        blackWindow.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
                        await Task.Delay(500);

                        bool ok = SELECTED_SENSOR.MeasYxyz(out SensorMeasureYxy_t res);
                        if (!ok)
                        {
                            LogAndPrint($"  ✖ Lv{lvl} 測量失敗");
                            continue;
                        }
                        //   Addi1goldenLumLog($"{t.cct}K", res.Y, $"Gray {gray}");
                        Addi1goldenLumLog("", res.Y, $"Level {gray}_gray_{t.cct}");
                        // 收集數據 (float Y * 100 -> int)
                        ushort val = (ushort)(res.Y * 100.0);
                        cctData.Add((int)(res.Y * 100.0));

                        // Hex Log: Low Byte 前, High Byte 後
                        LogAndPrint($"[Hex] {t.cct}K Lv{gray}: 0x{val & 0xFF:X2} 0x{(val >> 8) & 0xFF:X2} (Y={res.Y:F2})");

                        // Log for Python Parser
                        LogAndPrint($"Level {gray}: x={res.x:F4}, y={res.y:F4}, Y={res.Y:F4}");
                        calibrationPage?.AddCalibrationLog($"Level {gray}", res.x.ToString("F4"), res.y.ToString("F4"), res.Y.ToString("F4"));
                    }
                    // 傳送 CCT 數據
                    LogAndPrint($"→ Write {t.cct}K (CT:0x{t.ctCode:X2}, MapCh:{t.targetCh})...");
                    await Board_Store_I1_Data_Async(cctData, t.ctCode, t.targetCh, $"{t.cct}K");
                }
                blackWindow.textblockStatus.Text = "Finish i1 Golden Measurement...";
                LogAndPrint("\n========== Measurement DONE ==========");

                // =========================================================================================
                // Part 3: 存檔邏輯 
                // =========================================================================================
                if (mainWin != null && mainWin.IsDebugMode)
                {
                    string fileName = $"GoldenData_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                    // 使用 AppDomain.CurrentDomain.BaseDirectory 取得執行檔路徑
                    string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                    // 使用 StreamWriter 寫入檔案
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(fullPath, append: false))
                    {
                        writer.Write(logBuilder.ToString());
                    }

                    Console.WriteLine($"[Golden Data] 已經儲存到 {fullPath}");
                    System.Windows.MessageBox.Show($"量測完成！\nLog 已存檔為: {fileName}\n路徑: {fullPath}\n請複製內容給 Python 腳本使用。");
                }
                else
                {
                    System.Windows.MessageBox.Show("量測完成！\n(未開啟 Debug Mode，因此未產生 Log 檔案，請手動複製 Console)");
                }

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"量測發生錯誤: {ex.Message}");
            }
            finally
            {
                // 清理資源
                if (whiteBox != null && blackWindow.canvasRoot.Children.Contains(whiteBox))
                {
                    blackWindow.canvasRoot.Children.Remove(whiteBox);
                }

                buttonCorrect.IsEnabled = true;
                blackWindow.textblockStatus.Text = "Ready";

                // 恢復預設
                await protocol.ResetCTandGamma();
            }


            //260112 wu add to store i1 measure data

            StartTimer_ALC_Correct();
            buttonCorrect.IsEnabled = false;
            buttonCorrect.Content = "Correcting";
            labelCorrectYes.Content = "No";
            var item_alc_correct_descript = new alcMeasItem
            {
                Descript =
                    $"Start\n" +
                    $"ALC Correct",
                Center = "\n- Center -",
                Corner = "\n- Corner -",
                Difference = "\n- Diff -"
            };
            listBoxCenterCorner.Items.Add(item_alc_correct_descript);
            listBoxCenterCorner.ScrollIntoView(item_alc_correct_descript);
        }





















        private async Task Board_Store_I1_Data_Async(List<int> y32, byte ctCode, byte targetCh, string logName)
        {
            if (y32 == null || y32.Count != 32) return;

            var blackWindow = System.Windows.Application.Current.Windows.OfType<BlackWindow>().FirstOrDefault(w => w.IsVisible);
            if (blackWindow == null) return;

            var mainWin = (MainWindow)System.Windows.Application.Current.MainWindow;
            var i2c = mainWin?.GetI2CController();
            if (i2c == null) return;

            // [重要] FW 只接受 Channel 0, 1, 2。這裡做個防呆，避免送出 3,4,5 導致 FW 不理人
            if (targetCh > 2)
            {
                Console.WriteLine($"[Error] Channel {targetCh} 超出 FW 範圍(0-2)，強制改為 0");
                targetCh = 0;
            }

            // 1. 建立 Dummy LUTData
            LUTData dummyLut = new LUTData(256, 65535);

            // 2. 填入數據 (根據 targetCh 塞入對應的 LUT 陣列)
            // 這樣 DDCCI_Set_CommandGammaCT 內部的 switch(ch) 才能抓到資料
            int[] targetBuffer;
            if (targetCh == 0) targetBuffer = dummyLut.RLUT;      // Ch 0 -> 填 R
            else if (targetCh == 1) targetBuffer = dummyLut.GLUT; // Ch 1 -> 填 G
            else targetBuffer = dummyLut.BLUT;                    // Ch 2 -> 填 B

            for (int i = 0; i < y32.Count; i++)
            {
                targetBuffer[i] = y32[i];
            }

            // 3. 執行傳送
            await Task.Run(async () =>
            {
                // [關鍵計算] 
                // 32 點 int = 64 bytes 資料。
                // 每次 I2C 寫入 16 bytes。
                // 64 / 16 = 4 次。
                // 所以 idx 必須是 0, 1, 2, 3。
                // (如果你只跑 2 次，只能存到第 16 階，後面會斷掉)
                int idxMax = 4;

                for (byte idx = 0; idx < idxMax; idx++)
                {
                    byte result;
                    int tries = 0;

                    await blackWindow.Dispatcher.InvokeAsync(() =>
                       blackWindow.textblockStatus.Text = $"Upload {logName} (CT:{ctCode:X2} CH:{targetCh}) Pkt:{idx + 1}/{idxMax}");

                    do
                    {
                        Console.WriteLine($"Burn {logName}, CT=0x{ctCode:X2}, CH={targetCh}, idx={idx}, attempt={tries + 1}");

                        // 固定 GM=0x4E, CT=0x00, 只變動 CH 和 IDX
                        result = i2c.DDCCI_Set_CommandGammaCT(0x4E, ctCode, targetCh, idx, dummyLut);

                        await Task.Delay(300); // 給 FW 一點寫入時間

                        // 針對 Native Mode Checksum 錯位的容錯處理
                        if (result == 255)
                        {
                            Console.WriteLine("[Warn] Checksum error ignored (0x4E mode).");
                            result = 0;
                        }

                        if (result != 0)
                        {
                            Console.WriteLine($" → idx={idx} 失敗 (Code:{result})，第 {tries + 1} 次重試");
                            await Task.Delay(100);
                            tries++;
                        }

                        if (result != 0 && tries >= 3)
                        {
                            System.Windows.MessageBox.Show($"寫入失敗 (Code: {result})，終止 {logName} 寫入!");
                            return;
                        }

                    } while (result != 0 && tries < 3);
                }
            });
        }


        private async Task<BlackWindow> EnsureBlackWindowAsync(BlackWindowSource source)
        {
            // 1) 先找現有的
            var bw = System.Windows.Application.Current.Windows
                .OfType<BlackWindow>()
                .FirstOrDefault(w => w.IsVisible);

            if (bw != null) return bw;

            // 2) 沒有就走跟 Calibration 一樣的 SelectMonitorForm
            var selectForm = new Measure_Tool.SelectMonitorForm();
            if (selectForm.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return null;

            bw = new BlackWindow(selectForm.SelectedScreen, selectForm.SelectedScreenSize, source);
            bw.Show();

            // 3) 等 Loaded/Render，避免 ActualWidth/Height 還沒準備好
            await bw.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            await Task.Delay(200);

            return bw;
        }



        public void getPWM(byte[] commandByteArr)
        {
            MReplyP monitorReplyPackage = new MReplyP(commandByteArr);
            Debug.WriteLine($"Data Hi is {monitorReplyPackage.Data[1]}");
            Debug.WriteLine($"Data Low is {monitorReplyPackage.Data[0]}");
            curr_PWM = monitorReplyPackage.Data[0]; // For 8 bits PWM  ///Derek


            //curr_PWM = (ushort)((monitorReplyPackage.Data[1] << 8) | monitorReplyPackage.Data[0]); // For 8 bits PWM  

            //250905 wu add for 10 bits PWM
            ushort lo = monitorReplyPackage.Data[0];
            ushort hi = monitorReplyPackage.Data[1];

            curr_PWM_full = (ushort)((hi << 8) | lo);   // 0–1023 真正的 PWM 值
            curr_PWM = (byte)(curr_PWM_full >> 2);      // 如果還要保留給 8-bit 使用，再右移 2bit

            //curr_PWM_full = (ushort)((monitorReplyPackage.Data[1] << 8) | monitorReplyPackage.Data[0]);
            //curr_PWM = (byte)(curr_PWM_full >> 2);
            Debug.WriteLine($"Raw Lo=0x{lo:X2}, Hi=0x{hi:X2}, Parsed curr_PWM_full={curr_PWM_full}");
            //250905 wu add for 10 bits PWM
        }

        private void buttonALCConnect_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            //i2cController = new I2CController(mainWindow.myFtdiDevice);

            // Get ALC luminance for each ALC Mode
            byte[] GetALCDetailCommandArray =
               { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_75_SPECIAL_GET_COMMAND, 0x07, 0x04, 0x00, 0x00};
            byte retval, retryCount = 0;
            if (mainWindow.comboModelPort.Text.Contains("I2C"))
            {

                //// 【修改】根據 RadioButton 選擇來決定要實例化哪個 Controller
                //if (RadiobuttonExternal.IsChecked == true)
                //{
                //    var i2cControllerStm = new I2CController_STM(mainWindow.myFtdiDevice);
                //    var i2cController = new I2CController(mainWindow.myFtdiDevice);

                //    do
                //    {
                //        retval = i2cController.DDCCI_Send_Command(GetALCDetailCommandArray, getALCDetail);
                //        Console.WriteLine("Set ALC Connect array: " + BitConverter.ToString(GetALCDetailCommandArray).Replace("-", " "));
                //        Debug.WriteLine($"ret val = {retval}, retryCount = {++retryCount}");
                //    } while (retval != 0 && retryCount <= 10);
                //}
                //else
                //{
                var i2cController = new I2CController(mainWindow.myFtdiDevice);

                do
                {
                    retval = i2cController.DDCCI_Send_Command(GetALCDetailCommandArray, getALCDetail);
                    Console.WriteLine("Set ALCConnect array: " + BitConverter.ToString(GetALCDetailCommandArray).Replace("-", " "));
                    Debug.WriteLine($"ret val = {retval}, retryCount = {++retryCount}");
                } while (retval != 0 && retryCount <= 10);
                //}
                if (retval == 0)
                {
                    buttonCorrect.IsEnabled = true;
                }
            }
        }
        public void getALCDetail(byte[] commandByteArr)
        {

            MReplyP monitorReplyPackage = new MReplyP(commandByteArr);
            if ((byte)monitorReplyPackage.Status == 0xE0)
            {
                ALC_MODE_NUM = monitorReplyPackage.Data[0];
                textBoxALC1Data.Text = (monitorReplyPackage.Data[1] | monitorReplyPackage.Data[2] << 8).ToString();
                textBoxALC2Data.Text = (monitorReplyPackage.Data[3] | monitorReplyPackage.Data[4] << 8).ToString();
                textBoxALC3Data.Text = (monitorReplyPackage.Data[5] | monitorReplyPackage.Data[6] << 8).ToString();
                textBoxALC4Data.Text = (monitorReplyPackage.Data[7] | monitorReplyPackage.Data[8] << 8).ToString();
                textBoxALC5Data.Text = (monitorReplyPackage.Data[9] | monitorReplyPackage.Data[10] << 8).ToString();

                //wu  250909 add for check ALC_MODE intertnal or external
                byte data0 = monitorReplyPackage.Data[0];

                ALC_MODE_NUM = (byte)(data0 & 0x0F);          //4bit:mode
                ALC_MODE_CONNECTION = ((data0 >> 7) & 0x01) == 1;  // external = 1: STM32,  external = 0: internal(AD board)

                Debug.WriteLine(ALC_MODE_CONNECTION ? $"ALC Mode is external (STM32), mode count={ALC_MODE_NUM}" : $"ALC Mode is internal (AD board), mode count={ALC_MODE_NUM}");

                if (ALC_MODE_CONNECTION == true)  //Tool choose connection mode instead user click
                {
                    RadiobuttonExternal.IsChecked = true;
                    RadiobuttonExternal.IsEnabled = false;
                    RadiobuttonInternal.IsEnabled = false;
                }
                else
                {
                    RadiobuttonInternal.IsChecked = true;
                    RadiobuttonExternal.IsEnabled = false;
                    RadiobuttonInternal.IsEnabled = false;
                }




                //  ALC_MODE_SELECTED = alcMode;

                if ((monitorReplyPackage.Data[11] | monitorReplyPackage.Data[12] << 8) == 0x0001) //Sensor ID 
                {
                    var item_alc_detail = new alcMeasItem
                    {
                        Descript =
                    $"{ALC_MODE_NUM} ALC mode\n" +
                    $"{textBoxALC1Data.Text}nits\n" +
                    $"{textBoxALC2Data.Text}nits\n" +
                    $"{textBoxALC3Data.Text}nits\n" +
                    $"{textBoxALC4Data.Text}nits\n" +
                    $"{textBoxALC5Data.Text}nits"
                    };
                    listBoxCenterCorner.Items.Add(item_alc_detail);
                    listBoxCenterCorner.ScrollIntoView(item_alc_detail);

                    labelALCYes.Content = "Yes";
                    buttonCorrect.IsEnabled = true;
                }
                else
                {
                    var item_alc_detail = new alcMeasItem
                    {
                        Descript =
                    $"Can't find\n" +
                    $"ALC sensor"
                    };
                    listBoxCenterCorner.Items.Add(item_alc_detail);
                    listBoxCenterCorner.ScrollIntoView(item_alc_detail);

                    labelALCYes.Content = "No";
                    buttonCorrect.IsEnabled = false;
                }
            }
        }

        public void getALCRGB(byte[] commandByteArr)
        {
            MReplyP monitorReplyPackage = new MReplyP(commandByteArr);
            if (monitorReplyPackage.OPCode == OPCode.ONYX_CMD_75_SPECIAL_GET_COMMAND &&
                monitorReplyPackage.VCCode == VCCode.ONYX_FCODE_03_GET_ALC_SENSOR &&
                monitorReplyPackage.Status == Status.STATUS_SUCCESS)
            {
                Debug.WriteLine($"Green Data Word 0x{monitorReplyPackage.Data[1]}{monitorReplyPackage.Data[0]}");
                int Red_count = Convert.ToInt32(monitorReplyPackage.Data[1] << 8 | monitorReplyPackage.Data[0]);
                int Green_count = Convert.ToInt32(monitorReplyPackage.Data[3] << 8 | monitorReplyPackage.Data[2]);
                int Blue_count = Convert.ToInt32(monitorReplyPackage.Data[5] << 8 | monitorReplyPackage.Data[4]);
                double[,] Corr_Coeff_Matrix = {
                    { 0.000001056,  0.00010221,  -0.000061504 },
                    { -0.00001550,  0.00010266,  -0.000047278 },
                    { -0.00007810,  0.00009523,   0.00000994 }
                };
                double color_sensor_X = Corr_Coeff_Matrix[0, 0] * Red_count + Corr_Coeff_Matrix[0, 1] * Green_count + Corr_Coeff_Matrix[0, 2] * Blue_count;
                double color_sensor_Y = Corr_Coeff_Matrix[1, 0] * Red_count + Corr_Coeff_Matrix[1, 1] * Green_count + Corr_Coeff_Matrix[1, 2] * Blue_count;
                double color_sensor_Z = Corr_Coeff_Matrix[2, 0] * Red_count + Corr_Coeff_Matrix[2, 1] * Green_count + Corr_Coeff_Matrix[2, 2] * Blue_count;
                Debug.WriteLine($"Count of Color Sensor Red = {Red_count}, Green = {Green_count}, Blue = {Blue_count}");
                double color_sensor_x = color_sensor_X / (color_sensor_X + color_sensor_Y + color_sensor_Z);
                double color_sensor_y = color_sensor_Y / (color_sensor_X + color_sensor_Y + color_sensor_Z);
                Debug.WriteLine($"CIE1931 of Color Sensor X = {color_sensor_X}, Y = {color_sensor_Y}, Z = {color_sensor_Z}");
                Debug.WriteLine($"CIE1931 of Color Sensor Y = {color_sensor_Y}, x = {color_sensor_x}, y = {color_sensor_y}");
                // IT(100ms) RGB_GAIN(x1) RESOLUTION(0.0420) at PD_DIV(2/2 PD USED), Lux = resolution * green channel count
                // Use linear regression to estimate Luminance
                ALC_SENSOR_LUMINANCE = (0.0420 * Green_count) * 0.87738 - 0.54;
                Debug.WriteLine($"VEML on STM:  {ALC_SENSOR_LUMINANCE}");
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            SELECTED_SENSOR = mainWindow.ConnectedSensor;
            textBlockModule.Text = $"Sensor: {SELECTED_SENSOR.SensorName}";
        }

        private async void buttonSTMTest_Click(object sender, RoutedEventArgs e)
        {

            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;

            byte AppStatus = 0;
            var i2cControllerStm = new I2CController_STM(mainWindow.myFtdiDevice);
            var i2cController = new I2CController(mainWindow.myFtdiDevice);
            AppStatus = i2cControllerStm.I2C_ConfigureMpsse();
            AppStatus = i2cControllerStm.DDCCI_Null_Message();// zh 250212


            byte[] SetPCBrightnessMaxCommandArray =
                          { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_0A_SET_BRIGHTNESS, 0x00, 0x64 };
            byte retval_SetPCBrightnessMax, retryCountPC_SetBrightnessMa = 0;

            do
            {
                retval_SetPCBrightnessMax = i2cController.DDCCI_Send_Command(SetPCBrightnessMaxCommandArray, null, false);
                Console.WriteLine("Set Brightness Max array: " + BitConverter.ToString(SetPCBrightnessMaxCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_SetPCBrightnessMax}, retryCount = {++retryCountPC_SetBrightnessMa}");  // Record retry times
            } while (retval_SetPCBrightnessMax != 0 && retryCountPC_SetBrightnessMa <= 10);
            Console.WriteLine($"PC Brightness 已經設定為Max  !!!");

            await Task.Delay(300);


            byte[] SetSTMBrightnessMaxCommandArray =
                   { (byte)Destination.ONYX_MONITOR_STM_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_0A_SET_BRIGHTNESS, 0x00, 0x64 };
            byte retval_SetSTMBrightnessMax, retryCountSTM_SetBrightnessMa = 0;

            do
            {
                retval_SetSTMBrightnessMax = i2cControllerStm.DDCCI_Send_Command(SetSTMBrightnessMaxCommandArray, null, false);
                Console.WriteLine("Set Brightness Max array: " + BitConverter.ToString(SetSTMBrightnessMaxCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_SetSTMBrightnessMax}, retryCount = {++retryCountSTM_SetBrightnessMa}");  // Record retry times
            } while (retval_SetSTMBrightnessMax != 0 && retryCountSTM_SetBrightnessMa <= 10);
            Console.WriteLine($"STM Brightness 已經設定為Max  !!!");


            await Task.Delay(300);

            // 3. 向 STM32 要求溫度
            byte[] GetSTMTemperatureCommandArray =
                { (byte)Destination.ONYX_MONITOR_STM_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,(byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_14_GET_TEMPERATURE, 0x00, 0x00 };
            byte retval_GetSTMTemp, retryCountTemp_STM = 0;
            do
            {
                retval_GetSTMTemp = i2cControllerStm.DDCCI_Send_Command(GetSTMTemperatureCommandArray, getTemperature_STM, true);
                Console.WriteLine("Get Temperature array (STM): " + BitConverter.ToString(GetSTMTemperatureCommandArray).Replace("-", " "));
                Debug.WriteLine($"ret val = {retval_GetSTMTemp}, retryCount = {++retryCountTemp_STM}");  // Record retry times
            } while (retval_GetSTMTemp != 0 && retryCountTemp_STM <= 10);
            Console.WriteLine("STM Temperature 已讀取 !!!");



            // Read Color Sensor RGB count 
            byte[] GetALC_SensorCommandArray =
                { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86, (byte)FactoryMode.ONYX_FACTORY_CMD_D0, (byte)OPCode.ONYX_CMD_75_SPECIAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_03_GET_ALC_SENSOR, 0x00, 0x00 };
            byte retval_GetVEML, retry_VEMLCount = 0;

            do
            {
                retval_GetVEML = i2cControllerStm.DDCCI_Send_Command(GetALC_SensorCommandArray, getALCRGB);
                Debug.WriteLine($"ret val = {retval_GetVEML}, retryCount = {++retry_VEMLCount}");
            } while (retval_GetVEML != 0 && retry_VEMLCount <= 10);


        }




    }
}
