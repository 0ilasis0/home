using OnyxSensor;
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
using System.Windows.Shapes;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Application = System.Windows.Application;
using System.Reflection.Emit;
using ONYX.Utilities;
using static MonitorFactoryTool.Pages.Gamma;
using Button = System.Windows.Controls.Button;
using System.Diagnostics.Contracts;
using System.Reflection;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using ONYX_DataType;
using OfficeOpenXml.Interfaces.Drawing.Text;
using MonitorFactoryTool.ONYX.Utilities;
using System.Windows.Threading;
using static OfficeOpenXml.ExcelErrorValue;
using System.Threading;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using static MonitorFactoryTool.ONYX.Utilities.CalibrationProtocol;
using FDTI_Factory_i2c;

namespace MonitorFactoryTool.Pages
{
    /// <summary>
    /// BlackWindow.xaml 的互動邏輯
    /// </summary>
    public partial class BlackWindow : Window
    {
        // 用來保存 9 個白色方塊
        private List<Rectangle> _whiteSquares = new List<Rectangle>();
        //  public SensorBase sensor_1 = i1d3.getInstance();
        private Calibration calibWindow;

        public int GammaCount = 0; // 計算Gamma量測次數 
        public int DICOMCount = 0; // 計算Gamma量測次數 
        private Screen targetScreen;///selectmonitor使用
        private Screen _targetScreen;
        private int _screenSizeCm; // 使用 SelectMonitorForm 的螢幕對角線長度（cm）
        public SampleReport sampleReport = new SampleReport();
        public Rectangle whiteBox;
        private bool isMeaureRectangle;
        public int ScreenSizeInInches { get; private set; }

        private BlackWindowSource _source;///20250502 add 判斷blackwindow是從verify button 進去還是calibration button 進去
        private CalibProcess calibProcess;///20250502 add   用來calibration 
        private Rectangle contrastBox;// 量測contrast 流程中用的白色方框  每個流程的方框分開
        private Action<string> _log;  // 用來統一把訊息丟到 listBoxInfo 250613 wu add 用來統整blackwindow 的list 
        private CancellationTokenSource _cts;       //  CancellationTokenSource 用來控制當blackwindow exit 按下去要關閉背景task 
        private bool _taskCompleted = false;    // Derek add 
        private int gamma6500_Index = 1;   // 250703 wu add 用於記錄目前是 6500K 的第幾個 Gamma 項目
        private PanelDetailInfo _panelInfo;

        public async Task StartCalibration(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            var protocol = new CalibrationProtocol(mainWindow.GetI2CController());

            PanelDetailInfo panelInfo = await protocol.GetPanelDetailAsync();

            // 若讀取失敗就給一組安全預設值(FHD的設定)
            if (panelInfo == null)
                panelInfo = new PanelDetailInfo(false, 10, 1024, 12, 4095);

            _panelInfo = panelInfo;


            // 從 MainWindow 取得已經連接好的感測器
            SensorBase currentSensor = mainWindow.ConnectedSensor;
            // 強制打開左邊 + 底部的所有 panel
            this.TopPanel.Visibility = Visibility.Visible;
            this.BottomPanel.Visibility = Visibility.Visible;
            this.MeasureInfoPanel.Visibility = Visibility.Visible;

            calibProcess = new CalibProcess(currentSensor, _panelInfo);
            // 右側數值面板
            calibProcess.UpdateUIAction = this.UpdateMeasureInfo;

            // 左側 ListBox
            calibProcess.Event_ConsoleWriteLine += msg =>
                Dispatcher.Invoke(() => listBoxInfo.Items.Add(msg));

            Console.WriteLine("[BlackWindow] 開始 async calibration");

            await calibProcess.doWork(token);

            // 校正跑完後，再一次把可能殘留的白框清掉
            await ClearAllCalibrationRectanglesAsync();
        }


        public BlackWindow(Screen selectedScreen, int screenSizeInInches, BlackWindowSource source)
        {
            InitializeComponent();

            _source = source;
            targetScreen = selectedScreen ?? Screen.PrimaryScreen;
            _targetScreen = selectedScreen;

            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Left = targetScreen.Bounds.Left;
            this.Top = targetScreen.Bounds.Top;
            this.Width = targetScreen.Bounds.Width;
            this.Height = targetScreen.Bounds.Height;
            this.WindowState = WindowState.Normal;
            this.WindowStyle = WindowStyle.None;
            this.Topmost = true;

            this.ScreenSizeInInches = screenSizeInInches;

            Console.WriteLine($"[BlackWindow] Window position set to Left={this.Left}, Top={this.Top}");
            Console.WriteLine($"[BlackWindow] Target screen bounds: {targetScreen.Bounds}");
            Console.WriteLine($"[BlackWindow] Called from: {_source}");
        }

        private async void BlackWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
            var calibWindow = new Calibration();
            var mainWin = (MainWindow)Application.Current.MainWindow;
            var calibrationPage = mainWin.Calibration.Content as Calibration;
            //Console.WriteLine($"[BlackWindow] Loaded HashCode = {this.GetHashCode()}");

            // 0414
            // choose selected Screen
            var workingArea = targetScreen.Bounds;
            this.Left = workingArea.Left;
            this.Top = workingArea.Top;
            this.WindowState = WindowState.Maximized;

            // 一打開blackwindow 的白色方框
            Rectangle whiteBox = CreateCenteredWhiteBox();
            whiteBox.Fill = Brushes.White;
            canvasRoot.Children.Add(whiteBox);
            this.whiteBox = whiteBox;

