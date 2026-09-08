using Microsoft.Win32;
using MonitorFactoryTool.Pages;
using ONYX_DataType;
using OnyxSensor;// zh add for sensor
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using System.Windows.Threading;
using static MonitorFactoryTool.MainWindow;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Application;
using Excel = Microsoft.Office.Interop.Excel;

namespace MonitorFactoryTool.Pages
{
    /// <summary>
    /// Measure.xaml 的互動邏輯
    /// </summary>
    public partial class Measure : Page
    {
        private DispatcherTimer dispatcherTimer;

        private bool isLogging = false;
        public SensorBase sensor;//zh add for sensor

        // Create for Display USB unplug observe
        HotPlug hotPlug = new HotPlug();



        public Measure()
        {
            InitializeComponent();
        }
        private void buttonPattern_Click(object sender, RoutedEventArgs e)
        {
            //會跳出新的視窗顯示純色畫板
            Pages.Pattern pattern = new Pages.Pattern();
            pattern.Visibility = Visibility.Visible;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
         
        }


        private void buttonLogClear_Click(object sender, RoutedEventArgs e)
        {
            listBoxLog.Items.Clear();
            listBoxLog.UpdateLayout();
        }


        ///////////////以下export excel xlsx檔案//////////////////
        ///////////////export檔案時要先將excel關閉///////////////
        private void buttonLogExport_Click(object sender, RoutedEventArgs e)
        {
            if (buttonLogExport.IsEnabled)
            {
                // Configure open file dialog box
                var dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.FileName = "Meas_output"; // Default file name
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

                    string currentTime = DateTime.Now.ToString("yyyy/MM/dd/HH:mm:ss");

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
                        Excel.Range cell_date = worksheet.Cells[1, 1];
                        cell_date.Value = "Date";
                        Excel.Range cell_time = worksheet.Cells[1, 2];
                        cell_time.Value = "Time";
                        Excel.Range cell_x = worksheet.Cells[1, 3];
                        cell_x.Value = "x";
                        Excel.Range cell_y = worksheet.Cells[1, 4];
                        cell_y.Value = "y";
                        Excel.Range cell_Y = worksheet.Cells[1, 5];
                        cell_Y.Value = "Y";
                        Excel.Range cell_CT = worksheet.Cells[1, 6];
                        cell_CT.Value = "CT";
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("Error: Writing title failed");
                    }
                    try
                    {
                        for (int i = 0; i < listBoxLog.Items.Count; i++)
                        {
                            string[] parts = listBoxLog.Items[i].ToString().Split(new[] { ",     " }, StringSplitOptions.None);

                            // 初始化欄位
                            string date = "", time = "", x = "", y = "", Y = "", CT = "";

                            //zh add
                            date = parts[0];
                            time = parts[1];
                            x = parts[2];
                            y = parts[3];
                            Y = parts[4];
                            CT = parts[5];
                            Excel.Range cell_date = worksheet.Cells[i + 2, 1];
                            cell_date.Value = date;
                            Excel.Range cell_time = worksheet.Cells[i + 2, 2];
                            cell_time.Value = time;
                            Excel.Range cell_x = worksheet.Cells[i + 2, 3];
                            cell_x.Value = x;
                            Excel.Range cell_y = worksheet.Cells[i + 2, 4];
                            cell_y.Value = y;
                            Excel.Range cell_Y = worksheet.Cells[i + 2, 5];
                            cell_Y.Value = Y;
                            Excel.Range cell_CT = worksheet.Cells[i + 2, 6];
                            cell_CT.Value = CT;
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
        ///////////////以上export excel xlsx檔案//////////////////


        //////////// 以下每幾秒產生一個log text /////////////////////

        private System.Threading.Timer threadingTimer;
        private int TIME_RPERIOD;
        private void StartTimer(int period_ms)
        {
            if (period_ms < 1000)
            {
                period_ms = 1000;
            }
            threadingTimer = new System.Threading.Timer(new System.Threading.TimerCallback(ThreadMethod), null, 1000, period_ms);
        }
        private void StopTimer()
        {
            threadingTimer.Dispose();
        }
        private void ThreadMethod(object a)
        {
            if (isLogging)
            {
                SensorMeasureYxy_t Meas_Yxyz = new SensorMeasureYxy_t();
                bool devIsConnect = sensor.MeasYxyz(out Meas_Yxyz);
                ///// check i1D3 connection
                if (devIsConnect == false)
                {
                    this.Dispatcher.Invoke(new System.Action(() =>
                    {
                        sensor.Close();
                        buttonModuleConnect.Content = "Connect";
                        buttonLogStart.IsEnabled = false;
                        buttonGamma.IsEnabled = false;
                        buttonLogStart.Content = "Start Log";
                        StopTimer();
                    }));
                    MessageBox.Show("Please check device connection");
                    return;
                }
                ////
                Console.WriteLine($"Y = {Meas_Yxyz.Y}, x = {Meas_Yxyz.x}, y = {Meas_Yxyz.y}, z = {Meas_Yxyz.z}");
                double CT_n = (Meas_Yxyz.x - 0.3320) / ((float)Meas_Yxyz.y - 0.1858);
                double CCT = -437 * CT_n * CT_n * CT_n + 3601 * CT_n * CT_n - 6831 * CT_n + 5517;
                Dispatcher.Invoke(new System.Action(() =>
                {

                    textBoxMessureY.Text = Meas_Yxyz.Y.ToString("F4");
                    textBoxMessurex.Text = Meas_Yxyz.x.ToString("F4");
                    textBoxMessurey.Text = Meas_Yxyz.y.ToString("F4");
                    textBoxMessureColorTemp.Text = CCT.ToString("F2");

                    DateTime now = DateTime.Now;
                    string currentTime = $"{now.Year}/{now.Month}/{now.Day},     {now:HH:mm:ss}";
                    //string currentTime = $"{now:HH:mm:ss}";//zh

                    var logParts = new List<string>();

                    if (checkBoxMeasureDate.IsChecked == true)
                    {
                        logParts.Add($"{currentTime}");//zh
                                                       //logParts.Add($"time={currentTime}");
                    }
                    if (checkBoxMeasurexy.IsChecked == true)
                    {
                        logParts.Add($"{textBoxMessurex.Text}");//zh
                        logParts.Add($"{textBoxMessurey.Text}");//zh
                                                                //logParts.Add($"x={textBoxMessurex.Text}, y={textBoxMessurey.Text}");
                    }

                    if (checkBoxMeasureY.IsChecked == true)
                    {
                        logParts.Add($"{textBoxMessureY.Text}");//zh
                                                                //logParts.Add($"Y={textBoxMessureY.Text}nits");
                    }
                    if (checkBoxMeasureColorTemp.IsChecked == true)
                    {
                        logParts.Add($"{textBoxMessureColorTemp.Text}");//zh
                                                                        //logParts.Add($"CT={textBoxMessureColorTemp.Text}k");
                    }

                    listBoxLog.Items.Add(string.Join(",     ", logParts));

                    listBoxLog.UpdateLayout();
                    //頁面到最後一行
                    listBoxLog.ScrollIntoView(listBoxLog.Items[listBoxLog.Items.Count - 1]);
                }));
            }
        }



   



        private void textBoxLogSecond_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Regex.IsMatch(textBoxLogSecond.Text, @"^\d+$"))
                TIME_RPERIOD = Convert.ToInt16(textBoxLogSecond.Text) * 1000; // 1 sec = 1000 msec 
            else
                TIME_RPERIOD = 1000; // 1 sec
        }

        private void buttonLogStart_Click(object sender, RoutedEventArgs e)
        {
            if (isLogging)
            {
                isLogging = false;
                buttonLogStart.Content = "Start Log";
                buttonLogExport.IsEnabled = true;
                StopTimer();
            }
            else
            {
                isLogging = true;
                buttonLogStart.Content = "Stop Log";
                buttonLogExport.IsEnabled = false;
                StartTimer(TIME_RPERIOD);
            }
        }
        //////////// 以上每幾秒產生一個log text ///////////////////////////////// 

        public delegate void ListBoxDataChangedEventHandler(List<string> data);
        public event ListBoxDataChangedEventHandler ListBoxDataChanged;

        private List<string> logDataList = new List<string>(); // 用於存儲日誌數據

        private void buttonGamma_Click(object sender, RoutedEventArgs e)
        {
            Pages.Gamma gamma = new Pages.Gamma(sensor);

            //gamma.DataAdded += AddDataToMeasure;
            gamma.DataUpdated += UpdateTextBoxes;

            gamma.Visibility = Visibility.Visible;
            buttonModuleConnect.IsEnabled = false;
            buttonGamma.IsEnabled = false;
            // If Gamma measure is opened, stop measure log 
            buttonLogStart.IsEnabled = false;
            isLogging = false;
            buttonLogStart.Content = "Start Log";


            if (threadingTimer != null) StopTimer();

            // Binding enable startlog button event to Gamma page
            gamma.ButtonStateChanged += enableMeasButton;
            gamma.disconnectProcess += disconnectAtGamma;

            gamma.Closed += (s, args) =>
            {
                buttonModuleConnect.IsEnabled = true;
            };


        }

        public void AddDataToMeasure(string log)
        {
            Dispatcher.Invoke(() =>
            {
                listBoxLog.Items.Add(log);
            });
        }


        private void UpdateTextBoxes(double x, double y, double Y)
        {
            double CT_n = (x - 0.3320) / ((float)y - 0.1858);
            double CCT = -437 * CT_n * CT_n * CT_n + 3601 * CT_n * CT_n - 6831 * CT_n + 5517;

            Dispatcher.Invoke(() =>
            {
                textBoxMessurex.Text = x.ToString("F4");
                textBoxMessurey.Text = y.ToString("F4");
                textBoxMessureY.Text = Y.ToString("F4");
                textBoxMessureColorTemp.Text = CCT.ToString("F4");

            });
        }



        private void comboSensor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //右邊Sensor 下拉式選單
            string selectedPort = comboSensor.SelectedItem as string;
            //TBD
        }

