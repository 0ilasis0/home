using FDTI_Factory_i2c;
using MonitorFactoryTool.ONYX.Utilities;
using ONYX.Utilities;
using ONYX_DataType;
using OnyxSensor;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using static MonitorFactoryTool.MainWindow;
using Excel = Microsoft.Office.Interop.Excel;

namespace MonitorFactoryTool.Pages
{
    /// <summary>
    /// Gamma.xaml 的互動邏輯
    /// </summary>
    public partial class Gamma : Window
    {
        private SensorBase _sensor;

        private bool isStopPressed = false;

        public event Action<double> GammaMeasured;   ///wu 250401 add   傳值到blackwindow 中的listbox 用
        public event Action<string> DICOMMeasured;   ///wu 250401 add   傳值到blackwindow 中的listbox 用

        public Calibration CalibrationPage { get; set; } //251114 SN FW add to report 




        public Gamma(SensorBase sensor)
        {
            InitializeComponent();
            MainWindow.AlwaysOnTopChanged += OnAlwaysOnTopChanged;

            Screen[] screens = Screen.AllScreens;

            if (screens.Length > 1)
            {
                Screen secondScreen = screens[1];
                this.WindowStartupLocation = WindowStartupLocation.Manual;

                this.Loaded += (sender, e) =>
                {
                    this.Left = secondScreen.WorkingArea.Left + (secondScreen.WorkingArea.Width - this.ActualWidth) / 16;
                    this.Top = secondScreen.WorkingArea.Top + (secondScreen.WorkingArea.Height - this.ActualHeight) / 3;
                };
            }
            else
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            _sensor = sensor;
        }
        private void OnAlwaysOnTopChanged(bool isAlwaysOnTop)
        {
            this.Topmost = isAlwaysOnTop;
        }
        protected override void OnClosed(EventArgs e)
        {
            MainWindow.AlwaysOnTopChanged -= OnAlwaysOnTopChanged;
            base.OnClosed(e);
        }
        private void comboMeasureItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboTargetGamma.Items.Clear();
            comboSampleNumber.Items.Clear();
            comboSampleNumber.Items.Add(16);
            comboSampleNumber.SelectedIndex = 0;
            string[] TargetGamma = new string[] {
                    "Gamma 1.8",
                    "Gamma 2.0",
                    "Gamma 2.2",
                    "Gamma 2.4",
                    "Gamma 2.6"};
            string[] TargetColorTemp = new string[] {
                    "5400K",
                    "6500K",
                    "9300K"};
            switch (comboMeasureItem.SelectedIndex)
            {
                case 0:
                    Console.WriteLine("Measure Item is White");
                    comboTargetGamma.IsEnabled = true;
                    comboSampleNumber.IsEnabled = true;
                    foreach (string target in TargetGamma)
                    {
                        // Add it to the combo box.
                        this.comboTargetGamma.Items.Add(target);
                    }
                    comboTargetGamma.SelectedIndex = 0;
                    target_gamma = 1.8;
                    break;
                case 1:
                    Console.WriteLine("Measure Item is White + RGB");
                    comboTargetGamma.IsEnabled = true;
                    comboSampleNumber.IsEnabled = true;
                    foreach (string target in TargetGamma)
                    {
                        // Add it to the combo box.
                        this.comboTargetGamma.Items.Add(target);
                    }
                    comboTargetGamma.SelectedIndex = 0;
                    target_gamma = 1.8;
                    break;
                case 2:
                    Console.WriteLine("Measure Item is DICOM");
                    comboTargetGamma.IsEnabled = true;
                    comboSampleNumber.IsEnabled = false;

                    foreach (string target in TargetColorTemp)
                    {
                        // Add it to the combo box.
                        this.comboTargetGamma.Items.Add(target);
                    }
                    comboTargetGamma.SelectedIndex = 1;
                    break;
                default:
                    break;
            }
        }
        private void comboTargetGamma_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (comboTargetGamma.SelectedIndex)
            {
                case 0:
                    target_gamma = 1.8;
                    target_colortemp = 5400;
                    target_CT_x = 0.335;
                    target_CT_y = 0.350;
                    Console.WriteLine($"Measure target Gamma is {target_gamma}");
                    Console.WriteLine($"Measure target ColorTemp is {target_colortemp}");
                    break;
                case 1:
                    target_gamma = 2.0;
                    target_colortemp = 6500;
                    target_CT_x = 0.313;
                    target_CT_y = 0.329;
                    Console.WriteLine($"Measure target Gamma is {target_gamma}");
                    Console.WriteLine($"Measure target ColorTemp is {target_colortemp}");
                    break;
                case 2:
                    target_gamma = 2.2;
                    target_colortemp = 9300;
                    target_CT_x = 0.283;
                    target_CT_y = 0.298;
                    Console.WriteLine($"Measure target Gamma is {target_gamma}");
                    Console.WriteLine($"Measure target ColorTemp is {target_colortemp}");
                    break;
                case 3:
                    target_gamma = 2.4;
                    target_colortemp = 9300;
                    Console.WriteLine($"Measure target Gamma is {target_gamma}");
                    Console.WriteLine($"Measure target ColorTemp is {target_colortemp}");
                    break;
                case 4:
                    target_gamma = 2.6;
                    Console.WriteLine($"Measure target Gamma is {target_gamma}");
                    break;
                default:
                    break;
            }
        }

        private void comboSampleNumber_DropDownOpened(object sender, EventArgs e)
        {
            // Clear combobox
            this.comboSampleNumber.Items.Clear();
            switch (comboMeasureItem.SelectedIndex)
            {
                case 2:
                    string[] AvailableSampleForDICOM = new string[] {
                    "16"
                    };
                    foreach (string sample_num in AvailableSampleForDICOM)
                    {
                        // Add it to the combo box.
                        this.comboSampleNumber.Items.Add(sample_num);
                    }
                    break;
                default:
                    string[] AvailableSample = new string[] {
                    "16",
                    "32",
                    "64",
                    "128",
                    "256"};
                    foreach (string sample_num in AvailableSample)
                    {
                        // Add it to the combo box.
                        this.comboSampleNumber.Items.Add(sample_num);
                    }
                    break;
            }
        }

        private void comboSampleNumber_DropDownClosed(object sender, EventArgs e)
        {
            if (comboSampleNumber.Text == "")
            {
                comboSampleNumber.SelectedIndex = 0;
            }
            Console.WriteLine($"The Sample Number is {comboSampleNumber.Text}");
        }
        private void comboSampleNumber_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboMeasureItem.SelectedIndex == 0 || comboMeasureItem.SelectedIndex == 1)
            {
                if (comboSampleNumber.SelectedIndex == 0) buttonExport.IsEnabled = false;
                //else buttonExport.IsEnabled = true;//zh251107 del
            }
            else
            {
                //buttonExport.IsEnabled = true;//zh251107 del
            }
        }
        private async void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = (System.Windows.Controls.Button)sender;
            if (comboMeasureItem.SelectedIndex == -1) return;
            if (button.Content.ToString() == "Start")
            {
                button.Content = "Stop";
                listBoxGammaLog.Items.Clear();
                isStopPressed = false;
                comboMeasureItem.IsEnabled = false;
                comboTargetGamma.IsEnabled = false;
                comboSampleNumber.IsEnabled = false;
                textBoxAmbientLight.IsEnabled = false;
                buttonExport.IsEnabled = false;
                await measureGammaAsync(comboMeasureItem.SelectedIndex);
            }
            else
            {
                button.Content = "Start";
                comboMeasureItem.IsEnabled = true;
                comboTargetGamma.IsEnabled = true;
                if (comboMeasureItem.SelectedIndex != 2) comboSampleNumber.IsEnabled = true;
                textBoxAmbientLight.IsEnabled = true;
                if (comboMeasureItem.SelectedIndex == 0 || comboMeasureItem.SelectedIndex == 1)
                    if (comboSampleNumber.SelectedIndex == 0) buttonExport.IsEnabled = false;
                    else buttonExport.IsEnabled = true;
                isStopPressed = true;
            }
        }
        //private i1d3 sensor;
        public class gammaStandardData
        {
            public int gray_level { get; set; }
            public double luminance { get; set; }
        }
        public class gammaMeasureData
        {
            public int gray_level { get; set; }
            public double x { get; set; }
            public double y { get; set; }
            public double Y { get; set; }
        }

        //  For Gamma measure
        private List<gammaMeasureData> measureDataList = new List<gammaMeasureData>();
        private List<gammaMeasureData> measureDataList_Red = new List<gammaMeasureData>();
        private List<gammaMeasureData> measureDataList_Green = new List<gammaMeasureData>();
        private List<gammaMeasureData> measureDataList_Blue = new List<gammaMeasureData>();
        private List<gammaStandardData> standardDataList = new List<gammaStandardData>();
        private List<gammaStandardData> standardDataList_plus009 = new List<gammaStandardData>();
        private double[] DICOMMeasureArr_JND = new double[18];
        private double[] DICOMStandardArr_Y = new double[18];
        private double[] DICOMStandardArr_JND = new double[18];
        private double target_gamma = 1.8; // Default target Gamma is 1.8
        private double gamma_result;
        private double gamma_result_R;
        private double gamma_result_G;
        private double gamma_result_B;
        private int target_colortemp = 6500; // Default target color temp is 6500K
        private double target_CT_x = 0.313;
        private double target_CT_y = 0.329;

        //  For Delta E calculation
        private ColorData[] mStandardData = null;
        private ColorData[] mMeasureData = null;
        private double[] mDeltaE = null;
        private double mAverDeltaE = 0.0;
        private double mMaxDeltaE = 0.0;
        private async Task measureGammaAsync(int Item)
        {
            //  For Gamma measure
            double luminanceNits;
            gamma_result = gamma_result_R = gamma_result_G = gamma_result_B = 0;
            standardDataList.Clear();
            standardDataList_plus009.Clear();
            measureDataList.Clear();
            measureDataList_Red.Clear();
            measureDataList_Green.Clear();
            measureDataList_Blue.Clear();
            Int16 sample_num = Convert.ToInt16(comboSampleNumber.Text);

            //  For Delta E calculation
            mMeasureData = new ColorData[sample_num];
            mStandardData = new ColorData[sample_num];
            mDeltaE = new double[sample_num];
            mAverDeltaE = 0.0;

            SensorMeasureYxy_t Meas_Yxyz = new SensorMeasureYxy_t();
            bool devIsConnect = _sensor.MeasYxyz(out Meas_Yxyz);
            if (Item == 0)
            {

                Console.WriteLine($"Color value is 0");
                recGamma.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                await Task.Delay(1500);
                listBoxGammaLog.Items.Add($"Start Measure White:");
                listBoxGammaLog.Items.Add($"Level\tx\ty\tY");
                devIsConnect = _sensor.MeasYxyz(out Meas_Yxyz);
                listBoxGammaLog.Items.Add($"0\t{Meas_Yxyz.x.ToString("F4")}\t{Meas_Yxyz.y.ToString("F4")}\t{Meas_Yxyz.Y.ToString("F4")}");
                listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);

                // Save measure low data to list 
                double lum_low = Meas_Yxyz.Y;
                gammaMeasureData measureData_low = new gammaMeasureData();
                measureData_low.gray_level = 0;
                measureData_low.x = Math.Round(Meas_Yxyz.x, 4);
                measureData_low.y = Math.Round(Meas_Yxyz.y, 4);
                measureData_low.Y = Math.Round(Meas_Yxyz.Y, 4) + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                measureDataList.Add(measureData_low);
                for (int level = 1; level < sample_num; level++)
                {
                    if (buttonStart.Content.ToString() != "Stop") return;
                    if (devIsConnect == false) return;
                    int gray_level = 255 * level / (sample_num - 1);
                    Console.WriteLine($"Gray level is {level}");
                    byte levelByte = Convert.ToByte(gray_level);
                    recGamma.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(levelByte, levelByte, levelByte));
                    await Task.Delay(500);// zh modify
                    devIsConnect = _sensor.MeasYxyz(out Meas_Yxyz);

                    // Save measure data to list
                    gammaMeasureData measureData = new gammaMeasureData();
                    measureData.gray_level = gray_level;
                    measureData.x = Math.Round(Meas_Yxyz.x, 4);
                    measureData.y = Math.Round(Meas_Yxyz.y, 4);
                    measureData.Y = Math.Round(Meas_Yxyz.Y, 4) + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                    measureDataList.Add(measureData);
                    string log = ($"{gray_level}\t{Meas_Yxyz.x.ToString("F4")}\t{Meas_Yxyz.y.ToString("F4")}\t{Meas_Yxyz.Y.ToString("F4")}");

                    listBoxGammaLog.Items.Add($"{gray_level}\t{Meas_Yxyz.x.ToString("F4")}\t{Meas_Yxyz.y.ToString("F4")}\t{Meas_Yxyz.Y.ToString("F4")}");
                    UpdateData(measureData.x, measureData.y, measureData.Y);

                    listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);
                }
                if (devIsConnect)
                {
                    //  For Gamma measure
                    double[] measdataArr_Y = new double[measureDataList.Count];
                    double[] measdataArr_P = new double[measureDataList.Count];
                    for (int i = 0; i < measureDataList.Count; i++)
                    {
                        measdataArr_Y[i] = measureDataList[i].Y;
                        measdataArr_P[i] = measureDataList[i].gray_level;
                    }
                    gamma_result = GammaStandard.getGamma(measdataArr_Y);
                    Console.WriteLine($"Test function Gamma (ONYX) :{GammaStandard.getGamma(measdataArr_Y)}");
                    Console.WriteLine($"Test function Gamma (OLD VERSION) :{GammaStandard.gammaCalculte(measdataArr_P, measdataArr_Y)}");

                    //  For Gamma standard
                    luminanceNits = Meas_Yxyz.Y + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                    gammaStandardData standardData_low = new gammaStandardData();
                    standardData_low.gray_level = 0;
                    standardData_low.luminance = Math.Round(lum_low, 4);
                    standardDataList.Add(standardData_low);
                    standardDataList_plus009.Add(standardData_low);
                    for (int level = 1; level < sample_num; level++)
                    {
                        gammaStandardData standardData = new gammaStandardData();
                        gammaStandardData standardData_plus009 = new gammaStandardData();
                        int gray_level = 255 * level / (sample_num - 1);
                        float result = (float)(lum_low + (luminanceNits - lum_low) * Math.Pow(gray_level / 255.0000, target_gamma));
                        float result_2 = (float)(lum_low + (luminanceNits - lum_low) * Math.Pow(gray_level / 255.0000, target_gamma + 0.09));
                        standardData.gray_level = gray_level;
                        standardData_plus009.gray_level = gray_level;
                        standardData.luminance = Math.Round(result, 4);
                        standardData_plus009.luminance = Math.Round(result_2, 4);
                        standardDataList.Add(standardData);
                        standardDataList_plus009.Add(standardData_plus009);

                        Console.WriteLine($"{standardDataList[level - 1].luminance.ToString()}");
                    }

                    if (buttonStart.Content.ToString() != "Stop") return;
                    // For Delta E calculation
                    for (int i = 0; i < sample_num; i++)
                    {
                        mMeasureData[i] = new ColorData(measureDataList[i].x, measureDataList[i].y, measureDataList[i].Y, ColorData.MODE_xyY);
                    }
                    for (int i = 0; i < sample_num; i++)
                    {
                        mStandardData[i] = new ColorData(measureDataList[measureDataList.Count - 1].x, measureDataList[measureDataList.Count - 1].y, standardDataList[i].luminance, ColorData.MODE_xyY);
                    }
                    for (int i = 0; i < sample_num; i++)
                    {
                        LAB measure_lab = new LAB(mMeasureData[i], LAB.REF_WHITE_D65);
                        LAB standard_lab = new LAB(mStandardData[i], LAB.REF_WHITE_D65);
                        mDeltaE[i] = DeltaE2000.DeltaE(measure_lab, standard_lab);
                    }
                    int strCount = 60 * (sample_num - 1) / 255; // Filter data if its graylevel smaller than 60
                    for (int i = strCount; i < sample_num; i++)
                    {
                        mAverDeltaE += mDeltaE[i];
                    }
                    mAverDeltaE /= sample_num - strCount;
                    mMaxDeltaE = mDeltaE.Skip(strCount).Max();
                    Console.WriteLine($"Delta E average : {mAverDeltaE}, Delta E Max : {mMaxDeltaE}");

                    listBoxGammaLog.Items.Add($"Gamma Result : {gamma_result.ToString("F2")}");
                    listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);

                    GammaMeasured?.Invoke(gamma_result);       ///wu 250401 add   傳值到blackwindow 中的listbox 用

                    buttonStart.Content = "Start";
                    buttonExport.IsEnabled = true;//zh251107 add
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Please check device connection");
                    Close();
                    disconnectProcess?.Invoke();
                }
            }
            else if (Item == 1)
            {

                //************************************************** Measure White ********************************************************//
                if (isStopPressed) return;

                Console.WriteLine($"Color value is 0");
                recGamma.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                await Task.Delay(1500);
                listBoxGammaLog.Items.Add($"Start Measure White:");
                listBoxGammaLog.Items.Add($"Level\tx\ty\tY");
                devIsConnect = _sensor.MeasYxyz(out Meas_Yxyz);
                listBoxGammaLog.Items.Add($"0\t{Meas_Yxyz.x.ToString("F4")}\t{Meas_Yxyz.y.ToString("F4")}\t{Meas_Yxyz.Y.ToString("F4")}");
                listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);

                // Save low data to list 
                double lum_low = Meas_Yxyz.Y + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                gammaMeasureData measureData_low = new gammaMeasureData();
                measureData_low.gray_level = 0;
                measureData_low.x = Math.Round(Meas_Yxyz.x, 4);
                measureData_low.y = Math.Round(Meas_Yxyz.y, 4);
                measureData_low.Y = Math.Round(Meas_Yxyz.Y, 4) + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                measureDataList.Add(measureData_low);
                for (int level = 1; level < sample_num; level++)
                {
                    if (buttonStart.Content.ToString() != "Stop") return;
                    if (isStopPressed || devIsConnect == false) return;
                    int gray_level = 255 * level / (sample_num - 1);
                    Console.WriteLine($"Gray level is {level}");
                    byte levelByte = Convert.ToByte(gray_level);
                    recGamma.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(levelByte, levelByte, levelByte));
                    await Task.Delay(500);// zh modify
                    devIsConnect = _sensor.MeasYxyz(out Meas_Yxyz);
                    // Save measure data to list
                    gammaMeasureData measureData = new gammaMeasureData();
                    measureData.gray_level = gray_level;
                    measureData.x = Math.Round(Meas_Yxyz.x, 4);
                    measureData.y = Math.Round(Meas_Yxyz.y, 4);
                    measureData.Y = Math.Round(Meas_Yxyz.Y, 4) + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                    measureDataList.Add(measureData);

                    listBoxGammaLog.Items.Add($"{gray_level}\t{Meas_Yxyz.x.ToString("F4")}\t{Meas_Yxyz.y.ToString("F4")}\t{Meas_Yxyz.Y.ToString("F4")}");
                    UpdateData(measureData.x, measureData.y, measureData.Y);
                    listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);
                }
                if (devIsConnect)
                {
                    //  For Gamma measure
                    double[] measdataArr_Y = new double[measureDataList.Count];
                    double[] measdataArr_P = new double[measureDataList.Count];
                    for (int i = 0; i < measureDataList.Count; i++)
                    {
                        measdataArr_Y[i] = measureDataList[i].Y;
                        measdataArr_P[i] = measureDataList[i].gray_level;
                    }
                    gamma_result = GammaStandard.getGamma(measdataArr_Y);
                    Console.WriteLine($"Test function Gamma (ONYX) :{GammaStandard.getGamma(measdataArr_Y)}");
                    Console.WriteLine($"Test function Gamma (OLD VERSION) :{GammaStandard.gammaCalculte(measdataArr_P, measdataArr_Y)}");

                    //  For Gamma standard
                    luminanceNits = Meas_Yxyz.Y + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                    gammaStandardData standardData_low = new gammaStandardData();
                    standardData_low.gray_level = 0;
                    standardData_low.luminance = Math.Round(lum_low, 4);
                    standardDataList.Add(standardData_low);
                    standardDataList_plus009.Add(standardData_low);
                    for (int level = 1; level < sample_num; level++)
                    {
                        gammaStandardData standardData = new gammaStandardData();
                        gammaStandardData standardData_plus009 = new gammaStandardData();
                        int gray_level = 255 * level / (sample_num - 1);
                        float result = (float)(lum_low + (luminanceNits - lum_low) * Math.Pow(gray_level / 255.0000, target_gamma));
                        float result_2 = (float)(lum_low + (luminanceNits - lum_low) * Math.Pow(gray_level / 255.0000, target_gamma + 0.09));
                        standardData.gray_level = gray_level;
                        standardData_plus009.gray_level = gray_level;
                        standardData.luminance = Math.Round(result, 4);
                        standardData_plus009.luminance = Math.Round(result_2, 4);
                        standardDataList.Add(standardData);
                        standardDataList_plus009.Add(standardData_plus009);

                        Console.WriteLine($"{standardDataList[level - 1].luminance.ToString()}");
                    }

                    if (buttonStart.Content.ToString() != "Stop") return;
                    // For Delta E calculation
                    for (int i = 0; i < sample_num; i++)
                    {
                        mMeasureData[i] = new ColorData(measureDataList[i].x, measureDataList[i].y, measureDataList[i].Y, ColorData.MODE_xyY);
                    }
                    for (int i = 0; i < sample_num; i++)
                    {
                        mStandardData[i] = new ColorData(measureDataList[measureDataList.Count - 1].x, measureDataList[measureDataList.Count - 1].y, standardDataList[i].luminance, ColorData.MODE_xyY);
                    }
                    for (int i = 0; i < sample_num; i++)
                    {
                        LAB measure_lab = new LAB(mMeasureData[i], LAB.REF_WHITE_D65);
                        LAB standard_lab = new LAB(mStandardData[i], LAB.REF_WHITE_D65);
                        mDeltaE[i] = DeltaE2000.DeltaE(measure_lab, standard_lab);
                    }
                    int strCount = 60 * (sample_num - 1) / 255; // Filter data if its graylevel smaller than 60
                    for (int i = strCount; i < sample_num; i++)
                    {
                        mAverDeltaE += mDeltaE[i];
                    }
                    mAverDeltaE /= sample_num - strCount;
                    mMaxDeltaE = mDeltaE.Skip(strCount).Max();
                    Console.WriteLine($"Delta E average : {mAverDeltaE}, Delta E Max : {mMaxDeltaE}");

                    GammaMeasured?.Invoke(gamma_result);       ///wu 250401 add   傳值到blackwindow 中的listbox 用

                    listBoxGammaLog.Items.Add($"Gamma Result : {gamma_result.ToString("F2")}");
                    listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);

                    //GammaMeasured?.Invoke(gamma_result);       ///wu 250401 add   傳值到blackwindow 中的listbox 用

                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Please check i1D3 connection");
                    Close();
                    disconnectProcess?.Invoke();
                }

                //************************************************** Measure Red ********************************************************//
                if (isStopPressed) return;

                Console.WriteLine($"Color value is 0");
                recGamma.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                await Task.Delay(1500);
                listBoxGammaLog.Items.Add($"Start Measure Red:");
                listBoxGammaLog.Items.Add($"Level\tx\ty\tY");
                devIsConnect = _sensor.MeasYxyz(out Meas_Yxyz);
                listBoxGammaLog.Items.Add($"0\t{Meas_Yxyz.x.ToString("F4")}\t{Meas_Yxyz.y.ToString("F4")}\t{Meas_Yxyz.Y.ToString("F4")}");
                listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);

                // Save low data to list
                lum_low = Meas_Yxyz.Y + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                gammaMeasureData measureData_low_r = new gammaMeasureData();
                measureData_low_r.gray_level = 0;
                measureData_low_r.x = Math.Round(Meas_Yxyz.x, 4);
                measureData_low_r.y = Math.Round(Meas_Yxyz.y, 4);
                measureData_low_r.Y = Math.Round(Meas_Yxyz.Y, 4) + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify              
                measureDataList_Red.Add(measureData_low_r);

                for (int level = 1; level < sample_num; level++)
                {
                    if (buttonStart.Content.ToString() != "Stop") return;
                    if (isStopPressed || devIsConnect == false) return;
                    int gray_level = 255 * level / (sample_num - 1);
                    Console.WriteLine($"Gray level is {level}");
                    byte levelByte = Convert.ToByte(gray_level);
                    recGamma.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(levelByte, 0, 0));
                    await Task.Delay(500);// zh modify
                    devIsConnect = _sensor.MeasYxyz(out Meas_Yxyz);

                    // Save measure data to list
                    gammaMeasureData measureData = new gammaMeasureData();
                    measureData.gray_level = gray_level;
                    measureData.x = Math.Round(Meas_Yxyz.x, 4);
                    measureData.y = Math.Round(Meas_Yxyz.y, 4);
                    measureData.Y = Math.Round(Meas_Yxyz.Y, 4) + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                    measureDataList_Red.Add(measureData);

                    listBoxGammaLog.Items.Add($"{gray_level}\t{Meas_Yxyz.x.ToString("F4")}\t{Meas_Yxyz.y.ToString("F4")}\t{Meas_Yxyz.Y.ToString("F4")}");
                    UpdateData(measureData.x, measureData.y, measureData.Y);
                    listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);
                }
                if (devIsConnect)
                {
                    //  For Gamma measure
                    double[] measdataArr_Y = new double[measureDataList_Red.Count];
                    double[] measdataArr_P = new double[measureDataList_Red.Count];
                    for (int i = 0; i < measureDataList_Red.Count; i++)
                    {
                        measdataArr_Y[i] = measureDataList_Red[i].Y;
                        measdataArr_P[i] = measureDataList_Red[i].gray_level;
                    }
                    gamma_result_R = GammaStandard.getGamma(measdataArr_Y);
                    Console.WriteLine($"Test function Gamma (ONYX) :{GammaStandard.getGamma(measdataArr_Y)}");
                    Console.WriteLine($"Test function Gamma (OLD VERSION) :{GammaStandard.gammaCalculte(measdataArr_P, measdataArr_Y)}");

                    listBoxGammaLog.Items.Add($"Gamma Result : {gamma_result_R.ToString("F2")}");
                    listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);

                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Please check i1D3 connection");
                    Close();
                    disconnectProcess?.Invoke();
                }
                //************************************************** Measure Green ********************************************************//              
                if (isStopPressed) return;

                Console.WriteLine($"Color value is 0");
                recGamma.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                await Task.Delay(1500);
                listBoxGammaLog.Items.Add($"Start Measure Green:");
                listBoxGammaLog.Items.Add($"Level\tx\ty\tY");
                devIsConnect = _sensor.MeasYxyz(out Meas_Yxyz);
                listBoxGammaLog.Items.Add($"0\t{Meas_Yxyz.x.ToString("F4")}\t{Meas_Yxyz.y.ToString("F4")}\t{Meas_Yxyz.Y.ToString("F4")}");
                listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);

                // Save low data to list
                lum_low = Meas_Yxyz.Y + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                gammaMeasureData measureData_low_g = new gammaMeasureData();
                measureData_low_g.gray_level = 0;
                measureData_low_g.x = Math.Round(Meas_Yxyz.x, 4);
                measureData_low_g.y = Math.Round(Meas_Yxyz.y, 4);
                measureData_low_g.Y = Math.Round(Meas_Yxyz.Y, 4) + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                measureDataList_Green.Add(measureData_low_g);
                for (int level = 1; level < sample_num; level++)
                {
                    if (buttonStart.Content.ToString() != "Stop") return;
                    if (isStopPressed || devIsConnect == false) return;
                    int gray_level = 255 * level / (sample_num - 1);
                    Console.WriteLine($"Gray level is {level}");
                    byte levelByte = Convert.ToByte(gray_level);
                    recGamma.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, levelByte, 0));
                    await Task.Delay(500);// zh modify
                    devIsConnect = _sensor.MeasYxyz(out Meas_Yxyz);

                    // Save measure data to list
                    gammaMeasureData measureData = new gammaMeasureData();
                    measureData.gray_level = gray_level;
                    measureData.x = Math.Round(Meas_Yxyz.x, 4);
                    measureData.y = Math.Round(Meas_Yxyz.y, 4);
                    measureData.Y = Math.Round(Meas_Yxyz.Y, 4) + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                    measureDataList_Green.Add(measureData);

                    listBoxGammaLog.Items.Add($"{gray_level}\t{Meas_Yxyz.x.ToString("F4")}\t{Meas_Yxyz.y.ToString("F4")}\t{Meas_Yxyz.Y.ToString("F4")}");
                    UpdateData(measureData.x, measureData.y, measureData.Y);
                    listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);
                }
                if (devIsConnect)
                {
                    //  For Gamma measure
                    double[] measdataArr_Y = new double[measureDataList_Green.Count];
                    double[] measdataArr_P = new double[measureDataList_Green.Count];
                    for (int i = 0; i < measureDataList_Green.Count; i++)
                    {
                        measdataArr_Y[i] = measureDataList_Green[i].Y;
                        measdataArr_P[i] = measureDataList_Green[i].gray_level;
                    }
                    gamma_result_G = GammaStandard.getGamma(measdataArr_Y);
                    Console.WriteLine($"Test function Gamma (ONYX) :{GammaStandard.getGamma(measdataArr_Y)}");
                    Console.WriteLine($"Test function Gamma (OLD VERSION) :{GammaStandard.gammaCalculte(measdataArr_P, measdataArr_Y)}");

                    listBoxGammaLog.Items.Add($"Gamma Result : {gamma_result_G.ToString("F2")}");
                    listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Please check i1D3 connection");
                    Close();
                    disconnectProcess?.Invoke();
                }
                //************************************************** Measure Blue ********************************************************//              
                if (isStopPressed) return;

                Console.WriteLine($"Color value is 0");
                recGamma.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                await Task.Delay(1500);
                listBoxGammaLog.Items.Add($"Start Measure Blue:");
                listBoxGammaLog.Items.Add($"Level\tx\ty\tY");
                devIsConnect = _sensor.MeasYxyz(out Meas_Yxyz);
                listBoxGammaLog.Items.Add($"0\t{Meas_Yxyz.x.ToString("F4")}\t{Meas_Yxyz.y.ToString("F4")}\t{Meas_Yxyz.Y.ToString("F4")}");
                listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);

                // Save low data to list
                lum_low = Meas_Yxyz.Y + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                gammaMeasureData measureData_low_b = new gammaMeasureData();
                measureData_low_b.gray_level = 0;
                measureData_low_b.x = Math.Round(Meas_Yxyz.x, 4);
                measureData_low_b.y = Math.Round(Meas_Yxyz.y, 4);
                measureData_low_b.Y = Math.Round(Meas_Yxyz.Y, 4) + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                measureDataList_Blue.Add(measureData_low_b);
                for (int level = 1; level < sample_num; level++)
                {
                    if (buttonStart.Content.ToString() != "Stop") return;
                    if (isStopPressed || devIsConnect == false) return;
                    int gray_level = 255 * level / (sample_num - 1);
                    Console.WriteLine($"Gray level is {level}");
                    byte levelByte = Convert.ToByte(gray_level);
                    recGamma.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, levelByte));
                    await Task.Delay(500);// zh modify
                    devIsConnect = _sensor.MeasYxyz(out Meas_Yxyz);

                    // Save measure data to list
                    gammaMeasureData measureData = new gammaMeasureData();
                    measureData.gray_level = gray_level;
                    measureData.x = Math.Round(Meas_Yxyz.x, 4);
                    measureData.y = Math.Round(Meas_Yxyz.y, 4);
                    measureData.Y = Math.Round(Meas_Yxyz.Y, 4) + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                    measureDataList_Blue.Add(measureData);

                    listBoxGammaLog.Items.Add($"{gray_level}\t{Meas_Yxyz.x.ToString("F4")}\t{Meas_Yxyz.y.ToString("F4")}\t{Meas_Yxyz.Y.ToString("F4")}");
                    UpdateData(measureData.x, measureData.y, measureData.Y);
                    listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);
                }
                if (devIsConnect)
                {
                    //  For Gamma measure
                    double[] measdataArr_Y = new double[measureDataList.Count];
                    double[] measdataArr_P = new double[measureDataList.Count];
                    for (int i = 0; i < measureDataList_Blue.Count; i++)
                    {
                        measdataArr_Y[i] = measureDataList_Blue[i].Y;
                        measdataArr_P[i] = measureDataList_Blue[i].gray_level;
                    }
                    gamma_result_B = GammaStandard.getGamma(measdataArr_Y);
                    Console.WriteLine($"Test function Gamma (ONYX) :{GammaStandard.getGamma(measdataArr_Y)}");
                    Console.WriteLine($"Test function Gamma (OLD VERSION) :{GammaStandard.gammaCalculte(measdataArr_P, measdataArr_Y)}");

                    listBoxGammaLog.Items.Add($"Gamma Result : {gamma_result_B.ToString("F2")}");
                    listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);
                    buttonStart.Content = "Start";
                    buttonExport.IsEnabled = true;//zh251107 add
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Please check i1D3 connection");
                    Close();
                    disconnectProcess?.Invoke();
                }
            }
            else if (Item == 2)
            {
                // The sample number is special in DICOM
                mMeasureData = new ColorData[sample_num + 2];
                mStandardData = new ColorData[sample_num + 2];
                mDeltaE = new double[sample_num + 2];

                Console.WriteLine($"Color value is 0");
                recGamma.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                await Task.Delay(1500);
                listBoxGammaLog.Items.Add($"Start Measure DICOM:");
                listBoxGammaLog.Items.Add($"Level\tx\ty\tY");
                devIsConnect = _sensor.MeasYxyz(out Meas_Yxyz);
                listBoxGammaLog.Items.Add($"0\t{Meas_Yxyz.x.ToString("F4")}\t{Meas_Yxyz.y.ToString("F4")}\t{Meas_Yxyz.Y.ToString("F4")}");
                listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);

                // Save low data to list 
                double lum_low = Meas_Yxyz.Y + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                gammaMeasureData measureData_low = new gammaMeasureData();
                measureData_low.gray_level = 0;
                measureData_low.x = Math.Round(Meas_Yxyz.x, 4);
                measureData_low.y = Math.Round(Meas_Yxyz.y, 4);
                measureData_low.Y = Math.Round(Meas_Yxyz.Y, 4) + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                measureDataList.Add(measureData_low);
                for (int level = 1; level < sample_num + 2; level++)
                {
                    if (buttonStart.Content.ToString() != "Stop") return;
                    if (devIsConnect == false) return;
                    int gray_level = (256 / sample_num - 1) * level;
                    Console.WriteLine($"Gray level is {level}");
                    byte levelByte = Convert.ToByte(gray_level);
                    recGamma.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(levelByte, levelByte, levelByte));
                    await Task.Delay(500);// zh modify
                    devIsConnect = _sensor.MeasYxyz(out Meas_Yxyz);

                    // Save measure data to list
                    gammaMeasureData measureData = new gammaMeasureData();
                    measureData.gray_level = gray_level;
                    measureData.x = Math.Round(Meas_Yxyz.x, 4);
                    measureData.y = Math.Round(Meas_Yxyz.y, 4);
                    measureData.Y = Math.Round(Meas_Yxyz.Y, 4) + Convert.ToDouble(textBoxAmbientLight.Text);// zh modify
                    measureDataList.Add(measureData);

                    listBoxGammaLog.Items.Add($"{gray_level}\t{Meas_Yxyz.x.ToString("F4")}\t{Meas_Yxyz.y.ToString("F4")}\t{Meas_Yxyz.Y.ToString("F4")}");
                    UpdateData(measureData.x, measureData.y, measureData.Y);
                    listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);
                }
                if (devIsConnect)
                {
                    double[] DICOMMeasureArr_Y = new double[measureDataList.Count];
                    // Calculate measure Y to JND
                    for (int i = 0; i < measureDataList.Count; i++)
                    {
                        DICOMMeasureArr_JND[i] = JNDCalculator.LuminanceToJND(measureDataList[i].Y);
                        DICOMMeasureArr_Y[i] = measureDataList[i].Y;
                    }

                    // Get DICOM standard Y and JND
                    DICOMStandardArr_Y = GammaStandard.DICOMStandard(lum_low, measureDataList[measureDataList.Count - 1].Y, 18);

                    // ensure  DICOMStandardArr_JND matches DICOMStandardArr_Y size
                    DICOMStandardArr_JND = new double[DICOMStandardArr_Y.Length];

                    for (int i = 0; i < DICOMStandardArr_Y.Length; i++)
                    {
                        DICOMStandardArr_JND[i] = JNDCalculator.LuminanceToJND(DICOMStandardArr_Y[i]);

                    }
                    listBoxGammaLog.Items.Add($"GSDF Deviations :");
                    listBoxGammaLog.Items.Add($"Level.\t0:\tnull");
                    double[] deviations = LumDeviation.Deviation(DICOMMeasureArr_Y, DICOMStandardArr_Y, 18);
                    string DICOM_result = "PASS";
                    for (int i = 0; i < deviations.Length; i++)
                    {
                        string dev_result = Math.Abs(deviations[i]) < 15 ? "PASS" : "FAILED";
                        listBoxGammaLog.Items.Add($"Level\t{15 * (i + 1)}:\t{deviations[i].ToString("F2")}%\t{dev_result}");
                        listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);
                        if (dev_result == "FAILED") DICOM_result = "FAILED";
                    }

                    // For Delta E calculation
                    for (int i = 0; i < sample_num + 2; i++)
                    {
                        mMeasureData[i] = new ColorData(measureDataList[i].x, measureDataList[i].y, measureDataList[i].Y, ColorData.MODE_xyY);
                    }
                    for (int i = 0; i < sample_num + 2; i++)
                    {
                        mStandardData[i] = new ColorData(target_CT_x, target_CT_y, DICOMStandardArr_Y[i], ColorData.MODE_xyY);
                    }
                    for (int i = 0; i < sample_num + 2; i++)
                    {
                        LAB measure_lab = new LAB(mMeasureData[i], LAB.REF_WHITE_D65);
                        LAB standard_lab = new LAB(mStandardData[i], LAB.REF_WHITE_D65);
                        mDeltaE[i] = DeltaE2000.DeltaE(measure_lab, standard_lab);
                    }
                    // Filter data if its graylevel smaller than 60
                    for (int i = 4; i < sample_num + 2; i++)
                    {
                        mAverDeltaE += mDeltaE[i];
                    }
                    mAverDeltaE /= (sample_num - 4 + 2);
                    mMaxDeltaE = mDeltaE.Skip(4).Max();
                    Console.WriteLine($"Delta E average : {mAverDeltaE}, Delta E Max : {mMaxDeltaE}");

                    listBoxGammaLog.Items.Add($"DICOM Result : {DICOM_result}");

                    listBoxGammaLog.ScrollIntoView(listBoxGammaLog.Items[listBoxGammaLog.Items.Count - 1]);

                    DICOMMeasured?.Invoke(DICOM_result);       ///wu 250401 add   傳值到blackwindow 中的listbox 用

                    buttonStart.Content = "Start";
                    buttonExport.IsEnabled = true;//zh251107 add
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Please check i1D3 connection");
                    Close();
                    disconnectProcess?.Invoke();
                }
            }
            buttonStart.IsEnabled = true;
            comboMeasureItem.IsEnabled = true;
            comboTargetGamma.IsEnabled = true;
            comboSampleNumber.IsEnabled = true;
            textBoxAmbientLight.IsEnabled = true;
            if (comboMeasureItem.SelectedIndex == 0 || comboMeasureItem.SelectedIndex == 1)
                if (comboSampleNumber.SelectedIndex == 0) buttonExport.IsEnabled = false;
                else buttonExport.IsEnabled = true;
        }

        private  async void buttonExport_Click(object sender, RoutedEventArgs e)
        {
            //  var calibrationPage = Calibration as Calibration;
        
  
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow; 
            var protocol = new CalibrationProtocol(mainWindow.GetI2CController());
            var i2cController = mainWindow.GetI2CController();

            /////SNNumber
            byte[] GetSNNumberCommandArray =
                       { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND, 0x07, (byte)VCCode.         ONYX_FCODE_12_GET_SERIAL_NUMBER      , 0x00, 0x00 };

     

            byte retval_SN, retryCount_SN = 0;
           
                do
                {
                    retval_SN = i2cController.DDCCI_Send_Command(GetSNNumberCommandArray, mainWindow.getSNNumber);
                    Debug.WriteLine($"ret val = {retval_SN}, retryCount = {++retryCount_SN}");  // Record retry times
                } while (retval_SN != 0 && retryCount_SN <= 10);

            await Task.Delay(300);

            ///Firmware Version
            byte[] GetFirmwareCommandArray =
                            { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND, 0x07, (byte)VCCode.      ONYX_FCODE_13_GET_FIRMWARE_VERSION  , 0x00, 0x00 };

            //                        byte[] GetFirmwareCommandArray =
            //              { (byte)Destination.ONYX_MONITOR_STM_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
            //(byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND, 0x07, (byte)VCCode.ONYX_FCODE_13_GET_FIRMWARE_VERSION, 0x00, 0x00 };

    

            byte retval_FW, retryCount_FW = 0;
           
                do
                {
                    retval_FW = i2cController.DDCCI_Send_Command(GetFirmwareCommandArray, mainWindow.getFirmwareVersion);
                    Debug.WriteLine($"ret val = {retval_FW}, retryCount = {++retryCount_FW}");  // Record retry times
                } while (retval_FW != 0 && retryCount_FW <= 10);
            await Task.Delay(300);

            if (buttonExport.IsEnabled)
            {
                try
                {
                    // Output listbox log 
                    string str = comboTargetGamma.Text;
                    str = str.Replace(" ", "");
                    StreamWriter sw = new StreamWriter("./" + str + "_Measure.txt");
                    foreach (string log in listBoxGammaLog.Items)
                    {
                        sw.WriteLine(log);
                    }
                    sw.Close();
                    // Excel 
                    // Output
                    string excel_output_FilePath = "Gamma_report.xlsx";//zh add

                    // Set file path to Gamma report example
                    string excel_example_FilePath = "./gamma_report_example.xlsx";//zh note move to ../bin/debug
                                                                                  // Create a new Excel application
                    Excel.Application excelapp = new Excel.Application();

                    // Open Excel file
                    Excel.Workbook workbook = null;
                    if (System.IO.File.Exists(excel_output_FilePath))//zh add
                    {
                        workbook = excelapp.Workbooks.Open(Path.GetFullPath(excel_output_FilePath));//zh add
                    }
                    else
                    {
                        workbook = excelapp.Workbooks.Open(Path.GetFullPath(excel_example_FilePath));
                    }

                    // Set worksheet
                    int worksheet_target = 1; // 1 : Gamma 1.8, 2 : Gamma 2.0, 3 : Gamma 2.2, 4 : Gamma 2.4, 5 : Gamma 2.6, 6 : DICOM
                    if (comboMeasureItem.SelectedIndex == 0 || comboMeasureItem.SelectedIndex == 1)
                    {
                        switch (comboTargetGamma.SelectedIndex)
                        {
                            case 0:
                                worksheet_target = 1;
                                break;
                            case 1:
                                worksheet_target = 2;
                                break;
                            case 2:
                                worksheet_target = 3;
                                break;
                            case 3:
                                worksheet_target = 4;
                                break;
                            case 4:
                                worksheet_target = 5;
                                break;
                            default:
                                break;
                        }
                    }
                    else if (comboMeasureItem.SelectedIndex == 2)
                    {
                        worksheet_target = 6;
                    }



                    Excel.Worksheet worksheet = workbook.Worksheets[worksheet_target];

                    // Read data
                    int rowCount = worksheet.UsedRange.Rows.Count;
                    int colCount = worksheet.UsedRange.Columns.Count;

                    if (comboMeasureItem.SelectedIndex == 2)   // For DICOM measure
                    {
                        // Summary
                        //wu 251114 add SN number FW verison to export
        
                        worksheet.Cells[2, 5].Value2 = mainWindow.textBoxModelSerialNumber.Text;       // E2: SN
                        worksheet.Cells[3, 3].Value2 = mainWindow.textBoxModelFirmwareVersion.Text;
                   
               
                        Debug.WriteLine($"[Export] SN={mainWindow.textBoxModelSerialNumber.Text}, FW={mainWindow.textBoxModelFirmwareVersion.Text}");
                        //wu 251114 add SN number FW verison to export

                        Excel.Range cell_ambientlight = worksheet.Cells[7, 5];
                        cell_ambientlight.Value = textBoxAmbientLight.Text;
                        Excel.Range cell_performed = worksheet.Cells[8, 5];
                        cell_performed.Value = "Performed";
                        Excel.Range cell_date = worksheet.Cells[3, 6];
                        cell_date.Value = $"{DateTime.Now:yyyy/MM/dd HH:mm}";
                        //{now.Year}/{now.Month}/{now.Day},     {now:HH:mm:ss}
                        Excel.Range cell_title = worksheet.Cells[1, 1];
                        cell_title.Value = $"DICOM + {target_colortemp}K";
                        Excel.Range cell_CT = worksheet.Cells[12, 3];
                        cell_CT.Value = target_colortemp;
                        Excel.Range cell_AverDeltaE = worksheet.Cells[13, 5];
                        cell_AverDeltaE.Value = mAverDeltaE;
                        Excel.Range cell_MaxDeltaE = worksheet.Cells[14, 5];
                        cell_MaxDeltaE.Value = mMaxDeltaE;
                        // Data table
                        if (measureDataList.Count == 18)    // Fill measure Y
                        {
                            for (int i = 22; i <= 39; i++)
                            {
                                Excel.Range cell_dll = worksheet.Cells[i, 1];//zh add
                                cell_dll.Value = measureDataList[i - 22].gray_level;//zh add
                                Excel.Range cell_x = worksheet.Cells[i, 2];
                                cell_x.Value = measureDataList[i - 22].x;
                                Excel.Range cell_y = worksheet.Cells[i, 3];
                                cell_y.Value = measureDataList[i - 22].y;
                                Excel.Range cell_Y = worksheet.Cells[i, 4];
                                cell_Y.Value = measureDataList[i - 22].Y;
                            }
                        }

                        if (DICOMStandardArr_Y.Length == 18)    // Fill measure JND
                        {
                            for (int i = 22; i <= 39; i++)
                            {
                                Excel.Range cell = worksheet.Cells[i, 5];
                                cell.Value = Math.Round(DICOMMeasureArr_JND[i - 22], 4);
                            }
                        }

                        if (DICOMStandardArr_Y.Length == 18)    // Fill standard Y
                        {
                            for (int i = 22; i <= 39; i++)
                            {
                                Excel.Range cell_x = worksheet.Cells[i, 12];
                                cell_x.Value = target_CT_x;
                                Excel.Range cell_y = worksheet.Cells[i, 13];
                                cell_y.Value = target_CT_y;
                                Excel.Range cell = worksheet.Cells[i, 14];
                                cell.Value = Math.Round(DICOMStandardArr_Y[i - 22], 4);
                            }
                        }

                        if (DICOMStandardArr_JND.Length == 18)  // Fill standard JND
                        {
                            for (int i = 22; i <= 39; i++)
                            {
                                Excel.Range cell = worksheet.Cells[i, 15];
                                cell.Value = Math.Round(DICOMStandardArr_JND[i - 22], 4);
                            }
                        }
                    }
                    else // For Gamma measure 
                    {
                        // Summary
                        worksheet.Cells[2, 5].Value2 = mainWindow.textBoxModelSerialNumber.Text;       // E2: SN
                        worksheet.Cells[3, 3].Value2 = mainWindow.textBoxModelFirmwareVersion.Text;


                        Debug.WriteLine($"[Export] SN={mainWindow.textBoxModelSerialNumber.Text}, FW={mainWindow.textBoxModelFirmwareVersion.Text}");

                        Excel.Range cell_ambientlight = worksheet.Cells[7, 5];
                        cell_ambientlight.Value = textBoxAmbientLight.Text;
                        Excel.Range cell_performed = worksheet.Cells[8, 5];
                        cell_performed.Value = "Performed";
                        Excel.Range cell_date = worksheet.Cells[3, 6];
                        cell_date.Value = $"{DateTime.Now:yyyy/MM/dd HH:mm}";
                        Excel.Range cell_AverDeltaE = worksheet.Cells[13, 5];
                        cell_AverDeltaE.Value = mAverDeltaE;
                        Excel.Range cell_MaxDeltaE = worksheet.Cells[14, 5];
                        cell_MaxDeltaE.Value = mMaxDeltaE;
                        Excel.Range cell_GammaResult = worksheet.Cells[17, 5];
                        cell_GammaResult.Value = gamma_result;
                        // Standard data fill in excel
                        if (standardDataList.Count >= 32)
                        {
                            for (int i = 21; i <= 36; i++)
                            {
                                int divCount = (standardDataList.Count - 1) * (i - 21) / (32 - 1);
                                Excel.Range cell_dll = worksheet.Cells[i, 1];//zh add
                                cell_dll.Value = standardDataList[divCount].gray_level;//zh add
                                Excel.Range cell = worksheet.Cells[i, 4];
                                cell.Value = standardDataList[divCount].luminance;
                            }
                            for (int i = 21; i <= 36; i++)
                            {
                                int divCount = (standardDataList.Count - 1) * (i + 16 - 21) / (32 - 1);
                                Excel.Range cell_dll = worksheet.Cells[i, 5];//zh add
                                cell_dll.Value = standardDataList[divCount].gray_level;//zh add
                                Excel.Range cell = worksheet.Cells[i, 8];
                                cell.Value = standardDataList[divCount].luminance;
                            }
                        }
                        if (standardDataList_plus009.Count >= 32)
                        {
                            for (int i = 40; i <= 55; i++)
                            {
                                int divCount = (standardDataList_plus009.Count - 1) * (i - 40) / (32 - 1);
                                Excel.Range cell_dll = worksheet.Cells[i, 11];//zh add
                                cell_dll.Value = standardDataList_plus009[divCount].gray_level;//zh add
                                Excel.Range cell = worksheet.Cells[i, 14];
                                cell.Value = standardDataList_plus009[divCount].luminance;
                            }
                            for (int i = 40; i <= 55; i++)
                            {
                                int divCount = (standardDataList_plus009.Count - 1) * (i + 16 - 40) / (32 - 1);
                                Excel.Range cell_dll = worksheet.Cells[i, 15];//zh add
                                cell_dll.Value = standardDataList_plus009[divCount].gray_level;//zh add
                                Excel.Range cell = worksheet.Cells[i, 18];
                                cell.Value = standardDataList_plus009[divCount].luminance;
                            }
                        }
                        // Measure data fill in excel 
                        if (measureDataList.Count >= 32)
                        {
                            for (int i = 40; i <= 55; i++)
                            {
                                int divCount = (measureDataList.Count - 1) * (i - 40) / (32 - 1);
                                Excel.Range cell_dll = worksheet.Cells[i, 1];//zh add
                                cell_dll.Value = measureDataList[divCount].gray_level;//zh add
                                Excel.Range cell_x = worksheet.Cells[i, 2];
                                cell_x.Value = measureDataList[divCount].x;
                                Excel.Range cell_y = worksheet.Cells[i, 3];
                                cell_y.Value = measureDataList[divCount].y;
                                Excel.Range cell_Y = worksheet.Cells[i, 4];
                                cell_Y.Value = measureDataList[divCount].Y;
                            }
                            for (int i = 40; i <= 55; i++)
                            {
                                int divCount = (measureDataList.Count - 1) * (i + 16 - 40) / (32 - 1);
                                Excel.Range cell_dll = worksheet.Cells[i, 5];//zh add
                                cell_dll.Value = measureDataList[divCount].gray_level;//zh add
                                Excel.Range cell_x = worksheet.Cells[i, 6];
                                cell_x.Value = measureDataList[divCount].x;
                                Excel.Range cell_y = worksheet.Cells[i, 7];
                                cell_y.Value = measureDataList[divCount].y;
                                Excel.Range cell_Y = worksheet.Cells[i, 8];
                                cell_Y.Value = measureDataList[divCount].Y;
                            }
                        }

                        if (measureDataList_Red.Count >= 32)
                        {
                            for (int i = 59; i <= 74; i++)
                            {
                                int divCount = (measureDataList_Red.Count - 1) * (i - 59) / (32 - 1);
                                Excel.Range cell_dll = worksheet.Cells[i, 1];//zh add
                                cell_dll.Value = measureDataList_Red[divCount].gray_level;//zh add
                                Excel.Range cell_x = worksheet.Cells[i, 2];
                                cell_x.Value = measureDataList_Red[divCount].x;
                                Excel.Range cell_y = worksheet.Cells[i, 3];
                                cell_y.Value = measureDataList_Red[divCount].y;
                                Excel.Range cell_Y = worksheet.Cells[i, 4];
                                cell_Y.Value = measureDataList_Red[divCount].Y;
                            }
                            for (int i = 59; i <= 74; i++)
                            {
                                int divCount = (measureDataList_Red.Count - 1) * (i + 16 - 59) / (32 - 1);
                                Excel.Range cell_dll = worksheet.Cells[i, 5];//zh add
                                cell_dll.Value = measureDataList_Red[divCount].gray_level;//zh add
                                Excel.Range cell_x = worksheet.Cells[i, 6];
                                cell_x.Value = measureDataList_Red[divCount].x;
                                Excel.Range cell_y = worksheet.Cells[i, 7];
                                cell_y.Value = measureDataList_Red[divCount].y;
                                Excel.Range cell_Y = worksheet.Cells[i, 8];
                                cell_Y.Value = measureDataList_Red[divCount].Y;
                            }
                        }

                        if (measureDataList_Green.Count >= 32)
                        {
                            for (int i = 78; i <= 93; i++)
                            {
                                int divCount = (measureDataList_Green.Count - 1) * (i - 78) / (32 - 1);
                                Excel.Range cell_dll = worksheet.Cells[i, 1];//zh add
                                cell_dll.Value = measureDataList_Green[divCount].gray_level;//zh add
                                Excel.Range cell_x = worksheet.Cells[i, 2];
                                cell_x.Value = measureDataList_Green[divCount].x;
                                Excel.Range cell_y = worksheet.Cells[i, 3];
                                cell_y.Value = measureDataList_Green[divCount].y;
                                Excel.Range cell_Y = worksheet.Cells[i, 4];
                                cell_Y.Value = measureDataList_Green[divCount].Y;
                            }
                            for (int i = 78; i <= 93; i++)
                            {
                                int divCount = (measureDataList_Green.Count - 1) * (i + 16 - 78) / (32 - 1);
                                Excel.Range cell_cell = worksheet.Cells[i, 5];//zh add
                                cell_cell.Value = measureDataList_Green[divCount].gray_level;//zh add
                                Excel.Range cell_x = worksheet.Cells[i, 6];
                                cell_x.Value = measureDataList_Green[divCount].x;
                                Excel.Range cell_y = worksheet.Cells[i, 7];
                                cell_y.Value = measureDataList_Green[divCount].y;
                                Excel.Range cell_Y = worksheet.Cells[i, 8];
                                cell_Y.Value = measureDataList_Green[divCount].Y;
                            }
                        }

                        if (measureDataList_Blue.Count >= 32)
                        {
                            for (int i = 97; i <= 112; i++)
                            {
                                int divCount = (measureDataList_Blue.Count - 1) * (i - 97) / (32 - 1);
                                Excel.Range cell_dll = worksheet.Cells[i, 1];//zh add
                                cell_dll.Value = measureDataList_Blue[divCount].gray_level;//zh add
                                Excel.Range cell_x = worksheet.Cells[i, 2];
                                cell_x.Value = measureDataList_Blue[divCount].x;
                                Excel.Range cell_y = worksheet.Cells[i, 3];
                                cell_y.Value = measureDataList_Blue[divCount].y;
                                Excel.Range cell_Y = worksheet.Cells[i, 4];
                                cell_Y.Value = measureDataList_Blue[divCount].Y;
                            }
                            for (int i = 97; i <= 112; i++)
                            {
                                int divCount = (measureDataList_Blue.Count - 1) * (i + 16 - 97) / (32 - 1);
                                Excel.Range cell_dll = worksheet.Cells[i, 5];//zh add
                                cell_dll.Value = measureDataList_Blue[divCount].gray_level;//zh add
                                Excel.Range cell_x = worksheet.Cells[i, 6];
                                cell_x.Value = measureDataList_Blue[divCount].x;
                                Excel.Range cell_y = worksheet.Cells[i, 7];
                                cell_y.Value = measureDataList_Blue[divCount].y;
                                Excel.Range cell_Y = worksheet.Cells[i, 8];
                                cell_Y.Value = measureDataList_Blue[divCount].Y;
                            }
                        }
                    }

                    try//zh add
                    {
                        Console.WriteLine($"CurrentDirectory is {Environment.CurrentDirectory}");
                        workbook.SaveAs(Environment.CurrentDirectory + "\\Gamma_report.xlsx");
                        workbook.Close();
                        System.Windows.MessageBox.Show("報告匯出完成！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);//zh251107 add
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("Please close the file and try again");
                    }

                    excelapp.Quit();

                    System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(excelapp);

                    Console.ReadLine();
                }
                catch (Exception ex)//zh251107 add
                {
                    System.Windows.Forms.MessageBox.Show("Please Install Windows Office first!!!");
                }
            }

        }

        public event Action ButtonStateChanged;
        public event Action disconnectProcess;
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ButtonStateChanged?.Invoke();
        }


        public event Action<string> DataAdded;
        public event Action<double, double, double> DataUpdated;

        public void AddDataToGamma(string log)
        {
            listBoxGammaLog.Items.Add(log);
            DataAdded?.Invoke(log);
        }

        public void UpdateData(double x, double y, double Y)
        {
            DataUpdated?.Invoke(x, y, Y);
        }



        ///wu 250401 add  為了要讓gammawindow 中固定sample number 和量測white 
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            comboMeasureItem.SelectedIndex = 0;
            comboMeasureItem.IsEnabled = false;

            comboSampleNumber.Items.Clear();
            comboSampleNumber.Items.Add("16");
            comboSampleNumber.Items.Add("32");
            comboSampleNumber.Items.Add("64");
            comboSampleNumber.Items.Add("128");
            comboSampleNumber.Items.Add("256");

            comboSampleNumber.SelectedIndex = 1;
            comboSampleNumber.IsEnabled = false;
        }



    }
}