            await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Render);
        }
        public void UpdateMeasureInfo(SensorMeasureYxy_t data)
        {
            Dispatcher.Invoke(() =>
            {
                // Console.WriteLine($"[BlackWindow UI] Ready to update: {textboxMeasureInfoX != null}");

                textboxMeasureInfoX.Text = data.x.ToString("F4");
                textboxMeasureInfoSmallY.Text = data.y.ToString("F4");
                textboxMeasureInfoY.Text = data.Y.ToString("F4");

                double CT_n = (data.x - 0.3320) / (data.y - 0.1858);
                double CCT = -437 * Math.Pow(CT_n, 3) + 3601 * Math.Pow(CT_n, 2) - 6831 * CT_n + 5517;
                textboxMeasureInfoT.Text = CCT.ToString("F2");
            });
        }
        private async void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            // blackwindow 中的 exit button 確認 有沒有按下去
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            buttonStart.IsEnabled = false;

            // 強制確保左邊 panel 都是可見的
            TopPanel.Visibility = Visibility.Visible;
            BottomPanel.Visibility = Visibility.Visible;
            MeasureInfoPanel.Visibility = Visibility.Visible;
            // 初始化 _log，所有後續流程都 call _log("文字")
            _log = msg => Dispatcher.Invoke(() => listBoxInfo.Items.Add(msg));

            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;

            // 從 MainWindow 取得已連接的感測器
            SensorBase currentSensor = mainWindow.ConnectedSensor;

            // 檢查感測器是否已連接
            if (currentSensor == null)
            {
                System.Windows.MessageBox.Show("請先在量測頁面連接感測器！");
                buttonStart.IsEnabled = true; // 讓按鈕可以再次點擊
                return; // 中斷流程
            }

            if (mainWindow != null && mainWindow.HasReadFWVersion)   //260413  wu add 為了解決 blackwwindow 第二次開啟會遇到 Get FW 問題
            {
                this.ADBoard_FW.Text = mainWindow.CachedFWVersion;
            }



            //0414
            // inch to cm
            double screenSizeCm = ScreenSizeInInches * 2.54;
            // pixel: related to screen resolution
            double screenWidthPx = targetScreen.Bounds.Width;
            double screenHeightPx = targetScreen.Bounds.Height;
            // Calculate default 8cm on 21 inch screen's actual pixel information
            double whiteBoxPx = ScreenInformation.GetWhiteSquareSizePixels(screenWidthPx, screenHeightPx, screenSizeCm);
            //0414

            double w = this.ActualWidth;
            double h = this.ActualHeight;

            Console.WriteLine($"[INFO] Window size: {w} x {h}");

            var controller = mainWindow?.GetI2CController();
            var protocol = new CalibrationProtocol(controller);
            var calibrationPage = ((MainWindow)Application.Current.MainWindow).Calibration.Content as Calibration;
            calibrationPage.listBoxCalibration.Items.Clear();

            BottomPanel.Visibility = Visibility.Visible;


            if (mainWindow != null && !mainWindow.HasReadFWVersion)
            {
                //Show AD Board FW in blackwindow list
                byte[] GetFirmwareCommandArray =
                           { (byte)Destination.ONYX_MONITOR_CLIENT_ADDR, (byte)Source.ONYX_MONITOR_HOST_ADDR, 0x86,
                    (byte)FactoryMode.ONYX_FACTORY_CMD_C0, (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND, 0x07, (byte)VCCode.      ONYX_FCODE_13_GET_FIRMWARE_VERSION  , 0x00, 0x00 };
                byte retval_FW, retryCount_FW = 0;
                do
                {
                    retval_FW = controller.DDCCI_Send_Command(GetFirmwareCommandArray, mainWindow.getFirmwareVersion);
                    Debug.WriteLine($"ret val = {retval_FW}, retryCount = {++retryCount_FW}");  // Record retry times
                } while (retval_FW != 0 && retryCount_FW <= 10);
                await Task.Delay(300);
            }
            else
            {
                Debug.WriteLine("Skip GetFirmwareVersion command: FW version already cached.");
            }

            try
            {
                switch (_source)
                {
                    case BlackWindowSource.Verification:
                        // 驗證模式，按 Start 後才跑完整驗證流程
                        await RunVerificationWorkflow(currentSensor, token);
                        break;

                    case BlackWindowSource.Calibration:
                        // 校正模式，按 Start 後才跑校正
                        await StartCalibration(token);
                        await RunVerificationWorkflow(currentSensor, token);
                        break;
                    default:
                        Debug.WriteLine("[BlackWindow] 未知來源。");
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                _log("blackwindow exit 已經按下去，作業已被使用者中止");
            }
            finally
            {
                buttonStart.IsEnabled = true;
            }



        }
        /// <summary>
        /// 將 canvasRoot.Children 裡所有「測色／校正過程中可能遺留的 Rectangle」清掉，並等待一次 UI 更新。
        /// </summary>
        public async Task ClearAllCalibrationRectanglesAsync()
        {
            //  blackWindow 就是 this
            // 掃描 canvasRoot 內所有 Rectangle 元素
            var allRects = this.canvasRoot
                               .Children
                               .OfType<Rectangle>()
                               .ToList();

            // 「校正/驗證用」的 Rectangle 都是 Fill = Brushes.White 或 Brushes.Black
            // 其他 UI 如果也有 Rectangle，就要另外判斷或用 Tag 標記才不會誤刪
            foreach (var rect in allRects)
            {
                if (rect.Fill == Brushes.White || rect.Fill == Brushes.Black)
                {
                    this.canvasRoot.Children.Remove(rect);
                }
            }

            // 等一次 WPF 完成 Render，把畫面還原成乾淨的黑底（或其他 UI 狀態）
            await this.Dispatcher.InvokeAsync(
                () => { },
                System.Windows.Threading.DispatcherPriority.Render
            );
        }

        public Rectangle CreateCenteredWhiteBox()
        {
            double cm = ScreenSizeInInches * 2.54;
            double px = ScreenInformation.GetWhiteSquareSizePixels(_targetScreen.Bounds.Width, _targetScreen.Bounds.Height, cm + 5);
            double left = ActualWidth / 2 - px / 2;
            double top = ActualHeight / 2 - px / 2;
            //double left = 0;
            //double top = ActualHeight - px - 20;
            var rect = new Rectangle
            {
                Width = px,
                Height = px,
                Fill = Brushes.White
            };
            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);
            return rect;
        }

        private async Task RunVerificationWorkflow(SensorBase sensor, CancellationToken token)
        {
            Debug.WriteLine("[BlackWindow] 執行驗證流程");
            var calibrationPage = ((MainWindow)Application.Current.MainWindow).Calibration.Content as Calibration;
            var mainWin = (MainWindow)Application.Current.MainWindow;


            var stopwatch_VERIFICATION = System.Diagnostics.Stopwatch.StartNew();

            var protocol = new CalibrationProtocol(mainWin.GetI2CController());
            await protocol.SetBrightnessMax();  //全部流程前要先把螢幕亮度設定為MAX
            await protocol.ResetCTandGamma();


            // 拿 ColorTemp 與 Gamma 支援列表
            await protocol.GetColoetempDetailAndGammaDetail();

            bool doColorTemp = protocol.SupportColorTemp.Count > 0;
            PanelDetailInfo panelInfo = await protocol.GetPanelDetailAsync();
            bool needUniform = (panelInfo != null) && panelInfo.UniformitySupported;
            bool doGamma = protocol.SupportGamma.Any(gm => gm != "DICOM");
            bool doDicom = protocol.SupportGamma.Contains("DICOM");
            Console.WriteLine("doColorTemp: " + doColorTemp + " Reply_Do_Uniformity: " + needUniform + " doGamma: " + doGamma + " doDicom: " + doDicom);

            await MeasureContrastAsync(sensor, calibrationPage, protocol, token);
            if (doColorTemp)
                await MeasureColorTempAsync(sensor, calibrationPage, protocol, token);

            if (needUniform)

                await MeasureUniformityAsync(sensor, calibrationPage, protocol, token);

            if (doGamma)
                await MeasureGammaAsync(sensor, calibrationPage, protocol, token);
            if (doDicom)
                await MeasureDICOMAsync(sensor, calibrationPage, protocol, token);

            stopwatch_VERIFICATION.Stop();

            Debug.WriteLine($"[Total Verification] 總耗時: {stopwatch_VERIFICATION.Elapsed.TotalSeconds:F2} 秒");

            _taskCompleted = true;
            await protocol.ResetAll();//zh251208 add
        }


        private async Task MeasureContrastAsync(SensorBase sensor, Calibration calibrationPage, CalibrationProtocol protocol, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var blackWindow = Application.Current.Windows.OfType<BlackWindow>().FirstOrDefault(w => w.IsVisible);
            blackWindow.textblockStatus.Text = "Verifying Contrast";


            // 先把校正時可能遺留的白/黑方框清乾淨
            await ClearAllCalibrationRectanglesAsync();
            // 先確保我們已經取得支援的色溫明細
            await protocol.GetColoetempDetailAndGammaDetail();

            ////////////////////////////////////////////////////
            //////////////////Measure  Contrast/////////////////
            ////////////////////////////////////////////////////

            contrastBox = CreateCenteredWhiteBox();
            canvasRoot.Children.Add(contrastBox);
            await Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Render);
            await Task.Delay(300);


            // 測白
            bool devIsConnect_1 = sensor.MeasYxyz(out SensorMeasureYxy_t Meas_Yxyz_1);
            if (!devIsConnect_1)
            {
                Console.WriteLine("Please check i1d3 device connection");
                return;
            }
            double measuredWhite_1 = Meas_Yxyz_1.Y;
            Console.WriteLine($"Black Window 中的白色方塊(HIGH) Luminance (Y) = {measuredWhite_1} nits");
            UpdateMeasureInfo(Meas_Yxyz_1);

            string lumYWhite_1 = measuredWhite_1.ToString();
            await Task.Delay(300);

            // 等 WPF 真正把畫面更新成「沒有白方塊的畫面」
            canvasRoot.Children.Remove(contrastBox);
            await Dispatcher.Yield(DispatcherPriority.Render);
            await Task.Delay(1500);

            // 測黑
            bool devIsConnect_2 = sensor.MeasYxyz(out SensorMeasureYxy_t Meas_Yxyz_2);
            double measuredBlack_1 = Meas_Yxyz_2.Y;
            Console.WriteLine($"Black Window 中的黑色方塊(LOW) Luminance (Y) = {measuredBlack_1} nits");
            UpdateMeasureInfo(Meas_Yxyz_2);
            await Task.Delay(300);

            string lumYBlack_1 = measuredBlack_1.ToString();
            double contrast = measuredWhite_1 / measuredBlack_1;
            Console.WriteLine($"Black Window 中量測的對比度 = {contrast}");

            string contrast_result = "";
            if (double.TryParse(calibrationPage.textBoxPanelContrastInfo.Text?.ToString(), out double targetContrast))
            {
                contrast_result = (contrast >= targetContrast) ? "PASS" : "FAIL";
            }
            else
            {
                contrast_result = "PASS"; //預設pass
            }
            calibrationPage.SetContrast(contrast_result, contrast.ToString("F2"));
            //show data in log list
            calibrationPage.AddCalibrationLog("Contrast", contrast.ToString("F2"), contrast_result, "");

            listBoxInfo.Items[0] = $"Contrast: {contrast:F2}";
            await Task.Delay(100);
            SampleReport.Instance.SetContrast_report(measuredWhite_1, measuredBlack_1);
            BottomPanel.Visibility = Visibility.Visible;
        }



        /// <summary>
        /// 測量均勻度（Uniformity）
        /// </summary>
        private async Task MeasureUniformityAsync(SensorBase sensor, Calibration calibrationPage, CalibrationProtocol protocol, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            ////Decide whether measure uniformity or not
            isMeaureRectangle = true;

            Debug.WriteLine("[BlackWindow] 測量 Uniformity");

            // 計算 3x3 均勻度位置
            double cm = ScreenSizeInInches * 2.54;
            double px = ScreenInformation.GetWhiteSquareSizePixels(
                _targetScreen.Bounds.Width, _targetScreen.Bounds.Height, cm);
            double[] xRatio = { 0.25, 0.5, 0.75 };
            double[] yRatio = { 0.25, 0.5, 0.75 };

            //Two lists is for uniformity data
            List<double> whiteLumList = new List<double>();
            List<double> blackLumList = new List<double>();

            List<double> maxxLumList = new List<double>();
            List<double> maxyLumList = new List<double>();
            List<double> maxYLumList = new List<double>();
            List<double> zeroYLumList = new List<double>();
            // 3x3 9 squares
            var squares = new List<Rectangle>();
            if (isMeaureRectangle == true)
            {
                for (int r = 0; r < 3; r++)
                {
                    token.ThrowIfCancellationRequested();

                    for (int c = 0; c < 3; c++)
                    {
                        token.ThrowIfCancellationRequested();

                        Rectangle rect = new Rectangle
                        {
                            Width = px,
                            Height = px,
                            Fill = Brushes.White,
                            Visibility = Visibility.Collapsed
                        };

                        double left = ActualWidth * xRatio[c] - px / 2;
                        double top = ActualHeight * yRatio[r] - px / 2;

                        Canvas.SetLeft(rect, left);
                        Canvas.SetTop(rect, top);

                        canvasRoot.Children.Add(rect);
                        squares.Add(rect);
                    }
                }


                double centerBlackx = 0;
                double centerBlacky = 0;
                double centerBlackY = 0;
                int squareIndex = 0;


                // Display in sequence (each block stops for 1500 ms)
                foreach (var sq in squares)
                {
                    token.ThrowIfCancellationRequested();

                    var blackWindow = Application.Current.Windows.OfType<BlackWindow>().FirstOrDefault(bw => bw.IsVisible);
                    if (blackWindow == null)
                    {
                        System.Windows.MessageBox.Show("BlackWindow 已關閉，無法進行量測");
                        return;
                    }


                    sq.Visibility = Visibility.Visible;
                    System.Windows.Forms.MessageBox.Show("Please put sensor in pattern.!!");

                    // 80% pattern (R=204,G=204,B=204) 
                    sq.Fill = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                    // 強制畫面更新
                    Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                    await Task.Delay(1000);
                    sensor.MeasYxyz(out SensorMeasureYxy_t Meas_Yxyz_white);
                    UpdateMeasureInfo(Meas_Yxyz_white);
                    whiteLumList.Add(Meas_Yxyz_white.Y);
                    await Task.Delay(1000);

                    // 10% pattern (R=25,G=25,B=25) 
                    sq.Fill = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                    Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                    await Task.Delay(1000);
                    sensor.MeasYxyz(out SensorMeasureYxy_t Meas_Yxyz_black);
                    UpdateMeasureInfo(Meas_Yxyz_black);
                    blackLumList.Add(Meas_Yxyz_black.Y);
                    await Task.Delay(1000);

                    //0424 add max for report
                    // 100% pattern (R=255,G=255,B=255)
                    sq.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
                    await Task.Delay(1000);
                    sensor.MeasYxyz(out SensorMeasureYxy_t Meas_Yxyz_max);
                    UpdateMeasureInfo(Meas_Yxyz_max);

                    maxxLumList.Add(Meas_Yxyz_max.x);
                    maxyLumList.Add(Meas_Yxyz_max.y);
                    maxYLumList.Add(Meas_Yxyz_max.Y);
                    await Task.Delay(1000);


                    if (squareIndex == 4)
                    {
                        token.ThrowIfCancellationRequested();

                        sq.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                        await Task.Delay(1000);
                        sensor.MeasYxyz(out SensorMeasureYxy_t Meas_Yxyz_zero);
                        UpdateMeasureInfo(Meas_Yxyz_zero);
                        // center all black
                        centerBlackx = Meas_Yxyz_zero.x;
                        centerBlacky = Meas_Yxyz_zero.y;
                        centerBlackY = Meas_Yxyz_zero.Y;

                        await Task.Delay(1000);
                    }

                    sq.Visibility = Visibility.Collapsed;
                    squareIndex++;
                }


                Debug.WriteLine("[INFO] All squares done.");

                // Calculate uniformity
                double maxWhite = whiteLumList.Max();
                double minWhite = whiteLumList.Min();
                double white_h = 200 * (maxWhite - minWhite) / (maxWhite + minWhite);
                Console.WriteLine($"[White Uniformity] h = {white_h:F2}%");

                double maxBlack = blackLumList.Max();
                double minBlack = blackLumList.Min();
                double black_h = 200 * (maxBlack - minBlack) / (maxBlack + minBlack);
                Console.WriteLine($"[Black Uniformity] h = {black_h:F2}%");

                string whiteResult = white_h > 25 ? "FAIL" : "PASS";
                string blackResult = black_h > 25 ? "FAIL" : "PASS";

                var mainWin = (MainWindow)Application.Current.MainWindow;
                calibrationPage?.AddCalibrationLog("Uniformity 10%", $"{black_h:F2}%", blackResult, "-");
                calibrationPage?.AddCalibrationLog("Uniformity 80%", $"{white_h:F2}%", whiteResult, "-");

                listBoxInfo.Items[1] = $"Uniformity: 10%  {blackResult}  ,80%  {whiteResult}";

                if (whiteResult == "PASS" & blackResult == "PASS")
                {
                    calibrationPage.SetUniformity("PASS", black_h.ToString("F2"));
                }
                else
                {
                    calibrationPage.SetUniformity("FAIL", black_h.ToString("F2"));
                }
                //For verification report 
                SampleReport.Instance.SetUniformity_report(whiteLumList.ToArray(), whiteResult, blackLumList.ToArray(), blackResult, white_h, black_h);

                SampleReport.Instance.SetMax_report(maxxLumList.ToArray(), maxyLumList.ToArray(), maxYLumList.ToArray(), centerBlackx, centerBlacky, centerBlackY);

            }

            ///If isMeaureRectangle == false,need to create the white box in the middle
            Rectangle centerSquare;

            if (isMeaureRectangle)
            {
                centerSquare = squares[4];
            }
            else
            {
                centerSquare = new Rectangle
                {
                    Width = px,
                    Height = px,
                    Fill = Brushes.White,
                    Visibility = Visibility.Visible
                };
                Canvas.SetLeft(centerSquare, ActualWidth / 2 - px / 2);
                Canvas.SetTop(centerSquare, ActualHeight / 2 - px / 2);
                canvasRoot.Children.Add(centerSquare);
            }
            centerSquare.Fill = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            centerSquare.Visibility = Visibility.Visible;
            this.whiteBox = centerSquare;
        }


        /// <summary>
        /// 測量色溫（ColorTemp）
        /// </summary>
        private async Task MeasureColorTempAsync(SensorBase sensor, Calibration calibrationPage, CalibrationProtocol protocol, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var blackWindow = Application.Current.Windows.OfType<BlackWindow>().FirstOrDefault(w => w.IsVisible);
            int measurementCount = 0;

            // 把 "5400K" -> 5400 整數陣列
            var targetTemps = protocol.SupportColorTemp.Select(s => int.Parse(s.TrimEnd('K'))).ToArray();


            var measureDataList = new List<(int target, SensorMeasureYxy_t data, string result)>();
            BottomPanel.Visibility = Visibility.Visible;
            int idx = 0;
            foreach (int cct in targetTemps)
            {
                token.ThrowIfCancellationRequested();

                blackWindow.textblockStatus.Text = "Verifying Only Color Temp " + cct;

                await ClearAllCalibrationRectanglesAsync();

                byte ctByte = protocol.GetColorTempByte(cct);
                await protocol.SetCTonly(ctByte);

                await Task.Delay(100); // 等螢幕更新


                // 建一個白框
                var rect = CreateCenteredWhiteBox();
                canvasRoot.Children.Add(rect);

                // 等框定好
                await Task.Delay(300);

                // 採樣
                bool ok = sensor.MeasYxyz(out var meas);
                if (!ok)
                {
                    System.Windows.Forms.MessageBox.Show("Please check i1D3 device connection");
                    return;
                }

                // 更新 UI
                UpdateMeasureInfo(meas);

                // 計算 CCT
                double CT_n = (meas.x - 0.3320) / (meas.y - 0.1858);
                double CCT = -437 * CT_n * CT_n * CT_n
                             + 3601 * CT_n * CT_n
                             - 6831 * CT_n
                             + 5517;
                string valueStr = CCT.ToString("F2");

                // 判斷 PASS/FAIL
                bool pass = IsColorTempPass(meas.x, meas.y, cct);
                string result = pass ? "PASS" : "FAIL";

                // 更新 Calibration page
                calibrationPage.AddCalibrationLog($"Color Temp {cct}K", valueStr, result, "");


                if (result == "FAIL")
                {
                    switch (measurementCount)
                    {
                        case 0: calibrationPage.labelColorTemp1Judge.Content = "FAIL"; break;
                        case 1: calibrationPage.labelColorTemp2Judge.Content = "FAIL"; break;
                        case 2: calibrationPage.labelColorTemp3Judge.Content = "FAIL"; break;
                    }
                }
                else
                {
                    switch (measurementCount)
                    {
                        case 0: calibrationPage.labelColorTemp1Judge.Content = "PASS"; break;
                        case 1: calibrationPage.labelColorTemp2Judge.Content = "PASS"; break;
                        case 2: calibrationPage.labelColorTemp3Judge.Content = "PASS"; break;
                    }
                }

                string targetColorTempString = cct.ToString();

                if (calibrationPage != null)
                {
                    switch (measurementCount)
                    {
                        case 0:
                            calibrationPage.SetColorTemp(measurementCount, targetColorTempString, valueStr);
                            break;
                        case 1:
                            calibrationPage.SetColorTemp(measurementCount, targetColorTempString, valueStr);
                            break;
                        case 2:
                            calibrationPage.SetColorTemp(measurementCount, targetColorTempString, valueStr);
                            break;
                    }
                }
                measurementCount++;

                if (listBoxInfo.Items.Count > 2 + idx)
                    listBoxInfo.Items[2 + idx] = $"Color Temp {targetTemps[idx]}K: {valueStr}";
                await Task.Delay(10);
                // 更新主頁面標記
                switch (idx)
                {
                    case 0: calibrationPage.labelColorTemp1Judge.Content = result; break;
                    case 1: calibrationPage.labelColorTemp2Judge.Content = result; break;
                    case 2: calibrationPage.labelColorTemp3Judge.Content = result; break;
                }

                // 紀錄報告
                SampleReport.Instance.SetColorTemp_report(cct, meas.x, meas.y, meas.Y, result, valueStr);
                idx++;
                measureDataList.Add((cct, meas, result));
            }
        }


        public bool IsColorTempPass(double x, double y, int targetColorTemp)
        {
            double std_x = 0, std_y = 0;
            bool inRange = ColorTemperatureCalculator.CT2xy(targetColorTemp, ref std_x, ref std_y);

            Console.WriteLine($"[ColorTemp] {targetColorTemp}K 對應標準 x/y = ({std_x:F4}, {std_y:F4})");
            Console.WriteLine($"[Measured] 實際量測 x/y = ({x:F4}, {y:F4})");

            bool isPass = false;

            if (inRange)
            {
                double dx = Math.Abs(x - std_x);
                double dy = Math.Abs(y - std_y);
                // isPass = (dx <= 0.03 || dy <= 0.03);
                isPass = (dx <= 0.03 && dy <= 0.03);  //250704 wu modify to &&
                Console.WriteLine($"[Result] Δx = {dx:F4}, Δy = {dy:F4} → {(isPass ? "PASS" : "FAIL")}");
            }
            else
            {
                Console.WriteLine($"[Error] {targetColorTemp}K 超出可支援範圍（4000K ~ 25000K）");
            }

            return isPass;
        }

        /// <summary>
        /// 測量 Gamma
        /// </summary>
        private async Task MeasureGammaAsync(SensorBase sensor, Calibration calibrationPage, CalibrationProtocol protocol, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var blackWindow = Application.Current.Windows.OfType<BlackWindow>().FirstOrDefault(w => w.IsVisible);
            blackWindow.textblockStatus.Text = "Verifying Gamma";

            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            var controller = mainWindow?.GetI2CController();

            //  展開所有的 Gamma × ColorTemp 組合
            var gammaCtpairs = protocol.SupportGamma.Where(gm => gm != "DICOM").SelectMany(gm => protocol.SupportColorTemp.Select(ct => new { Gamma = gm, CT = ct })).ToList();
            GammaCount = 0;

            if (gammaCtpairs.Count == 0)
            {
                System.Windows.MessageBox.Show("沒有可用的 Gamma × CCT 組合進行驗證");
                return;
            }

            foreach (var pair in gammaCtpairs)
            {
                // 解析出 gamma / ct
                string gammaStr = pair.Gamma;
                string ctStr = pair.CT;

                if (!double.TryParse(gammaStr, out double targetGamma))
                    targetGamma = 2.2;

                int targetCT = int.Parse(ctStr.TrimEnd('K')); // e.g. "6500K" -> 6500
                byte ctByte = protocol.GetColorTempByte(targetCT);
                byte gmByte = protocol.GetGammaByte(targetGamma);
                Console.WriteLine("ctbyte: " + ctByte + "  gmbyte: " + gmByte);

                await protocol.SetGammaOnly(gmByte);
                await protocol.SetCTonly(ctByte);
                await Task.Delay(100);


                int sample_num = 32; //  Gamma: 32 level 
                double ambientLight = 0; // Default: 0

                List<gammaMeasureData> measureDataList = new List<gammaMeasureData>();
                List<gammaStandardData> standardDataList = new List<gammaStandardData>();

                SensorMeasureYxy_t Meas_Yxyz;
                bool devIsConnect;


                whiteBox = CreateCenteredWhiteBox();
                canvasRoot.Children.Add(whiteBox);

                // Level 0 -black
                whiteBox.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                await Task.Delay(1500);
                devIsConnect = sensor.MeasYxyz(out Meas_Yxyz);
                if (!devIsConnect)
                {
                    System.Windows.Forms.MessageBox.Show("請確認 i1D3 裝置連線");
                    return;
                }

                double lum_low = Meas_Yxyz.Y;
                double lum_ambient = ambientLight;
                gammaMeasureData measureData_low = new gammaMeasureData
                {
                    gray_level = 0,
                    x = Math.Round(Meas_Yxyz.x, 4),
                    y = Math.Round(Meas_Yxyz.y, 4),
                    Y = Math.Round(Meas_Yxyz.Y, 4) + lum_ambient
                };


                UpdateMeasureInfo(Meas_Yxyz);/// for update measure info
                blackWindow.textblockStatus.Text = $"Verifying Gamma {targetGamma:F1} @ {targetCT}K";

                measureDataList.Add(measureData_low);
                string targetGammaString = targetGamma.ToString("F1");
                calibrationPage?.AddCalibrationLog($"\nVerifying Gamma : ", "\n" + targetGammaString, "\n" + targetCT.ToString(), "");
                calibrationPage?.AddCalibrationLog($"   Level", "      x", "     y", "     Y");
                calibrationPage?.AddCalibrationLog("Level 0", measureData_low.x.ToString("F4"), measureData_low.y.ToString("F4"), measureData_low.Y.ToString("F4"));

                //32 step
                for (int level = 1; level < sample_num; level++)
                {
                    token.ThrowIfCancellationRequested();

                    int gray_level = 255 * level / (sample_num - 1);
                    byte levelByte = Convert.ToByte(gray_level);
                    whiteBox.Fill = new SolidColorBrush(Color.FromRgb(levelByte, levelByte, levelByte));
                    await Task.Delay(500);
                    devIsConnect = sensor.MeasYxyz(out Meas_Yxyz);
                    if (!devIsConnect)
                    {
                        System.Windows.Forms.MessageBox.Show("裝置中斷，請重新檢查");
                        return;
                    }
                    UpdateMeasureInfo(Meas_Yxyz); // for update measure info
                    gammaMeasureData measureData = new gammaMeasureData
                    {
                        gray_level = gray_level,
                        x = Math.Round(Meas_Yxyz.x, 4),
                        y = Math.Round(Meas_Yxyz.y, 4),
                        Y = Math.Round(Meas_Yxyz.Y, 4) + lum_ambient
                    };
                    measureDataList.Add(measureData);
                    Console.WriteLine($"{level}\t{Meas_Yxyz.x:F4}\t{Meas_Yxyz.y:F4}\t{Meas_Yxyz.Y:F4}");
                    calibrationPage?.AddCalibrationLog($"Level {gray_level}", measureData.x.ToString("F4"), measureData.y.ToString("F4"), measureData.Y.ToString("F4"));
                }
                canvasRoot.Children.Remove(whiteBox);

                var rect = CreateCenteredWhiteBox();
                canvasRoot.Children.Add(rect);

                // 等框定好
                await Task.Delay(100);

                bool ok = sensor.MeasYxyz(out var meas);
                if (!ok)
                {
                    System.Windows.Forms.MessageBox.Show("Please check i1D3 device connection");
                    return;
                }

                // 更新 UI
                UpdateMeasureInfo(meas);

                // 計算 CCT
                double CT_n = (meas.x - 0.3320) / (meas.y - 0.1858);
                double CCT = -437 * CT_n * CT_n * CT_n
                             + 3601 * CT_n * CT_n
                             - 6831 * CT_n
                             + 5517;
                string CCTStr = CCT.ToString("F2");


                Console.WriteLine("CCTSTR:" + CCTStr);


                // Calculate gamma
                double[] measdataArr_Y = measureDataList.Select(d => d.Y).ToArray();
                double[] measdataArr_P = measureDataList.Select(d => (double)d.gray_level).ToArray();
                double gamma_result = GammaStandard.getGamma(measdataArr_Y);
                calibrationPage?.AddCalibrationLog($"Gamma " + targetGammaString + " Result: ", gamma_result.ToString("F4"), "", "");

                Console.WriteLine("Gamma" + targetGammaString + " Result: " + gamma_result);

                string gamma_judge = (Math.Abs(gamma_result - targetGamma) > 0.09) ? "FAIL" : "PASS";

                if (targetCT == 6500)
                {
                    switch (gamma6500_Index)
                    {
                        case 1:
                            calibrationPage.labelGamma1.Content = $"Gamma {gammaStr}";
                            calibrationPage.textBoxGamma1.Text = gamma_result.ToString("F2");
                            calibrationPage.labelGamma1Judge.Content = gamma_judge;
                            break;
                        case 2:
                            calibrationPage.labelGamma2.Content = $"Gamma {gammaStr}";
                            calibrationPage.textBoxGamma2.Text = gamma_result.ToString("F2");
                            calibrationPage.labelGamma2Judge.Content = gamma_judge;
                            break;
                        //zh251107 add>
                        case 3:
                            calibrationPage.labelGamma3.Content = $"Gamma {gammaStr}";
                            calibrationPage.textBoxGamma3.Text = gamma_result.ToString("F2");
                            calibrationPage.labelGamma3Judge.Content = gamma_judge;
                            break;
                        case 4:
                            calibrationPage.labelGamma4.Content = $"Gamma {gammaStr}";
                            calibrationPage.textBoxGamma4.Text = gamma_result.ToString("F2");
                            calibrationPage.labelGamma4Judge.Content = gamma_judge;
                            break;
                            //zh251107 add<
                    }

                    gamma6500_Index++;
                }
                SampleReport.Instance.SetGamma_report(targetCT, targetGamma, gamma_result, gamma_judge);


                // === 更新 Gamma 顯示區塊 ===
                // 先取得 Color Temp 數量（從 listBoxInfo 第 2 行開始連續的）
                int colorTempCount = protocol.SupportColorTemp.Count; // 跟 ColorTemp 測量邏輯一致

                // listBoxIndex: 第幾組 Gamma × CT
                int listBoxIndex = 2 + protocol.SupportColorTemp.Count + GammaCount;

                if (listBoxInfo.Items.Count > listBoxIndex)
                {
                    listBoxInfo.Items[listBoxIndex] = $"Gamma {targetGamma:F1} {targetCT}K: {gamma_result:F2} {CCTStr}";
                }

                GammaCount++;

            }
            BottomPanel.Visibility = Visibility.Visible;
        }


        /// <summary>
        /// 測量 DICOM
        /// </summary>
        private async Task MeasureDICOMAsync(SensorBase sensor, Calibration calibrationPage, CalibrationProtocol protocol, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var blackWindow = Application.Current.Windows.OfType<BlackWindow>().FirstOrDefault(w => w.IsVisible);
            var supportCTs = protocol.SupportColorTemp;
            blackWindow.textblockStatus.Text = $"Verifying DICOM";
            var mainWin = (MainWindow)Application.Current.MainWindow;

            DICOMCount = 0;

            foreach (var ctStr in supportCTs)
            {
                token.ThrowIfCancellationRequested();

                if (!int.TryParse(ctStr.TrimEnd('K'), out int targetCT))
                    continue;


                byte ctByte = protocol.GetColorTempByte(targetCT);
                byte gmByte = 0x20; // DICOM 對應的 gamma byte

                await protocol.SetGammaOnly(gmByte);
                await protocol.SetCTonly(ctByte);
                await Task.Delay(100);

                blackWindow.textblockStatus.Text = $"Verifying DICOM {targetCT}";
                int sample_num = 16;  //  DICOM: 16 level 
                double ambientLight = 0;


                List<gammaMeasureData> measureDataList = new List<gammaMeasureData>();



                whiteBox = CreateCenteredWhiteBox();
                canvasRoot.Children.Add(whiteBox);


                await Task.Delay(1000);

                SensorMeasureYxy_t Meas_Yxyz;
                bool devIsConnect;

                // Level 0 - black
                whiteBox.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                await Task.Delay(1000);

                // 再次檢查
                token.ThrowIfCancellationRequested();


                devIsConnect = sensor.MeasYxyz(out Meas_Yxyz);
                if (!devIsConnect)
                {
                    System.Windows.Forms.MessageBox.Show("請確認 i1D3 裝置連線");
                    return;
                }

                double lum_low = Meas_Yxyz.Y + ambientLight;

                gammaMeasureData measureData_low = new gammaMeasureData
                {
                    gray_level = 0,
                    x = Math.Round(Meas_Yxyz.x, 4),
                    y = Math.Round(Meas_Yxyz.y, 4),
                    Y = Math.Round(Meas_Yxyz.Y, 4) + ambientLight
                };
                UpdateMeasureInfo(Meas_Yxyz); // for update measure info

                measureDataList.Add(measureData_low);
                int targetDICOMColorTemp = 6500;
                string targetDICOMColorTempString = targetDICOMColorTemp.ToString();
                calibrationPage?.AddCalibrationLog($"\nVerifying DICOM :  ", "\n" + targetCT, "", "");
                calibrationPage?.AddCalibrationLog($"   Level", "      x", "     y", "     Y");
                calibrationPage?.AddCalibrationLog(
           "Level 0:",
           measureData_low.x.ToString("F4"),
           measureData_low.y.ToString("F4"),
           measureData_low.Y.ToString("F2")

        );

                //18 level
                for (int level = 1; level < sample_num + 2; level++)
                {
                    token.ThrowIfCancellationRequested();

                    int gray_level = (256 / sample_num - 1) * level;
                    byte levelByte = Convert.ToByte(gray_level);
                    whiteBox.Fill = new SolidColorBrush(Color.FromRgb(levelByte, levelByte, levelByte));
                    await Task.Delay(500);
                    devIsConnect = sensor.MeasYxyz(out Meas_Yxyz);
                    if (!devIsConnect)
                    {
                        System.Windows.Forms.MessageBox.Show("裝置中斷，請重新檢查");
                        return;
                    }
                    UpdateMeasureInfo(Meas_Yxyz); // for update measure info
                    gammaMeasureData measureData = new gammaMeasureData
                    {
                        gray_level = gray_level,
                        x = Math.Round(Meas_Yxyz.x, 4),
                        y = Math.Round(Meas_Yxyz.y, 4),
                        Y = Math.Round(Meas_Yxyz.Y, 4) + ambientLight
                    };
                    measureDataList.Add(measureData);
                    Console.WriteLine($"{level}\t{Meas_Yxyz.x:F4}\t{Meas_Yxyz.y:F4}\t{Meas_Yxyz.Y:F4}");

                    calibrationPage?.AddCalibrationLog($"Level {15 * (level)}:", measureData.x.ToString("F4"), measureData.y.ToString("F4"), measureData.Y.ToString("F4"));
                }

                canvasRoot.Children.Remove(whiteBox);

                // 計算 CCT
                double CT_n = (Meas_Yxyz.x - 0.3320) / (Meas_Yxyz.y - 0.1858);
                double CCT = -437 * CT_n * CT_n * CT_n
                             + 3601 * CT_n * CT_n
                             - 6831 * CT_n
                             + 5517;
                string CCTStr = CCT.ToString("F2");


                Console.WriteLine("CCTSTR:" + CCTStr);


                // 轉換為 JND
                double[] DICOMMeasureArr_Y = measureDataList.Select(m => m.Y).ToArray();
                double[] DICOMMeasureArr_JND = DICOMMeasureArr_Y
                    .Select(y => JNDCalculator.LuminanceToJND(y)).ToArray();

                // 建立標準 JND 資料
                double[] DICOMStandardArr_Y = GammaStandard.DICOMStandard(
                    lum_low,
                    measureDataList.Last().Y,
                    18
                );

                double[] deviations = LumDeviation.Deviation(DICOMMeasureArr_Y, DICOMStandardArr_Y, 18);

                string dicomResult = "PASS";
                calibrationPage?.AddCalibrationLog($"Level 0:", "null", "", "");


                double sumDeviation = 0;
                int count = deviations.Length;
                double deviation_max = 0;
                for (int i = 0; i < deviations.Length; i++)
                {
                    double deviation = deviations[i];
                    sumDeviation += deviation;

                    if (Math.Abs(deviation) > deviation_max)
                        deviation_max = Math.Abs(deviation); // 取絕對值再更新

                    string dev_result = Math.Abs(deviations[i]) < 15 ? "PASS" : "FAIL";//zh251203 modify

                    calibrationPage?.AddCalibrationLog(

                        $"Level {15 * (i + 1)}: ",
                        $"{deviations[i]:F2}%",
                        dev_result,
                        "-"
                    );

                    if (dev_result == "FAIL") dicomResult = "FAIL";
                }

                Console.WriteLine($"[DICOM] 最大偏差 = {deviation_max:F2}%");

                calibrationPage?.AddCalibrationLog($"DICOM Result : ", dicomResult, "", "");

                int colorTempCount = protocol.SupportColorTemp.Count;
                int gammaCount = protocol.SupportGamma.Count(g => g != "DICOM") * colorTempCount;  // 只算普通 Gamma，不算 DICOM 本身
                int baseIndex = 2 + colorTempCount + gammaCount;

                int listBoxIndex = baseIndex + DICOMCount;

                if (listBoxInfo.Items.Count > listBoxIndex)
                {
                    // listBoxInfo.Items[listBoxIndex] = $"DICOM {targetCT}:  {dicomResult}  {CCTStr}";
                    UpdateListBoxInfoResult(listBoxIndex, prefix: $"DICOM {targetCT}:  ", result: dicomResult, suffix: $"  {CCTStr}");
                }
                listBoxIndex++;


                DICOMCount++;

                //  Only update for DICOM 6500K
                if (targetCT == 6500)
                {
                    calibrationPage?.SetDICOM("6500", dicomResult, deviation_max.ToString("F2"));
                    SampleReport.Instance.SetDICOM_report(6500, deviation_max, dicomResult);
                }

                //calibrationPage?.SetDICOM(targetDICOMColorTemp.ToString(), dicomResult, deviation_max.ToString("F2"));
                //SampleReport.Instance.SetDICOM_report(targetDICOMColorTemp, deviation_max, dicomResult);

                BottomPanel.Visibility = Visibility.Visible;

            }
        }

        private void UpdateListBoxInfoResult(int index, string prefix, string result, string suffix)
        {
            //Update blackwindow list: fail:red pass:green
            var tb = new TextBlock();

            tb.Inlines.Add(new Run(prefix));

            var runResult = new Run(result);
            if (string.Equals(result, "FAIL", StringComparison.OrdinalIgnoreCase))
                runResult.Foreground = Brushes.Red;
            else if (string.Equals(result, "PASS", StringComparison.OrdinalIgnoreCase))
                runResult.Foreground = Brushes.Green;
            tb.Inlines.Add(runResult);

            tb.Inlines.Add(new Run(suffix));

            listBoxInfo.Items[index] = tb;
        }



        private void OnColorTempFinished()
        {
            // Remove White box
            if (whiteBox != null && canvasRoot.Children.Contains(whiteBox))
            {
                canvasRoot.Children.Remove(whiteBox);
                whiteBox = null;
            }
        }
        private void buttonExit_Click(object sender, RoutedEventArgs e)
        {
            buttonStart.IsEnabled = false;

            _cts?.Cancel();

            this.Close();
        }
        private void Window_PreviewMouseDown(object sender, RoutedEventArgs e)
        {
            if (_taskCompleted)
            {
                _taskCompleted = false; // Reset task flag
                System.Windows.MessageBox.Show("VERIFY IS DONE!!!");
            }
        }
    }

}