        private async void buttonModuleConnect_Click(object sender, RoutedEventArgs e)// zh add for sensor
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            //bool result = true;

         
           // SensorBase sensor = null; 


            switch (comboSensor.SelectedIndex)
            {
                case 0:
                    sensor = i1d3.getInstance();
                    break;
                case 1:
                    sensor = ca410.getInstance();
                    break;
            }


            if (buttonModuleConnect.Content.Equals("Connect"))
            {

                if (sensor.Config())
                {
                    buttonModuleConnect.IsEnabled = false;
                    comboSensor.IsEnabled = false;
                    buttonModuleConnect.Content = "Connecting";
                    bool result = await Task.Run(() => sensor.Open());

                    if (result)
                    {                     
                        buttonModuleConnect.Content = "Disconnect";
                        buttonLogStart.IsEnabled = true;
                        buttonGamma.IsEnabled = true;   
                        //mainWindow.TabItemALC.IsEnabled = true; //250204 wu add enable ALC page
                        //mainWindow.TabItemCalibration.IsEnabled = true; //250211 wu add enableCalibration page
                        mainWindow.UpdateSensorConnection(true); //250710 wu add  connect/disconnected 之後更新page 
                        mainWindow.ConnectedSensor = sensor; //250630 wu add 傳使用的sensor 出去
                    }
                    else
                    {
                        buttonModuleConnect.Content = "Connect";
                        comboSensor.IsEnabled = true;
                        Console.WriteLine("Open 失敗");
                    }
                    buttonModuleConnect.IsEnabled = true;
                }
            }
            else
            {               
                sensor.Close();
                mainWindow.UpdateSensorConnection(false); //250710 wu add  connect/disconnected 之後更新page 
                if (mainWindow.ConnectedSensor != null)
                {
                    mainWindow.ConnectedSensor.Close();
                    mainWindow.ConnectedSensor = null; // 清除sensor
                }

                comboSensor.IsEnabled = true;
                buttonModuleConnect.Content = "Connect";
                buttonLogStart.IsEnabled = false;
                buttonGamma.IsEnabled = false; 
                mainWindow.TabItemALC.IsEnabled = false; // 250204 wu add  disenable ALC page
                mainWindow.TabItemCalibration.IsEnabled = false; //250211 wu add enable Calibration page
                if (threadingTimer != null) StopTimer();
                //MessageBox.Show("Please check device connectioffbfbfdbdfbdfbdbfdbdfn");
            }


        }

