using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Markup;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

using FTD2XX_NET;
using ONYX_DataType;
using static MonitorFactoryTool.MainWindow;
using MReqP = ONYX_DataType.MonitorRequestPackage;
using MReplyP = ONYX_DataType.MonitorReplyPackage;
using System.CodeDom;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Xml;
using System.ComponentModel.Design;
using System.Diagnostics;

using System.Configuration;
using System.Management.Instrumentation;
using MonitorFactoryTool.FactoryGrid;
using System.Windows.Media.Media3D;

//240915新增 for I2C
using FDTI_Factory_i2c;
using OnyxSensor;
using MonitorFactoryTool.Pages;
using ONYX.Utilities;


namespace MonitorFactoryTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                      Definitions                                           #####################
        //###################################################################################################################################
        //###################################################################################################################################

        // ###### Driver defines ######
        FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;

        // Serial port define
        readonly private SerialPort serialPort1;
        public static TaskCompletionSource<bool> _receiveCompletionSource;

        // FTDI USB plug/unplug define
        static bool DeviceOpen = false;

        uint devcount = 0;

        // Create new instance of the FTDI device class
        public FTDI myFtdiDevice = new FTDI();

        // Create for FTDI USB unplug observe
        HotPlug hotPlug = new HotPlug();


        // Create for XML command add function
        private string xmlCommand = "";


        //public event Action<Item> OnItemSelected;

        // Link to Factory Page
        private Factory factoryPage;
        private static Factory factoryWindow = new Factory();
        //pattern always on top
        public static event Action<bool> AlwaysOnTopChanged;

        //240915 for I2C
        private I2CController i2cController;

        public static MainWindow Instance { get; private set; } //0505 RGB test  
        public Calibration CalibrationPage => this.Calibration.Content as Calibration; //0508 測試 gamma calibration 新增的 

        public SensorBase ConnectedSensor { get; set; }//0630 測試calibprocess 共用sensor 新增的 


        private bool isDebugMode = false; //250702 wu add 檢查Ddubug mode 決定要不要log LUT txt
        public bool IsDebugMode => isDebugMode;

        public bool IsBoardConnected { get; private set; } = false;//250710 wu add 檢查board sensor 連接
        public bool IsSensorConnected { get; private set; } = false;

        // I2C clock

        // I2C define
        static byte AppStatus = 0;
        private string SNNumber = ""; //250704  把SNNumber 換成 Module name

        private string FWversion = "";
        private string InputSource = "";

        private Gamma _gammaWindow;

        public bool HasReadFWVersion { get; set; } = false;  //260413  wu add 為了解決 Blackwwindow 第二次開啟會遇到 Get FW 問題
        public string CachedFWVersion { get; set; } = string.Empty;

        public MainWindow(String param)//zh 250701
        {
            InitializeComponent();
            serialPort1 = new SerialPort();
            serialPort1.DataReceived += serialPort1_DataReceived;

            // 240915新增  初始化 I2C 控制器
            i2cController = new I2CController(myFtdiDevice);

            Instance = this;  // 把自己設為全域存取點 ///0505 RGBｔｅｓｔ　
            this.Closing += MainWindow_Closing;

            //zh 250701>
            if (param.Equals("-Ddebug"))
            {
                isDebugMode = true;
                MessageBox.Show("Tool is Debug...");
            }
            //zh 250701<
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // For FTDI USB unplug observe
            hotPlug.Register(HotPlugDeviceInserted, HotPlugDeviceRemoved);

            // Serial port rate default
            comboModelPortRate.Text = "3000000";

            // 加载 Factory 页面
            LoadFactoryPage();

        }
        /////RGB  測試用 
        public I2CController GetI2CController()
        {
            return i2cController;
        }


        private void LoadFactoryPage()
        {
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Process.GetCurrentProcess().Kill(); // 關掉之後不會再背景執行，避免重開之後i2c 連不上的問題 
            MessageBox.Show("Window is closing...");


            factoryPage = FrameFactory.Content as Factory;

            if (factoryPage == null)
            {
                Debug.WriteLine("Failed to load Factory page.");
            }
            else
            {
                Debug.WriteLine("Factory page is not null.");
            }

        }


        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                      Button Click Event                                    #####################
        //###################################################################################################################################
        //###################################################################################################################################

        private void buttonPattern_Click(object sender, RoutedEventArgs e)
        {
            Pages.Pattern pattern = new Pages.Pattern();
            pattern.Visibility = Visibility.Visible;
        }
        private async void buttonModelConnect_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;

            var calibrationPage = this.Calibration.Content as Calibration;
            var ALCPage = this.ALC.Content as ALC;

            var controller = this.GetI2CController();  //wu 250715 add 
            if (controller == null)
            {
                MessageBox.Show("I2C Controller 尚未初始化");
                return;
            }

            if (comboModelPortRate.Text == "400 Kbit/s")
                controller.ClockDivisor = 74;
            else
            {
                controller.ClockDivisor = 59;

            }
            Console.WriteLine("i2c 正在使用的速率是 ClockDivisor: " + controller.ClockDivisor + "  " + comboModelPortRate.Text);


            if (button.Content.ToString() == "Connect")
            {

                // 執行連線操作
                if (this.comboModelPort.Text.CompareTo(System.String.Empty) != 0)
                {
                    if (this.comboModelPort.Text.Contains("COM"))
                    {
                        // Set port name to that selected in the combobox
                        serialPort1.PortName = this.comboModelPort.Text;

                        // Set port parameters
                        if (this.comboModelPortRate.Text.CompareTo(System.String.Empty) != 0)
                        {
                            try
                            {
                                serialPort1.BaudRate = Convert.ToInt32(comboModelPortRate.Text.Trim(' ', 'B', 'a', 'u', 'd'));
                                serialPort1.DataBits = 8;
                                serialPort1.StopBits = System.IO.Ports.StopBits.One;
                                serialPort1.Parity = System.IO.Ports.Parity.None;
                                serialPort1.Handshake = System.IO.Ports.Handshake.None;
                                // Try to open the serial port
                                try
                                {
                                    serialPort1.Open();
                                    hotPlug.Register(HotPlugDeviceInserted, HotPlugDeviceRemoved);
                                    // If we have successfully opened the port, enable/disable buttons
                                    if (serialPort1.IsOpen)
                                    {
                                        button.Content = "Disconnect";
                                        serialPort1.RtsEnable = true;
                                        serialPort1.DtrEnable = true;

                                        // Set write timeout
                                        serialPort1.WriteTimeout = 5000;

                                        // Disable combo box
                                        comboModelPort.IsEnabled = false;
                                        comboModelPortRate.IsEnabled = false;

                                        // Get model informations
                                        try
                                        {
                                            getModelInformations();
                                        }
                                        catch (System.Exception)
                                        {
                                            MessageBox.Show("Failed to write to port.");
                                        }
                                    }
                                }
                                catch (System.Exception)
                                {
                                    MessageBox.Show("Failed to open selected port.\nIs it open in another application?\n(" + ")");
                                }
                            }
                            catch
                            {
                                MessageBox.Show("Please select rate!");
                            }
                        }
                    }
                    else if (this.comboModelPort.Text.Contains("I2C"))
                    {
                        bool DeviceInit = false;
                        uint i2cNum = UInt16.Parse(this.comboModelPort.Text.Remove(0, 3));
                        uint i2cIndex = 0 + 2 * i2cNum;
                        // Try to open the I2C port
                        ftStatus = myFtdiDevice.OpenByIndex(i2cIndex); // Opening I2C port 

                        // Update the Status text line
                        if (ftStatus == FTDI.FT_STATUS.FT_OK)
                        {
                            DeviceOpen = true;
                            buttonModelConnect.Content = "Disconnect";

                            // TabItemFactory.IsEnabled = true; //250709 wu add enable Factory page
                            UpdateBoardConnection(true);   // //250710  wu add  Factory 會自動被打開

                            // For FTDI USB unplug observe
                            hotPlug.Register(HotPlugDeviceInserted, HotPlugDeviceRemoved);
                        }
                        else
                        {
                            DeviceOpen = false;
                        }

                        if (DeviceOpen == true)
                        {
                            DeviceInit = true;
                            AppStatus = i2cController.I2C_ConfigureMpsse();

                            byte retval_SN, retryCount_SN = 0;
                            byte retval_FW, retryCount_FW = 0;
                            byte retval_IS, retryCount_IS = 0;



                            if (AppStatus != 0)
                            {
                                SetTheTextToListBoxCommand("Failed I2c Init");
                                DeviceInit = false;
                            }
                            else
                            {
                                SetTheTextToListBoxCommand("Open I2c");
                                DeviceInit = true;
                                // Disable combo box
                                comboModelPort.IsEnabled = false;
                                comboModelPortRate.IsEnabled = false;
                                AppStatus = i2cController.DDCCI_Null_Message();// zh 250212

                                /////SNNumber
                                byte[] GetSNNumberCommandArray =
                                           { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND, 0x07, (byte)VCCode.         ONYX_FCODE_12_GET_SERIAL_NUMBER      , 0x00, 0x00 };

                                await Task.Delay(100); //zh251128  add


                                if (comboModelPort.Text.Contains("I2C"))
                                {
                                    do
                                    {
                                        retval_SN = i2cController.DDCCI_Send_Command(GetSNNumberCommandArray, getSNNumber);
                                        Debug.WriteLine($"ret val = {retval_SN}, retryCount = {++retryCount_SN}");  // Record retry times
                                    } while (retval_SN != 0 && retryCount_SN <= 10);
                                }

                                await Task.Delay(100);  //wu250715 add 

                                ///Firmware Version
                                byte[] GetFirmwareCommandArray =
                                                { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND, 0x07, (byte)VCCode.      ONYX_FCODE_13_GET_FIRMWARE_VERSION  , 0x00, 0x00 };

                                //                        byte[] GetFirmwareCommandArray =
                                //              { (byte)Destination.ONYX_MONITOR_STM_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                                //(byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_13_GET_FIRMWARE_VERSION, 0x00, 0x00 };

                                await Task.Delay(100); //wu250815 add   

                                if (comboModelPort.Text.Contains("I2C"))
                                {
                                    do
                                    {
                                        retval_FW = i2cController.DDCCI_Send_Command(GetFirmwareCommandArray, getFirmwareVersion);
                                        Debug.WriteLine($"ret val = {retval_FW}, retryCount = {++retryCount_FW}");  // Record retry times
                                    } while (retval_FW != 0 && retryCount_FW <= 10);
                                }

                                await Task.Delay(100); //wu250715 add 

                                /////InputSource
                                byte[] GetInputSourceCommandArray =
                                             { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_05_GET_INPUT_SOURCE     , 0x00, 0x00 };

                                await Task.Delay(100); //wu250815 add   


                                if (comboModelPort.Text.Contains("I2C"))
                                {
                                    do
                                    {
                                        retval_IS = i2cController.DDCCI_Send_Command(GetInputSourceCommandArray, getInputSource);
                                        Debug.WriteLine($"ret val = {retval_IS}, retryCount = {++retryCount_IS}");  // Record retry times
                                    } while (retval_IS != 0 && retryCount_IS <= 10);
                                }

                            }


                            if (retryCount_SN >= 10 && retryCount_IS >= 10 && retryCount_FW >= 10)   //260421 wu add for checking isp board cable connection
                            {
                                MessageBox.Show("Connect Failed! Please check cable!");
                                buttonModelConnect.Content = "Connect";
                                comboModelPort.IsEnabled = true;
                                comboModelPortRate.IsEnabled = true;
                                DeviceOpen = false;
                                UpdateBoardConnection(false);

                                try
                                {
                                    if (myFtdiDevice != null && myFtdiDevice.IsOpen)
                                    {
                                        myFtdiDevice.Close();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Close FTDI Device Error: {ex.Message}");
                                }
                            }
                            else
                            {
                                DeviceOpen = true;
                                UpdateBoardConnection(true);
                            }
                        }
                    }
                }
            }
            else
            {

                // 執行斷線操作

                // Try to close the serial port
                try
                {
                    // Close the serial port
                    if (serialPort1.IsOpen)
                    {
                        serialPort1.DiscardInBuffer();
                        serialPort1.Close();
                    }
                    button.Content = "Connect";
                    //TabItemFactory.IsEnabled = false; //250709 wu add enable Factory page
                    //TabItemALC.IsEnabled = false; //250709 wu add enable Factory page
                    //TabItemCalibration.IsEnabled = false; //250709 wu add enable Factory page

                    UpdateBoardConnection(false);  // Factory / ALC / Calib 全關

                    calibrationPage.labelModelNameContent.Content = "XXXXX";
                    ALCPage.labelALCModelName.Content = "XXXXX";

                    // Close the FTDI device and then close the window
                    myFtdiDevice.Close();

                    // For FTDI USB unplug observe close
                    hotPlug.Unregister();


                }
                catch (System.Exception)
                {
                    MessageBox.Show("Failed to close port.");
                }
                finally
                {
                    UpdateBoardConnection(false);  // Factory / ALC / Calib 全關



                    // If we have successfully closed the port, enable/disable buttons


                    // Enable combo box
                    comboModelPort.IsEnabled = true;
                    comboModelPortRate.IsEnabled = true;

                    // Clear combo box
                    this.comboModelPort.Items.Clear();
                    this.comboModelPortRate.Items.Clear();
                    // Clear serial number 
                    this.textBoxModelSerialNumber.Text = "NA";
                    // Clear input source 
                    this.textBoxModelInputSource.Text = "NA";
                    // Clear firmware version
                    this.textBoxModelFirmwareVersion.Text = "NA";
                }
            }


        }

        public void getSNNumber(byte[] commandByteArr)
        {
            var calibrationPage = this.Calibration.Content as Calibration;
            var ALCPage = this.ALC.Content as ALC;


            MReplyP monitorReplyPackage = new MReplyP(commandByteArr);
            if (monitorReplyPackage.OPCode == OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND && monitorReplyPackage.Status == Status.STATUS_SUCCESS)
            {

                // 轉換 Data 陣列為 HEX 字串
                string hexString = BitConverter.ToString(monitorReplyPackage.Data);
                string cleanHexString = hexString.Replace("-", "");

                // 將 HEX 轉 ASCII (序列號)
                SNNumber = HexToAscii(cleanHexString);

                Debug.WriteLine("SNNUMBER is " + SNNumber);

                calibrationPage.labelModelNameContent.Content = SNNumber;  //wu add 連接board 之後更新model name 到page 裡面
                ALCPage.labelALCModelName.Content = SNNumber;

                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    if (!string.IsNullOrEmpty(SNNumber))
                        mainWindow.textBoxModelSerialNumber.Text = SNNumber;
                }
            }
        }
        public void getFirmwareVersion(byte[] commandByteArr)
        {
            MReplyP monitorReplyPackage = new MReplyP(commandByteArr);
            if (monitorReplyPackage.OPCode == OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND &&
                monitorReplyPackage.Status == Status.STATUS_SUCCESS &&
                monitorReplyPackage.VCCode == VCCode.ONYX_FCODE_13_GET_FIRMWARE_VERSION)
            {
                string hexString = BitConverter.ToString(monitorReplyPackage.Data).Replace("-", "");
                string FWversion = HexToAscii(hexString);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    //mainwindow
                    var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.CachedFWVersion = FWversion;
                        mainWindow.HasReadFWVersion = true;
                        mainWindow.textBoxModelFirmwareVersion.Text = FWversion;
                    }

                    foreach (System.Windows.Window window in System.Windows.Application.Current.Windows)
                    {
                        if (window is MonitorFactoryTool.Pages.BlackWindow blackWindow)
                        {
                            if (blackWindow.ADBoard_FW != null)
                            {
                                blackWindow.ADBoard_FW.Text = FWversion;
                            }
                        }
                    }
                });


                Debug.WriteLine("FWversion is " + FWversion);
            }
        }
        public void getInputSource(byte[] commandByteArr)
        {
            MReplyP monitorReplyPackage = new MReplyP(commandByteArr);
            if (monitorReplyPackage.OPCode == OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND &&
                monitorReplyPackage.Status == Status.STATUS_SUCCESS &&
                monitorReplyPackage.VCCode == VCCode.ONYX_FCODE_05_GET_INPUT_SOURCE)
            {
                string hexString = BitConverter.ToString(monitorReplyPackage.Data).Replace("-", "");
                string InputSource = HexToAscii(hexString);


                // **取得 Input Source (Data 最後一個 byte)**
                string inputSourceHex = monitorReplyPackage.Data[monitorReplyPackage.Data.Length - 1].ToString("X2");
                string inputSourceText;

                switch (inputSourceHex)
                {
                    case "00":
                        inputSourceText = "VGA";
                        break;
                    case "01":
                        inputSourceText = "HDMI";
                        break;
                    case "02":
                        inputSourceText = "DP";
                        break;
                    case "03":
                        inputSourceText = "USBC";
                        break;
                    case "05":
                        inputSourceText = "DVI";
                        break;
                    default:
                        inputSourceText = "Unknown";
                        break;
                }

                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.textBoxModelInputSource.Text = inputSourceText;
                }

                Debug.WriteLine("InputSource is " + InputSource);
            }
        }

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


        private void buttonModelCommandClear_Click(object sender, RoutedEventArgs e)
        {
            listBoxCommand.Items.Clear();
        }

        private void buttonModelCommandImport_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "XMLFILE"; // Default file name
            dialog.DefaultExt = ".xml"; // Default file extension
            dialog.Filter = "(.xml)|*.xml"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName; // This filename contains the full archive path
                LoadXmlFile(filename);
            }
        }


        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                      Combobox DropDown Event                               #####################
        //###################################################################################################################################
        //###################################################################################################################################

        private void comboModelPort_Loaded(object sender, EventArgs e)
        {
            // Init combobox item
            this.comboModelPort.Items.Clear();
            this.comboModelPort.Text = "";
            this.comboModelPort.Items.Add("NA");
            this.comboModelPort.SelectedItem = "NA";
        }

        private void comboModelPortRate_Loaded(object sender, EventArgs e)
        {
            // Init combobox item
            this.comboModelPortRate.Items.Clear();
            this.comboModelPortRate.Text = "";
            this.comboModelPortRate.Items.Add("NA");
            this.comboModelPortRate.SelectedItem = "NA";
        }

        private void comboModelPort_DropDownOpened(object sender, EventArgs e)
        {
            // Clear combobox
            this.comboModelPort.Items.Clear();
            this.comboModelPort.Text = "";

            try
            {
                ftStatus = myFtdiDevice.GetNumberOfDevices(ref devcount);
                SetTheTextToListBoxCommand("FTDI dev count " + devcount);
            }
            catch
            {
                MessageBox.Show("Driver not loaded");
            }

            //AvailableCOMPorts = System.IO.Ports.SerialPort.GetPortNames();
            FTDI.FT_DEVICE_INFO_NODE[] list = new FTDI.FT_DEVICE_INFO_NODE[devcount];

            ftStatus = myFtdiDevice.GetDeviceList(list);
            uint i2cNum = 0;
            foreach (FTDI.FT_DEVICE_INFO_NODE node in list)
            {
                if ((ftStatus = myFtdiDevice.OpenByLocation(node.LocId)) == FTDI.FT_STATUS.FT_OK)
                {
                    try
                    {
                        string comport;
                        string sn;
                        myFtdiDevice.GetCOMPort(out comport);
                        myFtdiDevice.GetSerialNumber(out sn);
                        Console.WriteLine($"Ftdi sn: {sn}");//zh260128 add
                        if (sn == "A" || sn == "ni85WQMJA" || sn == "ni85WQMJB" || sn == "SY3S736QA" || sn == "FT6F0HBHA" || sn == "FT6F0HMTA" ||
                            sn == "FT6F0IHIA" || sn == "FT6F0HR1A" || sn == "FT6F0JH4A" || sn == "FT6F0IRBA" || sn == "SY9NL82SA" || sn == "SY9NL3U0B" || sn == "SY9NL87RA"|| sn == "SY9NL3UWA" || sn == "SY9NL87RB") 
                        {
                            comport = "I2C" + i2cNum;
                            this.comboModelPort.Items.Add(comport);
                            i2cNum++;
                        }
                        else
                        {
                            this.comboModelPort.Items.Add(comport);
                        }
                    }
                    finally
                    {
                        myFtdiDevice.Close();
                    }

                }
            }
        }

        private void comboModelPortRate_DropDownOpened(object sender, EventArgs e)
        {
            // Clear combobox
            this.comboModelPortRate.Items.Clear();
            this.comboModelPortRate.Text = "";
            if (this.comboModelPort.Text.Contains("COM"))
            {
                string[] AvailableRate = new string[] {
                "9600 Baud",
                "14400 Baud",
                "19200 Baud",
                "38400 Baud",
                "56000 Baud",
                "57600 Baud",
                "115200 Baud",
                "128000 Baud",
                "256000 Baud",
                "512000 Baud",
                "1000000 Baud",
                "2000000 Baud",
                "3000000 Baud"};

                foreach (string serialPortRate in AvailableRate)
                {
                    // Add it to the combo box.
                    this.comboModelPortRate.Items.Add(serialPortRate);
                }
            }
            else if (this.comboModelPort.Text.Contains("I2C"))
            {
                string[] AvailableRate = new string[] {
                "100 Kbit/s",
                "400 Kbit/s",
                };

                foreach (string serialPortRate in AvailableRate)
                {
                    // Add it to the combo box.
                    this.comboModelPortRate.Items.Add(serialPortRate);
                }
            }
        }

        private void comboModelPort_DropDownClosed(object sender, EventArgs e)
        {
            // Clear combobox and selected item as NA
            if (comboModelPort.SelectedItem == null)
            {
                this.comboModelPort.Items.Clear();
                this.comboModelPort.Text = "";
                this.comboModelPort.Items.Add("NA");
                this.comboModelPort.SelectedItem = "NA";
            }
        }

        private void comboModelPortRate_DropDownClosed(object sender, EventArgs e)
        {
            // Clear combobox and selected item as NA
            if (comboModelPortRate.SelectedItem == null)
            {
                this.comboModelPortRate.Items.Clear();
                this.comboModelPortRate.Text = "";
                this.comboModelPortRate.Items.Add("NA");
                this.comboModelPortRate.SelectedItem = "NA";
            }
        }

        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                      UART Communication Layer                               #####################
        //###################################################################################################################################
        //###################################################################################################################################

        public int readDataLength;

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            // This event handler fires each time data is received by the serial port.
            // Read available data from the serial port and display it on the form.
            // This event does not run in the UI thread, so need to 
            // use delegate function

            string tempString = "";
            // serialPort1.ReadByte();

            if (serialPort1.BytesToRead > 0)
            {
                for (int i = 0; i < readDataLength; i++)
                {
                    tempString += serialPort1.ReadByte().ToString("X2"); // X2 = 2-digit hex
                    //if (serialPort1.BytesToRead > 0)
                    //    tempString += " ";
                }
            }
            //MessageBox.Show($"{tempString}");
            Debug.WriteLine($"Read: \t{tempString}");
            try
            {
                serialPort_CommandAnalyzer(tempString);
            }
            catch
            {
                Debug.WriteLine("Command analysis failed !!!!!!!!!!");
            }

            /*
            if (tempString.Contains("E0")) // Run next commnad condiction 
            {
                _receiveCompletionSource.SetResult(true);
                factoryWindow.factoryGridCommandIsComplete(StringToByteArray(tempString));           
            }
            else
            {
                factoryWindow.isAckE0 = false;
            }*/

        }
        public byte[] replyCMDtoFactory;
        private void serialPort_CommandAnalyzer(string strCommand)
        {
            DateTime now = DateTime.Now;
            SetTheTextToListBoxCommand($"[{now:HH:mm:ss}] Read:\t{strCommand}");
            this.Dispatcher.BeginInvoke(new Action(delegate
            {
                listBoxCommand.SelectedIndex = listBoxCommand.Items.Count - 1;
                listBoxCommand.ScrollIntoView(listBoxCommand.SelectedItem);
            }));
            var byteArrCommand = StringToByteArray(strCommand);
            MReplyP monitorReplyPackage = new MReplyP(byteArrCommand);
            switch (monitorReplyPackage.Status)
            {
                // Check reply command status 
                case Status.STATUS_SUCCESS:
                    if (byteArrCommand[0] == (byte)Destination.ONYX_MONITOR_HOST_ADDR && byteArrCommand[1] == (byte)Source.ONYX_MONITOR_CLIENT_ADDR)
                    {
                        if (byteArrCommand[3] == (byte)FactoryMode.ONYX_FACTORY_CMD_C0)
                        {
                            SetTheTextToListBoxCommand("Transmission successful");
                            if (byteArrCommand[4] == (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND)
                            {

                            }
                            else if (byteArrCommand[4] == (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND)
                            {
                                replyCMDtoFactory = byteArrCommand;
                                switch (byteArrCommand[6])
                                {
                                    case (byte)VCCode.ONYX_FCODE_05_GET_INPUT_SOURCE:
                                        string tempInputSource;
                                        switch (monitorReplyPackage.Data[0])
                                        {
                                            case (byte)0x00:
                                                tempInputSource = "HDMI";
                                                SetTheTextToListBoxCommand($"Iuput Source:\t{tempInputSource}");
                                                //textBoxModelInputSource.Text = "HDMI";
                                                SetTheTextToTextBoxInputSource(tempInputSource);
                                                break;
                                            case (byte)0x01:
                                                tempInputSource = "DP";
                                                SetTheTextToListBoxCommand($"Iuput Source:\t{tempInputSource}");
                                                textBoxModelInputSource.Text = "HDMI";
                                                SetTheTextToTextBoxInputSource(tempInputSource);
                                                break;
                                            default:
                                                break;
                                        }
                                        break;
                                    case (byte)VCCode.ONYX_FCODE_12_GET_SERIAL_NUMBER:
                                        string tempSerialNumber = System.Text.Encoding.ASCII.GetString(monitorReplyPackage.Data);
                                        SetTheTextToListBoxCommand($"Serial Number:\t{tempSerialNumber}");
                                        textBoxModelSerialNumber.Text = tempSerialNumber;
                                        SetTheTextToTextBoxSerialNumber(tempSerialNumber);
                                        break;
                                    case (byte)VCCode.ONYX_FCODE_13_GET_FIRMWARE_VERSION:
                                        string tempFirmwareVersion = System.Text.Encoding.ASCII.GetString(monitorReplyPackage.Data);
                                        SetTheTextToListBoxCommand($"Firmware Version:\t{tempFirmwareVersion}");
                                        textBoxModelFirmwareVersion.Text = tempFirmwareVersion;
                                        SetTheTextToTextBoxFirmwareVersion(tempFirmwareVersion);
                                        break;
                                    default:

                                        break;
                                }
                            }
                        }
                    }
                    else
                    {

                    }
                    if (_receiveCompletionSource != null)
                    {
                        _receiveCompletionSource.SetResult(true);
                    }
                    factoryWindow.factoryGridCommandIsComplete(StringToByteArray(strCommand));
                    break;
                case Status.STATUS_CHKSUM_NG:
                    SetTheTextToListBoxCommand($"The command sequence checksum incorrect");
                    break;
                case Status.STATUS_TIMEOUT:
                    SetTheTextToListBoxCommand($"Don't replay while time");
                    break;
                case Status.STATUS_INVALID_CMD:
                    SetTheTextToListBoxCommand($"The instructions code don't support");
                    break;
                case Status.STATUS_INVALID_FUNC:
                    SetTheTextToListBoxCommand($"There are instructions but no functions");
                    break;
                default: break;
            }
        }

        public static byte[] StringToByteArray(string hex)
        {
            IEnumerable<string> arr;
            if (hex.Contains(' '))
            {
                arr = hex.Split(' ');
            }
            else
            {
                arr = Enumerable.Range(0, hex.Length / 2).Select(i => hex.Substring(i * 2, 2));
            }

            return arr.Select(x => Convert.ToByte(x, 16)).ToArray();
        }

        public static string ByteArrayToString(byte[] arr)
        {
            return System.Text.Encoding.Default.GetString(arr);
        }

        private async Task UARTSendCommandsAsync(string[] commands)
        {

            foreach (var command in commands)
            {
                SetTheTextToListBoxCommand($"Send:\t{command}");
                switch (command.IndexOf(command))
                {
                    case 0:
                        readDataLength = 21;
                        break;
                    case 1:
                        readDataLength = 21;
                        break;
                    case 2:
                        readDataLength = 10;
                        break;
                }
                await UARTSendCommandAsync(command);
            }
        }


        private async Task UARTSendCommandAsync(string command)
        {
            // Clear buffer 
            serialPort1.DiscardInBuffer();


            // Send command
            var arrSN = StringToByteArray(command);
            serialPort1.Write(arrSN, 0, arrSN.Length);

            // Wait for reception to complete
            _receiveCompletionSource = new TaskCompletionSource<bool>();
            await _receiveCompletionSource.Task;
        }

        private async Task I2CSendCommandAsync(string command)
        {
            var arrSN = StringToByteArray(command);
            byte retval, retryCount = 0;
            do
            {
                retval = i2cController.DDCCI_Send_Command(arrSN);
                Debug.WriteLine($"ret val = {retval}, retryCount = {++retryCount}");
            } while (retval != 0 && retryCount <= 100);
            factoryWindow.factoryGridCommandIsComplete(arrSN);
        }


        public async void getModelInformations()
        {
            try
            {
                //string commandBrightness = "88020010000064000ACA";  //亮度10 
                string commandBrightness = "88020010000064004D8D";  //亮度77 88020010000064004D8D
                readDataLength = 21;
                if (this.comboModelPort.Text.Contains("COM"))
                    await UARTSendCommandAsync(commandBrightness);
                //else 
                //    await I2CSendCommandAsync(commandBrightness);


                string commandSN = "6E5186C073071200001F";
                readDataLength = 21;
                if (this.comboModelPort.Text.Contains("COM"))
                    await UARTSendCommandAsync(commandSN);
                //else
                //    await I2CSendCommandAsync(commandSN);

                string commandFWVER = "6E5186C073071300001E";
                readDataLength = 21;
                if (this.comboModelPort.Text.Contains("COM"))
                    await UARTSendCommandAsync(commandFWVER);
                //else
                //    await I2CSendCommandAsync(commandFWVER);


                string commandIPS = "6E5186C0730705000008";
                readDataLength = 10;
                if (this.comboModelPort.Text.Contains("COM"))
                    await UARTSendCommandAsync(commandIPS);
                //else
                //    await I2CSendCommandAsync(commandIPS);

                //string[] getModelInformationsCMD = {"6E5186C073071200001F", "6E5186C073071300001E", "6E5186C0730705000008"};
                //await UARTSendCommandsAsync(getModelInformationsCMD);
            }
            catch (System.Exception)
            {
                MessageBox.Show("Failed to write to port.");
            }
        }
        public async void uartSendCommand(string command)
        {
            factoryWindow.createRCS();
            try
            {
                if (this.comboModelPort.Text.Contains("COM"))
                    await UARTSendCommandAsync(command);
                //else if (this.comboModelPort.Text.Contains("I2C"))
                //    await I2CSendCommandAsync(command);
            }
            catch (System.Exception)
            {
                MessageBox.Show("Failed to write to port.");
            }
        }
        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                 USB plug/unplug observe Layer                              #####################
        //###################################################################################################################################
        //###################################################################################################################################

        public void DisconnectProcess()
        {
            DeviceOpen = false;
            buttonModelConnect.Content = "Connect";
            comboModelPort.IsEnabled = true;
            comboModelPortRate.IsEnabled = true;
            comboModelPort.Items.Clear();
            comboModelPortRate.Items.Clear();
            textBoxModelSerialNumber.Text = "NA";
            textBoxModelInputSource.Text = "NA";
            textBoxModelFirmwareVersion.Text = "NA";
            serialPort1.DiscardInBuffer();
            /*
            button_Disconnect.IsEnabled = false;
            button_Connect.IsEnabled = true;
            label_ConnectStatue.Content = "Disconnect";
            label_I2cStatus.Content = "Closed I2C";
            button_SetTest.IsEnabled = false;
            button_GetTest.IsEnabled = false;
            textbox_SetStatus.Text = "";
            textbox_GetStatus.Text = "";
            AppStatus = I2C_SetLineStatesIdle();
            if (AppStatus != 0)
            {
                Console.WriteLine("button_Disconnect_Click error!");
            }
            */
            // Close the FTDI device and then close the window
            myFtdiDevice.Close();

            // For FTDI USB unplug observe close
            hotPlug.Unregister();
        }

        public class HotPlug
        {
            public delegate void DelegateHotPlug(object sender, EventArrivedEventArgs e);
            public void Register(
            DelegateHotPlug DeviceInsertedEvent, DelegateHotPlug DeviceRemovedEvent)
            {
                insertQuery = new WqlEventQuery(
                "SELECT * FROM __InstanceCreationEvent " +
                "WITHIN 2 " +
                "WHERE TargetInstance ISA 'Win32_USBHub'"
                );
                insertWatcher = new ManagementEventWatcher(insertQuery);
                insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
                insertWatcher.Start();
                removeQuery = new WqlEventQuery(
                "SELECT * FROM __InstanceDeletionEvent " +
                "WITHIN 2 " +
                "WHERE TargetInstance ISA 'Win32_USBHub'"
                );
                removeWatcher = new ManagementEventWatcher(removeQuery);
                removeWatcher.EventArrived += new EventArrivedEventHandler(DeviceRemovedEvent);
                removeWatcher.Start();
            }
            public void Unregister()
            {
                insertWatcher.Stop();
                insertWatcher.Dispose();
                removeWatcher.Stop();
                removeWatcher.Dispose();
            }
            private WqlEventQuery insertQuery = null;
            private WqlEventQuery removeQuery = null;

            private ManagementEventWatcher insertWatcher = null;
            private ManagementEventWatcher removeWatcher = null;
        }
        private void HotPlugDeviceInserted(object sender, EventArrivedEventArgs e)
        {
            var instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];

            foreach (var property in instance.Properties)
            {
                if (property.Name.Equals("Caption"))
                {
                    if (!property.Value.Equals("USB Composite Device"))
                    {
                        // Query the D3XX devices connected to the system
                        // if no device is currently opened
                        SetTheTextToListBoxCommand("FTDI USD Insert !!!!!!!!!!!!!!");
                        break;
                    }
                }
            }

        }
        private void HotPlugDeviceRemoved(object sender, EventArrivedEventArgs e)
        {
            var instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            foreach (var property in instance.Properties)
            {
                if (property.Name.Equals("DeviceID"))
                {
                    string strValue = (string)property.Value;
                    if (strValue.Contains("VID_0403") && strValue.Contains("PID_6010"))
                    {

                        Console.WriteLine(property.Value);
                        SetTheTextToListBoxCommand("FTDI USD Remove !!!!!!!!!!!!!!");
                        //Close FTDI device
                        DeviceOpen = false;
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            DisconnectProcess();
                        }));

                    }
                    break;
                }
            }
            //Console.WriteLine("Remove!!!");
        }

        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                 Set Text Box Function                                      #####################
        //###################################################################################################################################
        //###################################################################################################################################

        private void SetDescriptionToListBoxCommand(string strText)
        {
            // xml 裡的description以逗號分開
            string[] descriptionItems = strText.Split(',');

            this.Dispatcher.BeginInvoke(new Action(delegate
            {
                listBoxCommand.Items.Clear();
                foreach (var item in descriptionItems)
                {
                    listBoxCommand.Items.Add(item.Trim());
                }
            }));
        }

        public void SetTheTextToListBoxCommand(string strText)
        {
            this.Dispatcher.BeginInvoke(new Action(delegate
            {
                listBoxCommand.Items.Add(strText);
            }));
        }

        public void SetTheTextToTextBoxFirmwareVersion(string strText)
        {
            this.Dispatcher.BeginInvoke(new Action(delegate
            {
                this.textBoxModelFirmwareVersion.Text = ($"{strText}");
            }));
        }

        public void SetTheTextToTextBoxInputSource(string strText)
        {
            this.Dispatcher.BeginInvoke(new Action(delegate
            {
                this.textBoxModelInputSource.Text = ($"{strText}");
            }));
        }
        public void SetTheTextToTextBoxSerialNumber(string strText)
        {
            this.Dispatcher.BeginInvoke(new Action(delegate
            {
                this.textBoxModelSerialNumber.Text = ($"{strText}");
            }));
        }

        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                 XML Read Function Layer                                    #####################
        //###################################################################################################################################
        //###################################################################################################################################

        // Define item list
        public class Item
        {
            public string Magic { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public string Length { get; set; }
            public string VCCode { get; set; }
            public string Model { get; set; }
            public string Style { get; set; }
            public string Prefix { get; set; }
            public string Wlength { get; set; }// zh add
            public string Rlength { get; set; }
            public string Delay { get; set; }

            public string Description { get; set; }

            public List<string> DataValues { get; set; }
        }

        private void LoadXmlFile(string xmlFilePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(xmlFilePath);
            }
            catch
            {
                MessageBox.Show("Can't find xml file, please check the file path again");
                return;
            }

            //Read the version attribute from the<commands> element and set it to the TextBlock.
            XmlNode commandsNode = xmlDoc.SelectSingleNode("//commands");
            if (commandsNode != null && commandsNode.Attributes["version"] != null)
            {
                textBlockVersion.Text = commandsNode.Attributes["version"].Value;
            }
            else
            {
                textBlockVersion.Text = "Version not found";
            }

            XmlNodeList commandMagic = xmlDoc.GetElementsByTagName("magic");
            XmlNodeList commandName = xmlDoc.GetElementsByTagName("name");
            XmlNodeList commandType = xmlDoc.GetElementsByTagName("type");
            XmlNodeList commandLength = xmlDoc.GetElementsByTagName("length");
            XmlNodeList commandVCCode = xmlDoc.GetElementsByTagName("vccode");
            XmlNodeList commandModel = xmlDoc.GetElementsByTagName("model");
            XmlNodeList commandStyle = xmlDoc.GetElementsByTagName("style");
            XmlNodeList commandPrefix = xmlDoc.GetElementsByTagName("prefix");
            XmlNodeList commandWlength = xmlDoc.GetElementsByTagName("wlength");//zh add
            XmlNodeList commandRlength = xmlDoc.GetElementsByTagName("rlength");
            XmlNodeList commandDelay = xmlDoc.GetElementsByTagName("delay");
            XmlNodeList commandDescription = xmlDoc.GetElementsByTagName("description");

            var items = new List<Item>();

            for (int i = 0; i < commandName.Count; i++)
            {
                // 取得當前 command 的名稱
                string commandNameText = commandName[i]?.InnerText ?? "";
                var item = new Item
                {
                    Magic = commandMagic[i]?.InnerText,
                    Name = commandName[i]?.InnerText,
                    Type = commandType[i]?.InnerText,
                    Length = commandLength[i]?.InnerText,
                    VCCode = commandVCCode[i]?.InnerText,
                    Prefix = commandPrefix[i]?.InnerText,
                    Style = commandStyle[i]?.InnerText,//zh add
                    Wlength = commandWlength[i]?.InnerText,//zh add
                    Rlength = commandRlength[i]?.InnerText,//zh add
                    Delay = commandDelay[i]?.InnerText,
                    Description = commandDescription[i]?.InnerText,
                    DataValues = new List<string>()
                };

                // 找所有item的data
                XmlNode commandNode = commandName[i].ParentNode;
                for (int j = 0; ; j++)
                {
                    XmlNode dataNode = commandNode.SelectSingleNode($"data{j}");
                    if (dataNode == null) break;
                    item.DataValues.Add(dataNode.InnerText);
                }

                items.Add(item);
            }
            comboCommand.ItemsSource = items;
            comboCommand.DisplayMemberPath = "Name";
            comboCommand.SelectedValuePath = "Name";
        }

        private void comboCommand_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = comboCommand.SelectedItem as Item;

            if (selectedItem != null)
            {
                // Selected item
                var selectedName = selectedItem.Name;
                //Console.WriteLine("Main Window 頁面中的command 選單選的command 是" + selectedItem.Name);
                string HostAddr = "6E";
                xmlCommand = $"{HostAddr}" + $"{selectedItem.Magic}" + $"{selectedItem.Length}" + $"{selectedItem.Prefix}";
                SetDescriptionToListBoxCommand(selectedItem.Description);
            }
        }

        private void buttonModelCommandExport_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = comboCommand.SelectedItem as Item;

            // 假設 selectedItemsList 是全局變數
            List<Item> selectedItemsList = new List<Item>();


            if (selectedItem != null)
            {
                selectedItemsList.Add(selectedItem);
                // MessageBox.Show($"已添加 {selectedItem.Name} 到匯出列表");
            }

            if (buttonModelCommandExport.IsEnabled)
            {
                string filePath = "D:\\CommandOutput.xls";
                string currentTime = DateTime.Now.ToString("yyyy/MM/dd/HH:mm:ss");

                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("<?xml version=\"1.0\"?>");
                    writer.WriteLine("<?mso-application progid=\"Excel.Sheet\"?>");
                    writer.WriteLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
                    writer.WriteLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
                    writer.WriteLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
                    writer.WriteLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
                    writer.WriteLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");
                    writer.WriteLine(" <Worksheet ss:Name=\"Sheet1\">");
                    writer.WriteLine("  <Table>");


                    foreach (var item in listBoxCommand.Items)
                    {
                        var mesg = item.ToString();
                        int timelength = mesg.IndexOf(']') - mesg.IndexOf('[');
                        if (timelength > 0)
                        {
                            string time = mesg.Substring(1, timelength - 1);
                            string mesgTrimTime = mesg.Remove(0, timelength + 2);
                            writer.WriteLine("   <Row>");
                            writer.WriteLine($"    <Cell><Data ss:Type=\"String\">{time}</Data></Cell>");
                            writer.WriteLine($"    <Cell><Data ss:Type=\"String\">{mesgTrimTime}</Data></Cell>");
                            writer.WriteLine("   </Row>");
                        }
                        else
                        {
                            writer.WriteLine("   <Row>");
                            writer.WriteLine($"    <Cell><Data ss:Type=\"String\"> </Data></Cell>");
                            writer.WriteLine($"    <Cell><Data ss:Type=\"String\">{mesg}</Data></Cell>");
                            writer.WriteLine("   </Row>");
                        }
                    }


                    writer.WriteLine("  </Table>");
                    writer.WriteLine(" </Worksheet>");
                    writer.WriteLine("</Workbook>");

                }
                MessageBox.Show($"Excel 文件已創建於 {filePath}");
            }
        }

        public byte check_sum(byte[] pArray)
        {
            byte checksum = 0x00;
            for (int i = 0; i < pArray.Length; i++)
            {
                byte data = pArray[i];
                checksum ^= data;
            }
            return checksum;
        }

        private void checkBoxAlwaysOnTop_Checked(object sender, EventArgs e)
        {
            if ((bool)checkBoxAlwaysOnTop.IsChecked)
            {
                this.Topmost = true;
            }
            else
            {
                this.Topmost = false;
            }
        }

        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                command list right mouse  process                           #####################
        //###################################################################################################################################
        //###################################################################################################################################

        // 複製選中項
        private void CopySelected_Click(object sender, RoutedEventArgs e)
        {
            if (listBoxCommand.SelectedItems.Count > 0)
            {
                StringBuilder copiedText = new StringBuilder();

                foreach (var item in listBoxCommand.SelectedItems)
                {
                    copiedText.AppendLine(item.ToString());
                }

                Clipboard.SetText(copiedText.ToString());
            }
        }

        // 選擇全部
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            listBoxCommand.SelectAll();
        }

        // 清空所有項目
        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            listBoxCommand.Items.Clear();
        }

        private void listBoxCommand_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            listBoxCommand.ScrollIntoView(listBoxCommand.SelectedItem);
            listBoxCommand.UpdateLayout();
        }

        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                Test Function                                               #####################
        //###################################################################################################################################
        //###################################################################################################################################

        private void buttonTest_Click(object sender, RoutedEventArgs e)  ///wu 250709 保留function 但是mainwindow畫面不顯示test button
        {
            byte result_SET = 0;
            byte result_GET = 0;
            byte trycount = 3;


            byte[] pppp = new byte[] { 0x37 * 2, 0x51, 0x82, 0x01, 0x10 };
            byte[] pppp_set = new byte[] { 0x37 * 2, 0x51, 0x84, 0x03, 0x10, 0x00, 0x50 };
            SetTheTextToListBoxCommand(" ");
            do
            {
                result_SET = i2cController.DDCCI_Send_Command(pppp_set);

                //result_GET = i2cController.DDCCI_Send_Command(pppp);

                trycount--;
            } while ((result_SET != 0 || result_GET != 0) && trycount != 0);
        }



        /// 只要連線狀態改變就呼叫UpdateBoardConnection
        public void UpdateBoardConnection(bool connected)
        {
            IsBoardConnected = connected;
            EvaluatePageEnable();
        }

        public void UpdateSensorConnection(bool connected)
        {
            IsSensorConnected = connected;
            EvaluatePageEnable();
        }

        /// Tab 的開關 
        private void EvaluatePageEnable()
        {
            TabItemFactory.IsEnabled = IsBoardConnected;                    // Factory 只看board 
            bool ready = IsBoardConnected && IsSensorConnected;          //board 和sensor 做AND 
            TabItemALC.IsEnabled = ready;
            TabItemCalibration.IsEnabled = ready;
        }



    }
}