        private void comboSensor_Loaded(object sender, RoutedEventArgs e)//zh add for sensor
        {
            comboSensor.Items.Clear();
            sensor = i1d3.getInstance();
            Console.WriteLine($"Sensor list {sensor.SensorName}.");
            comboSensor.Items.Add(sensor.SensorName);
            sensor = ca410.getInstance();
            Console.WriteLine($"Sensor list {sensor.SensorName}.");
            comboSensor.Items.Add(sensor.SensorName);


            var mainWindow = (MainWindow)Application.Current.MainWindow;
            if (mainWindow.ConnectedSensor != null)
            {
                sensor = mainWindow.ConnectedSensor;  

                if (sensor is ca410)
                    comboSensor.SelectedIndex = 1;
                else
                    comboSensor.SelectedIndex = 0;

                comboSensor.IsEnabled = false;
                buttonModuleConnect.Content = "Disconnect";
                buttonLogStart.IsEnabled = true;
                buttonGamma.IsEnabled = true;
            }
            else
            {
               
                comboSensor.SelectedIndex = 0;
                buttonModuleConnect.Content = "Connect";
            }

            //comboSensor.SelectedIndex = 0;

            //// Restore sensor instance
            //if(buttonModuleConnect.Content.ToString() == "Disconnect")
            //{
            //    switch (comboSensor.SelectedIndex)
            //    {
            //        case 0: sensor = i1d3.getInstance(); break;
            //        case 1: sensor = ca410.getInstance(); break;
            //    }
            //}

        }

        private void enableMeasButton()
        {
            buttonLogStart.IsEnabled = true;
            buttonGamma.IsEnabled = true;
        }
        private void disconnectAtGamma()
        {
            sensor.Close();
            buttonModuleConnect.Content = "Connect";
            buttonLogStart.IsEnabled = true;
            buttonGamma.IsEnabled = false;
        }

    }
}

