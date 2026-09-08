using FDTI_Factory_i2c;
using MonitorFactoryTool;
using MonitorFactoryTool.ONYX.Utilities;
using MonitorFactoryTool.Pages;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using ONYX.Utilities;
using ONYX_DataType;
using OnyxSensor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using static MonitorFactoryTool.ONYX.Utilities.CalibrationProtocol;
using static MonitorFactoryTool.Pages.Gamma;
using MReplyP = ONYX_DataType.MonitorReplyPackage;


namespace MonitorFactoryTool
{
    public static class GlobalCalibData
    {
        public static int[] Native_R = new int[32];//ian add 0617 存從NativeRGB.txt讀到的數值。
        public static int[] Native_G = new int[32];//ian add 0617 存從NativeRGB.txt讀到的數值。
        public static int[] Native_B = new int[32];//ian add 0617 存從NativeRGB.txt讀到的數值。
    }
    public class CalibProcess
    {
        /// <summary>
        /// Events Handler -- Console Write Line;
        /// </summary>
        /// <param name="Msg"></param>
        public delegate void EventsHandler(object sender);
        public event EventsHandler Event_ConsoleWriteLine;
        public Action<SensorMeasureYxy_t> UpdateUIAction;
        public static int[] vector_5400 = new int[3]; // [0]=R, [1]=G, [2]=B
        public static int[] vector_6500 = new int[3];
        public static int[] vector_9300 = new int[3];
        
        private static int[] backup_5400_Y_ints = new int[32];//ian add 0617 存校正的 Y值讓之後Erase後可以讀回來。
        private static int[] backup_6500_Y_ints = new int[32];//ian add 0617 存校正的 Y值讓之後Erase後可以讀回來。
        private static int[] backup_9300_Y_ints = new int[32];//ian add 0617 存校正的 Y值讓之後Erase後可以讀回來。
        private static bool _isI1DataFlushed = false; // 紀錄是否已經讀取過 i1d3 的資料並存到 backup_XXX_Y_ints //ian add 0617
        private static bool _isNativeRgbLoaded = false; // 紀錄是否已經讀取過 Native RGB //ian add 0617
        private I2CController i2cController;
        static byte AppStatus = 0;

        //private GammaColorTrackingCalib calib = new GammaColorTrackingCalib();
        //>>wu add 250707
        private GammaColorTrackingCalib calib;
        private readonly GammaColorTrackingCalib _gammaCalib;
        //<<wu add 250707

        private int mTarget = 6500;//5400//9300
        private byte mIndex = 0;

        public bool IsRGBWPanel => true;//zh 250619//false; // 暫時寫死，未來可根據機型自動判斷


        private PanelColorTempData panelColorTempData = new PanelColorTempData(); // ✅ 為全域欄位，儲存 RGB 曲線
        public ColorVector ColorVectorMeasured { get; private set; } //  測得的 RGB 向量比例
        private double[] xDataSample;
        private double[] rLumSample;
        private double[] gLumSample;
        private double[] bLumSample;
        private double[] wLumSample;//zh 250619
        private double[] wdLumSample;//zh 250619

        public bool isDoingColorTracking = true; //0528 wu add for 方便分功能測試新增flag 
        public bool isDoingColorTemp = true;
        public bool isDoingDICOMCalib = true;
        
        public bool enableFlashWrite = false;//設為 false 就會跳過所有 Flash 的 Erase 與 Write 動作
        public event Action RGBGainCalibrationCompleted; //0528 wu add 做完純色溫校正(含送完指令)後跳到verify 流程


        private class CalibTarget
        {
            /////設定要量測的色溫和gamma 
            public int CCT { get; set; }
            public double Gamma { get; set; }

            public CalibTarget(double gamma, int cct)
            {
                CCT = cct;
                Gamma = gamma;
            }

            public override string ToString()
            {
                // 如果 Gamma 是 NaN，代表 DICOM
                if (double.IsNaN(Gamma))
                    return $"DICOM @ {CCT}K";
                else
                    return $"{CCT}K @ Gamma {Gamma:F2}";
            }
        }


        public struct PatternRGB
        {
            public int R;
            public int G;
            public int B;

            public PatternRGB(int r, int g, int b)
            {
                R = r;
                G = g;
                B = b;
            }

            public override string ToString() => $"({R}, {G}, {B})";
        }

        private class BaseData // 250701 wu add 把gamma 每一次的量測分開來
        {
            public double[] MeasY;           // 32 點灰階亮度
            public ColorVector Cv;           // 量測到的 RGB 向量
        }
        // <色溫, 資料> // 250701 wu add 把gamma 每一次的量測分開來
        private readonly Dictionary<int, BaseData> _baseCache = new Dictionary<int, BaseData>();
        // <純色溫的RGB> // 250702 wu add 紀錄純色溫校正後的的RGB
        private const string _rgbLogFile = "ColorTemp_RGB_Log.txt";

        private List<CalibTarget> targetList = new List<CalibTarget>();

        private readonly SensorBase _sensor;
        private readonly I2CController _i2c;

        //>>wu add 250707
        private readonly PanelDetailInfo _panelInfo;
        public CalibProcess(SensorBase sensor, PanelDetailInfo panelInfo)
        {
            _sensor = sensor ?? throw new ArgumentNullException(nameof(sensor));
            _panelInfo = panelInfo ?? throw new ArgumentNullException(nameof(panelInfo));
            _gammaCalib = new GammaColorTrackingCalib(
                          _panelInfo.EntryCount,
                          _panelInfo.LutResolution);
            calib = _gammaCalib;  //為了不大量改名字
        }
        //<<wu add 250707

        public async Task doWork(CancellationToken token)
        {
            double totalSeconds_CT = 0;
            double totalSeconds_GAMMA = 0;
            double totalSeconds_DICOM = 0;
            double totalSeconds_ALL = 0;
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            var mainWin = (MainWindow)Application.Current.MainWindow;
            var calibrationPage = mainWin.Calibration.Content as Calibration;
            var blackWindow = Application.Current.Windows.OfType<BlackWindow>().FirstOrDefault(w => w.IsVisible);

            var stopwatch_ALL = System.Diagnostics.Stopwatch.StartNew(); // 用於計算全部總時間

            var controller = mainWindow?.GetI2CController();
            if (controller == null)
            {
                Console.WriteLine("錯誤：無法取得 i2cController");
                return;
            }
            
            blackWindow.buttonStart.IsEnabled = false;

            var protocol = new CalibrationProtocol(controller);
            token.ThrowIfCancellationRequested();
            await blackWindow.ClearAllCalibrationRectanglesAsync();
            await protocol.ResetCTandGamma(); // ct for user and gamma off 
            await protocol.GetColoetempDetailAndGammaDetail();

            token.ThrowIfCancellationRequested();
            // 1) 在进入 foreach(target) 之前，先把 protocol 的兩對列表轉成字典
            var ctMap = protocol.SupportColorTemp
                .Zip(protocol.SupportColorTempCodes, (text, code) => (text, code))
                .ToDictionary(x => x.text, x => x.code);

            var gammaMap = protocol.SupportGamma
                .Zip(protocol.SupportGammaCodes, (text, code) => (text, code))
                .ToDictionary(x => x.text, x => x.code);

            // 更新支援的target list 決定要做的項目
            await PopulateTargetList(protocol, token);




            token.ThrowIfCancellationRequested();

            ResetRgbLog();  ///  如果已經有舊的txt 就刪掉舊的RGB txt 
            ResetCalibrationItems();  //清掉calibration右上角的item 項目


            // 從 targetList 中選出不重複的色溫值
            var pureCCTs = protocol.SupportColorTemp.Select(text => int.Parse(text)).Distinct();
            Console.WriteLine("--- 開始執行純色溫校正 ---");

            foreach (var cct in pureCCTs)
            {
                token.ThrowIfCancellationRequested();
                Console.WriteLine($"============================== 準備校正純色溫 {cct}K ==============================");

                var stopwatch_CT = System.Diagnostics.Stopwatch.StartNew();
                mTarget = cct;
                await calibrate(cct, token);
                await protocol.ResetCTandRGB128();
                await Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Render);

                stopwatch_CT.Stop();
                double seconds_CT = stopwatch_CT.Elapsed.TotalSeconds;
                totalSeconds_CT += seconds_CT;
                Console.WriteLine($"完成純色溫校正 {cct}K，花費時間：{seconds_CT:F2} 秒");
                Console.WriteLine("");
            }


            if (protocol.SupportGamma.Any())
            {
                token.ThrowIfCancellationRequested();
                Console.WriteLine("--- 準備量測面板原生曲線 (Gamma off, CT=user) ---");
                await protocol.ResetCTandRGB128();
                await protocol.SetBrightnessMax();//zh 250715
                await protocol.GammaErase();//zh 260130 add
                
                var stopwatch_measure_native = System.Diagnostics.Stopwatch.StartNew();
                await measure(token);
                stopwatch_measure_native.Stop();

                Console.WriteLine($"[Measure Native] 耗時：{stopwatch_measure_native.Elapsed.TotalSeconds:F2} 秒"); 
            }
            else
            {
                Console.WriteLine("沒有 Gamma/DICOM ，跳過原生面板量測\n");
            }



            foreach (var target in targetList)
            {
                mTarget = target.CCT;
                token.ThrowIfCancellationRequested();

                Console.WriteLine($"============================== 開始校正 {target} ==============================");//zh modify 250611
               
                // 準備 key
                string ctKey = target.CCT.ToString();       // 例如 "6500"
                string gmKey = double.IsNaN(target.Gamma) ? "DICOM" : target.Gamma.ToString("F1");

                // 從字典拿 code
                if (!ctMap.TryGetValue(ctKey, out byte ctCode))
                {
                    Console.WriteLine($"Error: 無法找到 CCT={ctKey} 對應的 code");
                    continue;
                }
                if (!gammaMap.TryGetValue(gmKey, out byte gmCode))
                {
                    Console.WriteLine($"Error: 無法找到 Gamma={gmKey} 對應的 code");
                    continue;
                }


               
                token.ThrowIfCancellationRequested();
                ////////////////////////Gamma 搭配色溫校正(Color Tracking)////////////////////////////
                if (!double.IsNaN(target.Gamma))
                {

                    var stopwatch_GAMMA = System.Diagnostics.Stopwatch.StartNew();
                    await CalibrateAllGammaAsync(
                     new List<CalibTarget> { target },   // 只把「目前的gamma」丟進去
                     token);

                    //  取出準備燒錄的LUT，校正一個就燒一個
                    var lut = calib.GetLUTData(target.CCT, target.Gamma);
                    var burner = new BurnLUTToBoard(controller);

                    Console.WriteLine($"開始燒錄 {target.CCT}K @ γ={target.Gamma:F2} " + $"(CT=0x{ctCode:X2}, GM=0x{gmCode:X2})");

                    await burner.BurnOne(gmCode, ctCode, lut, target.Gamma, target.CCT);

                    stopwatch_GAMMA.Stop();
                    double seconds_GAMMA = stopwatch_GAMMA.Elapsed.TotalSeconds;
                    totalSeconds_GAMMA += seconds_GAMMA;
                    Console.WriteLine($"完成GAMMA {target.CCT} 校正，花費時間：{seconds_GAMMA:F2} 秒");
                    Console.WriteLine("");

                    await Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Render); //為了解決之後verity 量測contrast 白色方框沒有不見的問題 加上這行去更新畫面
                }
                else
                {
                    token.ThrowIfCancellationRequested();
                    ///////////////////////DICOM 搭配色溫校正////////////////////////////

                    var stopwatch_DICOM = System.Diagnostics.Stopwatch.StartNew();
                    double jndScale = 1.0;

                    await dicomCalib(jndScale, target.CCT, token);

                    //await CalibrateAllDICOMAsync( new List<CalibTarget> { target }, jndScale,token);// 只把「目前的gamma」丟進去

                    //  取出準備燒錄的LUT，校正一個燒一個

                    var dicomLut = calib.GetDICOMLUTData(target.CCT, jndScale);
                    var burner = new BurnLUTToBoard(controller);

                    Console.WriteLine($"開始燒錄 DICOM {target.CCT}K @ JND={jndScale:F2} " + $"(CT=0x{ctCode:X2}, GM=0x{gmCode:X2})");
                    Console.WriteLine("DICOM 流程的gmCode  ctCode" + gmCode, ctCode);

                    await burner.BurnOne(gmCode, ctCode, dicomLut, target.Gamma, target.CCT);

                    stopwatch_DICOM.Stop();
                    double seconds_DICOM = stopwatch_DICOM.Elapsed.TotalSeconds;
                    totalSeconds_DICOM += seconds_DICOM;

                    Console.WriteLine($"完成 DICOM {target.CCT}K 校正與燒錄，花費時間：{seconds_DICOM:F2} 秒");
                    Console.WriteLine("");
                    await Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Render); //Update UI

                    token.ThrowIfCancellationRequested();
                    blackWindow.buttonStart.IsEnabled = true;

                    await Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Render);
                }
            }
            stopwatch_ALL.Stop(); // 停止計算全部總時間

            totalSeconds_ALL = stopwatch_ALL.Elapsed.TotalSeconds;

            Console.WriteLine("==========================================================");
            Console.WriteLine("                      所有校正完成");
            Console.WriteLine("==========================================================");
            if (totalSeconds_CT > 0)
            {
                Console.WriteLine($"純色溫校正總耗時：{totalSeconds_CT:F2} 秒");
            }
            if (totalSeconds_GAMMA > 0)
            {
                Console.WriteLine($"Gamma 校正與燒錄總耗時：{totalSeconds_GAMMA:F2} 秒");
            }
            if (totalSeconds_DICOM > 0)
            {
                Console.WriteLine($"DICOM 校正與燒錄總耗時：{totalSeconds_DICOM:F2} 秒");
            }
            Console.WriteLine($"--- 全部流程總耗時：{totalSeconds_ALL:F2} 秒 ---");
            Console.WriteLine("==========================================================");
        }



        public async Task calibrate(int cct_parameter, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            int temperatureToUse = this.mTarget;
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;

            var blackWindow = Application.Current.Windows.OfType<BlackWindow>().FirstOrDefault(w => w.IsVisible);
            if (blackWindow == null)
            {
                MessageBox.Show("找不到 BlackWindow，無法進行 Gamma 校正");
                return;
            }

            blackWindow.textblockStatus.Text = "Calibration Color Temp " + temperatureToUse;

            //先在畫面中央顯示一個白色方框
            Rectangle white = blackWindow.CreateCenteredWhiteBox();
            blackWindow.canvasRoot.Children.Add(white);
            blackWindow.whiteBox = white;

            //  讓 WPF 先把「白方框」真正畫到畫面上
            await blackWindow.Dispatcher.InvokeAsync(
                () => { },
                System.Windows.Threading.DispatcherPriority.Render
            );

            await Task.Delay(100);

            ColorData color = new ColorData(mTarget, 1);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            });

            var controller = mainWindow?.GetI2CController();
            if (controller == null)
            {
                Console.WriteLine("錯誤：無法取得 i2cController");
                return;
            }

            var protocol = new CalibrationProtocol(controller);  // 取得 controller 並傳給 protocol
            await Task.Delay(100);

            byte rGain, gGain, bGain;
            GetInitialRGB(temperatureToUse, out rGain, out gGain, out bGain);

            Console.WriteLine("color " + temperatureToUse + "k : " + color.x.ToString(), color.y.ToString(), color.Y.ToString(), color.z.ToString());
            protocol.SetRedGain(rGain);
            protocol.SetGreenGain(gGain);
            protocol.SetBlueGain(bGain);

            token.ThrowIfCancellationRequested();

            SensorMeasureYxy_t Meas_Yxyz_1 = new SensorMeasureYxy_t();
            bool devIsConnect = _sensor.MeasYxyz(out Meas_Yxyz_1);
            if (!devIsConnect)
            {
                Console.WriteLine("Please check i1d3 device connection");
                return;
            }
            double CT_n = (Meas_Yxyz_1.x - 0.3320) / ((float)Meas_Yxyz_1.y - 0.1858);
            double CCT = -437 * CT_n * CT_n * CT_n + 3601 * CT_n * CT_n - 6831 * CT_n + 5517;
            Console.WriteLine($"CCT=" + CCT);
            Console.WriteLine($"[CalibProcess] meas1 UpdateUIAction is {(UpdateUIAction == null ? "null" : "OK")}");
            //  在每次量測後呼叫 UI 更新
            UpdateUIAction?.Invoke(Meas_Yxyz_1);

            await Task.Delay(100); /////確保UI 有被更新 
            Console.WriteLine("Meas_Yxyz_1: Y = " + Meas_Yxyz_1.Y + "x = " + Meas_Yxyz_1.x + "y = " + Meas_Yxyz_1.y + "z = " + Meas_Yxyz_1.z);

            double CT_n_2 = 0.0000;//zh260212 add
            double CCT_2 = 0.0000;//zh260212 add
            //    while ((Math.Abs(Meas_Yxyz_1.x - color.x) >= 0.002) ||
            //(Math.Abs(Meas_Yxyz_1.y - color.y) >= 0.002))
            //while ((Math.Abs(Meas_Yxyz_1.x - color.x) >= 0.002) &&
            //(Math.Abs(Meas_Yxyz_1.y - color.y) >= 0.002))
            while (Math.Abs(mTarget - CCT_2) >= 200)//zh260212 add
            {
                Console.WriteLine($"YYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY=" + Math.Abs(mTarget - CCT_2));
                // === 判斷方向並調整 ===
                if (Meas_Yxyz_1.x < color.x - 0.002)
                {
                    rGain = (byte)Math.Min(255, rGain + 1); // 加紅
                    bGain = (byte)Math.Max(0, bGain - 1);   // 減藍
                }
                else if (Meas_Yxyz_1.x > color.x + 0.002)
                {
                    rGain = (byte)Math.Max(0, rGain - 1);   // 減紅
                    bGain = (byte)Math.Min(255, bGain + 1); // 加藍
                }

                if (Meas_Yxyz_1.y < color.y - 0.002)
                {
                    gGain = (byte)Math.Min(255, gGain + 1); // 加綠
                                                            //  bGain = (byte)Math.Max(0, bGain - 1);   // 減藍
                }
                else if (Meas_Yxyz_1.y > color.y + 0.002)
                {
                    gGain = (byte)Math.Max(0, gGain - 1);   // 減綠
                                                            //  bGain = (byte)Math.Min(255, bGain + 1); // 加藍
                }

                //while ((Math.Abs(Meas_Yxyz_1.x - color.x) >= 0.002) ||
                //        (Math.Abs(Meas_Yxyz_1.y - color.y) >= 0.002))
                //{

                //    if (rGain == 0xFF && gGain == 0xFF && bGain == 0xFF)
                //    {
                //        if (Meas_Yxyz_1.x > color.x + 0.002)
                //            rGain -= 0x01;
                //        else
                //            bGain -= 0x01;

                //        if (Meas_Yxyz_1.y > color.y + 0.002)
                //            gGain -= 0x01;
                //        else
                //            bGain -= 0x01;
                //    }
                //    else if (rGain == 0xFF || gGain == 0xFF)
                //    {
                //        bGain -= 0x01;
                //        if (rGain == 0xFF)
                //            rGain -= 0x01;

                //        if (gGain == 0xFF)
                //            gGain -= 0x01;
                //    }
                //    else
                //    {
                //        if (Meas_Yxyz_1.x < color.x - 0.002)
                //            rGain += 0x01;

                //        if (Meas_Yxyz_1.x > color.x + 0.002)
                //            rGain -= 0x01;

                //        if (Meas_Yxyz_1.y < color.y - 0.002)
                //            gGain += 0x01;

                //        if (Meas_Yxyz_1.y > color.y + 0.002)
                //            gGain -= 0x01;
                //    }
                protocol.SetRedGain(rGain);
                protocol.SetGreenGain(gGain);
                protocol.SetBlueGain(bGain);

                Console.WriteLine("Set Gain : " + "RED gain" + rGain + "Green gain" + gGain + "Blue gain" + bGain);

                token.ThrowIfCancellationRequested();
                SensorMeasureYxy_t Meas_Yxyz_2 = new SensorMeasureYxy_t();

                bool devIsConnect_2 = _sensor.MeasYxyz(out Meas_Yxyz_2);
                if (!devIsConnect_2)
                {
                    Console.WriteLine("Please check i1d3 device connection");
                    return;
                }
                //double CT_n_2 = (Meas_Yxyz_2.x - 0.3320) / ((float)Meas_Yxyz_2.y - 0.1858);
                //double CCT_2 = -437 * CT_n_2 * CT_n_2 * CT_n_2 + 3601 * CT_n_2 * CT_n_2 - 6831 * CT_n_2 + 5517;
                CT_n_2 = (Meas_Yxyz_2.x - 0.3320) / ((float)Meas_Yxyz_2.y - 0.1858);//zh260212
                CCT_2 = -437 * CT_n_2 * CT_n_2 * CT_n_2 + 3601 * CT_n_2 * CT_n_2 - 6831 * CT_n_2 + 5517;//zh260212
                Console.WriteLine($"CCT_2=" + CCT_2);
                Console.WriteLine($"[CalibProcess] meas2 UpdateUIAction is {(UpdateUIAction == null ? "null" : "OK")}");
                // 在每次量測後呼叫 UI 更新
                UpdateUIAction?.Invoke(Meas_Yxyz_2);

                await Task.Delay(100);/////確保UI 有被更新 


                Console.WriteLine("Set RGB = [" + rGain.ToString() + ", " + gGain.ToString() + ", " + bGain.ToString() + "]");
                Console.WriteLine("Meas_Yxyz_2: Y = " + Meas_Yxyz_2.Y + "x = " + Meas_Yxyz_2.x + "y = " + Meas_Yxyz_2.y + "z = " + Meas_Yxyz_2.z);

                Meas_Yxyz_1 = Meas_Yxyz_2;
            }
            Console.WriteLine("CALIBRATION　DONE");


            token.ThrowIfCancellationRequested();

            blackWindow.canvasRoot.Children.Remove(white);

            // 等 WPF 把畫面更新成「完全沒有白方框」
            await blackWindow.Dispatcher.InvokeAsync(
                () => { },
                System.Windows.Threading.DispatcherPriority.Render
            );

            Console.WriteLine("CALIBRATION DONE for " + temperatureToUse + "K");
            await protocol.SetFactoryRGB(rGain, gGain, bGain, temperatureToUse);

            if (mainWindow.IsDebugMode)
                AppendRgbToLog(temperatureToUse, rGain, gGain, bGain);

            await protocol.ResetCTandGamma();
        }

        public static void ResetRgbLog()   ///  如果已經有舊的txt 就刪掉舊的RGB txt 
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _rgbLogFile);
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    Console.WriteLine("[LogManager] Previous RGB log file has been reset.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LogManager] Error resetting log file: {ex.Message}");
                }
            }
        }
        private static void AppendRgbToLog(int cct, byte r, byte g, byte b)     ///  純色溫的rgb 寫進txt 
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _rgbLogFile);

            bool needHeader = !File.Exists(path);

            try
            {
                using (var sw = new StreamWriter(path, append: true, Encoding.UTF8))
                {
                    if (needHeader)
                    {
                        sw.WriteLine("CCT(K)\tR\tG\tB");
                    }
                    sw.WriteLine($"{cct}\t{r}\t{g}\t{b}");
                }
                Console.WriteLine($"[LogManager] Logged: {cct}K  R={r} G={g} B={b}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LogManager] Error writing to log file: {ex.Message}");
            }
        }

        private void GetInitialRGB(int targetCCT, out byte rGain, out byte gGain, out byte bGain)
        {//// 這邊的數值都是gamma off 量出來的 
         // initial
            rGain = 128;
            gGain = 128;
            bGain = 128;

            switch (targetCCT)
            {
                case 5400:
                    rGain = 124;
                    gGain = 118;
                    bGain = 111;
                    //rGain = 157;   //wu  0617 update
                    //gGain = 116;
                    //bGain = 104;
                    break;
                case 6500:
                    //rGain = 135;
                    //gGain = 135;
                    //bGain = 121;

                    //rGain = 127;
                    //gGain = 127;
                    //bGain = 119;

                    //rGain = 135;    //wu  0617 update
                    //gGain = 136;
                    //bGain = 119;

                    //AUO G215HAN 1000nits
                    rGain = 119;
                    gGain = 120;
                    bGain = 126;

                    break;
                case 9300:
                    //rGain = 110;
                    //gGain = 110;
                    //bGain = 140;

                    //rGain = 112;
                    //gGain = 122;
                    //bGain = 143;

                    //AUO G215HAN 500nits 822P
                    rGain = 102;
                    gGain = 110;
                    bGain = 138;

                    //AUO G215HVN 1000nits
                    rGain = 97;
                    gGain = 100;
                    bGain = 126;
                    break;
                default:
                    Console.WriteLine($"[警告] 未定義的目標色溫 {targetCCT}，使用預設 128");
                    break;
            }
        }

        public async Task measure(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;

            var blackWindow = Application.Current.Windows.OfType<BlackWindow>().FirstOrDefault(w => w.IsVisible);
            blackWindow.textblockStatus.Text = "Measure Panel Native ";
            var calibrationPage = mainWindow.Calibration.Content as Calibration;
            double targetX = 0.313;
            double targetY = 0.329;
            if (mTarget == 9300)
            {
                targetX = 0.283;
                targetY = 0.297;

                //AUO G215HAN 1000nits
                //targetX = 0.284;
                //targetY = 0.293;
            }
            else if (mTarget == 5400)
            {
                //targetX = 0.340;//zh 250627 del
                //targetY = 0.350;//zh 250627 del
                //targetX = 0.335;//zh 250627
                //targetY = 0.365;//zh 250627

                //AUO G215HAN 1000nits
                targetX = 0.334;
                targetY = 0.343;
            }
            else if (mTarget == 6500)
            {
                //targetX = 0.313;
                //targetY = 0.329;

                //AUO G215HAN 1000nits
                targetX = 0.313;
                targetY = 0.323;
            }

            // wu 0623 用gamma off ct user rgb 128 128 128 量的x y 數值
            //targetX = 0.3036;//zh 250627 del
            //targetY = 0.3134;//zh 250627 del

            // 如果是非 RGBW 面板 → 先微調白點
            if (!IsRGBWPanel)
            {
                Console.WriteLine("[measure] 檢測為非 native 面板，執行白點微調");

                await FineTuneWhitePointToTargetAsync(targetX, targetY, token, mTarget);
            }

            int sampleNum = 32;

            this.xDataSample = new double[sampleNum];
            this.rLumSample = new double[sampleNum];
            this.gLumSample = new double[sampleNum];
            this.bLumSample = new double[sampleNum];
            this.wLumSample = new double[sampleNum];//zh 250619
            this.wdLumSample = new double[sampleNum];//zh 250619

            PanelColorTempData panelColorTempData = new PanelColorTempData();

            token.ThrowIfCancellationRequested();

            // 先測量一次整體黑點，或使用 MeasureColorVector() 中得到的 black.Y
            setPatternColor(0, 0, 0);
            await Task.Delay(100); // 等待穩定

            _sensor.MeasYxyz(out SensorMeasureYxy_t overallBlackMeasurement);
            double blackPointY = overallBlackMeasurement.Y;
            Console.WriteLine("開始測量 red/green/blue gamma 曲線 (已扣除黑點)...");
            Console.WriteLine($"量測 Native Panel");
            calibrationPage?.AddCalibrationLog($"量測 Native Panel", "", "", "");
            calibrationPage?.AddCalibrationLog($"   Level", "      x", "     y", "     Y");

            for (int i = 0; i < sampleNum; i++)
            {
                int level = (i * 255) / (sampleNum - 1);
                xDataSample[i] = level / 255.0;

                setPatternColor(level, 0, 0);
                await Task.Delay(300);
                _sensor.MeasYxyz(out SensorMeasureYxy_t r_raw);
                rLumSample[i] = Math.Max(0, r_raw.Y - blackPointY);// * (double)0.98; // 減去黑點

                Console.WriteLine($"Level {level} r_raw : x={r_raw.x:F4}, y={r_raw.y:F4}, Y={r_raw.Y:F4}");
                calibrationPage?.AddCalibrationLog($"Level {level} r_raw", r_raw.x.ToString("F4"), r_raw.y.ToString("F4"), r_raw.Y.ToString("F4"));
                await Task.Delay(10);


                // 更新右邊數值面板
                UpdateUIAction?.Invoke(r_raw);

                setPatternColor(0, level, 0);
                await Task.Delay(300);
                _sensor.MeasYxyz(out SensorMeasureYxy_t g_raw);
                gLumSample[i] = Math.Max(0, g_raw.Y - blackPointY);//*(double)0.98; // 減去黑點
                calibrationPage?.AddCalibrationLog($"Level {level} g_raw", g_raw.x.ToString("F4"), g_raw.y.ToString("F4"), g_raw.Y.ToString("F4"));
                await Task.Delay(10);
                Console.WriteLine($"Level {level} g_raw : x={g_raw.x:F4}, y={g_raw.y:F4}, Y={g_raw.Y:F4}");
                UpdateUIAction?.Invoke(g_raw);

                setPatternColor(0, 0, level);
                await Task.Delay(300);
                _sensor.MeasYxyz(out SensorMeasureYxy_t b_raw);
                bLumSample[i] = Math.Max(0, b_raw.Y - blackPointY); // 減去黑點
                calibrationPage?.AddCalibrationLog($"Level {level} b_raw", b_raw.x.ToString("F4"), b_raw.y.ToString("F4"), b_raw.Y.ToString("F4"));
                await Task.Delay(10);
                Console.WriteLine($"Level {level} b_raw : x={b_raw.x:F4}, y={b_raw.y:F4}, Y={b_raw.Y:F4}");
                UpdateUIAction?.Invoke(b_raw);

            }

            int expandSize = _panelInfo.LutResolution; //wu 250707
            // 確保 panelColorTempData 是 CalibProcess 的成員變數
            this.panelColorTempData.redLum = CubicInterpolator.interpolation(xDataSample, rLumSample, expandSize);
            this.panelColorTempData.greenLum = CubicInterpolator.interpolation(xDataSample, gLumSample, expandSize);
            this.panelColorTempData.blueLum = CubicInterpolator.interpolation(xDataSample, bLumSample, expandSize);

            Console.WriteLine("Red/Green/Blue gamma 曲線內插完成 (已扣除黑點)");
        }


        /// <summary>
        /// 只做一次完整量測（黑點、32 階灰階、Fine-Tune 白點、Color Vector）
        /// </summary>
        private async Task<BaseData> MeasureDICOMBaseOnceAsync(int cct,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            LoadNativeRGB_FromFile();//ian 260617 add 從ALC生成.txt檔案中讀取純色溫的RGB數值
            var mainWin = (MainWindow)Application.Current.MainWindow;
            var blackWindow = Application.Current.Windows
                            .OfType<BlackWindow>()
                            .FirstOrDefault(w => w.IsVisible);
            var calibrationPage = mainWin.Calibration.Content as Calibration;

            List<gammaMeasureData> measList = new List<gammaMeasureData>();

            Console.WriteLine($"[{cct}K] 量測 32 階灰階開始");
            calibrationPage?.AddCalibrationLog($"Start Measure 32-step gray @", $"{cct}K", "", "");
            calibrationPage?.AddCalibrationLog($"   Level", "      x", "     y", "     Y");

            blackWindow.textblockStatus.Text = "Start Measure 32 - step gray @ " + cct + "K";

            // 畫黑框、量黑點 
            Rectangle wBox = blackWindow.CreateCenteredWhiteBox();
            blackWindow.canvasRoot.Children.Add(wBox);
            double ambientLight = 0.56;//zh 260627 TBD
            wBox.Fill = Brushes.Black;
            await Task.Delay(1500, token);
            _sensor.MeasYxyz(out SensorMeasureYxy_t resBlack);
            double blackY = resBlack.Y;
            //   calibrationPage?.AddCalibrationLog("Level 0", resBlack.x.ToString("F4"), resBlack.y.ToString("F4"), resBlack.Y.ToString("F2"));
            // Console.WriteLine($"Level 0: x={resBlack.x:F4}, y={resBlack.y:F4}, Y={resBlack.Y:F4}");
            gammaMeasureData measureDataLow = new gammaMeasureData
            {
                gray_level = 0,
                x = Math.Round(resBlack.x, 4),
                y = Math.Round(resBlack.y, 4),
                //Y = Math.Round(result.Y, 4)   //gpt   正確
                Y = Math.Round(resBlack.Y, 4) + ambientLight//zh 260627

            };
            measList.Add(measureDataLow);

            //  量 32 點灰階

            int sampleNum = 32;

            for (int lvl = 1; lvl < sampleNum; lvl++)
            {
                int gray = 255 * lvl / (sampleNum - 1);
                byte g = (byte)gray;
                wBox.Fill = new SolidColorBrush(Color.FromRgb(g, g, g));
                await Task.Delay(500, token);

                bool ok = _sensor.MeasYxyz(out SensorMeasureYxy_t res);
                if (!ok) { Console.WriteLine($"  ✖ Lv{lvl} 測量失敗"); continue; }

                gammaMeasureData measureData = new gammaMeasureData
                {
                    gray_level = gray,
                    x = Math.Round(res.x, 4),
                    y = Math.Round(res.y, 4),
                    Y = Math.Round(res.Y, 4)
                };
                measList.Add(measureData);

                Console.WriteLine($"Level {gray}: x={res.x:F4}, y={res.y:F4}, Y={res.Y:F4}");
                calibrationPage?.AddCalibrationLog($"Level {gray}", res.x.ToString("F4"), res.y.ToString("F4"), res.Y.ToString("F4"));
            }

            // Fine-Tune 白點 & 量 Color Vector
            double targetX = 0.313, targetY = 0.329;   // 6500 K = D65
            if (cct == 9300) { targetX = 0.283; targetY = 0.297; }
            else if (cct == 5400) { targetX = 0.335; targetY = 0.365; }

            PatternRGB fineWhite = await FineTuneWhitePointToTargetAsync(targetX, targetY, token, cct);
            ColorVector cv = await MeasureColorVector(fineWhite.R, fineWhite.G, fineWhite.B);

            blackWindow.canvasRoot.Children.Remove(wBox);
            Console.WriteLine($"[{cct}K] 基礎量測完成\n");
            double[] yValues = measList.Select(m => m.Y).ToArray();

            return new BaseData
            {
                MeasY = yValues,
                Cv = cv
            };
        }

        private void LoadNativeRGB_FromFile() // ian 260617 add 從ALC生成.txt檔案中讀取純色溫的RGB數值
        {
            // 如果已經讀過，就直接跳過不讀檔
            if (_isNativeRgbLoaded)
            {
                return;
            }
            string nativePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NativeRGB.txt");

            if (System.IO.File.Exists(nativePath))
            {
                var lines = System.IO.File.ReadAllLines(nativePath);

                // 確保檔案至少包含標題列 + 至少一筆資料 (大於 1 行)
                if (lines.Length > 1)
                {
                    int dataCount = lines.Length - 1;
                    int[] tempR = new int[dataCount];
                    int[] tempG = new int[dataCount];
                    int[] tempB = new int[dataCount];

                    for (int i = 1; i < lines.Length; i++)
                    {
                        var parts = lines[i].Split('\t');
                        if (parts.Length >= 4)
                        {
                            tempR[i - 1] = int.Parse(parts[1]);
                            tempG[i - 1] = int.Parse(parts[2]);
                            tempB[i - 1] = int.Parse(parts[3]);
                        }
                    }

                    // 寫回全域變數
                    GlobalCalibData.Native_R = tempR;
                    GlobalCalibData.Native_G = tempG;
                    GlobalCalibData.Native_B = tempB;

                    if (GlobalCalibData.Native_R.Length > 1)
                    {
                        Console.WriteLine($"[CalibProcess] Native RGB 已從檔案讀回，R[1]={GlobalCalibData.Native_R[1]}, G[1]={GlobalCalibData.Native_G[1]}, B[1]={GlobalCalibData.Native_B[1]}");
                    }

                    // 讀取成功後，標記為 true，下次就不會再進來了
                    _isNativeRgbLoaded = true;
                }
            }
        }
        private async Task<BaseData> MeasureGammaBaseOnceAsync(int cct, CancellationToken token)
        {

            token.ThrowIfCancellationRequested();
            LoadNativeRGB_FromFile();//ian 260617 add 從ALC生成.txt檔案中讀取純色溫的RGB數值
            var mainWin = (MainWindow)Application.Current.MainWindow;
            var blackWindow = Application.Current.Windows
                            .OfType<BlackWindow>()
                            .FirstOrDefault(w => w.IsVisible);
            var calibrationPage = mainWin.Calibration.Content as Calibration;

            Console.WriteLine($"[{cct}K] 量測 32 階灰階開始");
            calibrationPage?.AddCalibrationLog($"Start Measure 32-step gray @", $"{cct}K", "", "");
            calibrationPage?.AddCalibrationLog($"   Level", "      x", "     y", "     Y");

            blackWindow.textblockStatus.Text = "Start Measure 32 - step gray @ " + cct + "K";

            // ---------- Step 1: 畫黑框、量黑點 ----------
            Rectangle wBox = blackWindow.CreateCenteredWhiteBox();
            blackWindow.canvasRoot.Children.Add(wBox);

            wBox.Fill = Brushes.Black;
            await Task.Delay(1500, token);
            _sensor.MeasYxyz(out SensorMeasureYxy_t resBlack);
            double blackY = resBlack.Y;
            //   calibrationPage?.AddCalibrationLog("Level 0", resBlack.x.ToString("F4"), resBlack.y.ToString("F4"), resBlack.Y.ToString("F2"));
            // Console.WriteLine($"Level 0: x={resBlack.x:F4}, y={resBlack.y:F4}, Y={resBlack.Y:F4}");
            // ---------- Step 2: 量 32 點灰階 ----------
            var measList = new List<double>();
            int sampleNum = 32;

            for (int lvl = 0; lvl < sampleNum; lvl++)
            {
                int gray = 255 * lvl / (sampleNum - 1);
                byte g = (byte)gray;
                wBox.Fill = new SolidColorBrush(Color.FromRgb(g, g, g));
                await Task.Delay(500, token);

                bool ok = _sensor.MeasYxyz(out SensorMeasureYxy_t res);
                if (!ok) { Console.WriteLine($"  ✖ Lv{lvl} 測量失敗"); continue; }

                double yCorr = Math.Max(0, res.Y - blackY);
                measList.Add(yCorr);

                // Console.WriteLine($"Level {gray}", res.x.ToString("F4"), res.y.ToString("F4"), res.Y.ToString("F4"));
                Console.WriteLine($"Level {gray}: x={res.x:F4}, y={res.y:F4}, Y={res.Y:F4}");
                calibrationPage?.AddCalibrationLog($"Level {gray}", res.x.ToString("F4"), res.y.ToString("F4"), res.Y.ToString("F4"));

            }

            // ---------- Step 3: Fine-Tune 白點 & 量 Color Vector ----------
            double targetX = 0.313, targetY = 0.329;   // 6500 K = D65
            if (cct == 9300) { targetX = 0.283; targetY = 0.297; }
            else if (cct == 5400) { targetX = 0.335; targetY = 0.365; }

            //AUO G215HAN 1000nits
            //double targetX = 0.300, targetY = 0.310;   // 6500 K = D65
            //if (cct == 9300) { targetX = 0.276; targetY = 0.283; }
            //else if (cct == 5400) { targetX = 0.325; targetY = 0.335; }

            PatternRGB fineWhite = await FineTuneWhitePointToTargetAsync(targetX, targetY, token, cct);
            ColorVector cv = await MeasureColorVector(fineWhite.R, fineWhite.G, fineWhite.B);


            //Gamma Color Vector
            // ---------- Step 4: 把 Color Vector 存入全域變數，並更新備份 ---------- 
            double real_rY = cv.rVector * cv.MaxLuminance;
            double real_gY = cv.gVector * cv.MaxLuminance;
            double real_bY = cv.bVector * cv.MaxLuminance;

            // 自己算出真正的物理總和 (rY + gY + bY)
            double real_sumY = real_rY + real_gY + real_bY;
            if (real_sumY <= 0) real_sumY = 1e-6; // 避免除以零

            // 算出 OSD 專用的正確比例
            int vR = (int)Math.Round((real_rY / real_sumY) * 10000.0);
            int vG = (int)Math.Round((real_gY / real_sumY) * 10000.0);
            int vB = (int)Math.Round((real_bY / real_sumY) * 10000.0);

            // 1. 不管量測哪個 CCT，先把專屬的 32 個亮度點備份起來！
            for (int i = 0; i < 32; i++)
            {
                // measList是扣黑後的Net Y
                // 寫入Flash前把blackY加回去，恢復Absolute Y
                double absoluteY = measList[i] + blackY;
                int val = (int)Math.Round(absoluteY * 100.0);
                if (cct == 5400) backup_5400_Y_ints[i] = val;
                else if (cct == 6500) backup_6500_Y_ints[i] = val;
                else if (cct == 9300) backup_9300_Y_ints[i] = val;
                if (i == 0 || i == 16 || i == 31)
                {
                    Console.WriteLine($"[DEBUG] [{cct}K ] Index [{i:D2}]: 原 Y = {measList[i]:F4} -> 存入 = {val}");
                }
                
            }

            // 2. 存入對應的 Vector
            if (cct == 5400)
            {
                CalibProcess.vector_5400[0] = vR;
                CalibProcess.vector_5400[1] = vG;
                CalibProcess.vector_5400[2] = vB;
            }
            else if (cct == 6500)
            {
                CalibProcess.vector_6500[0] = vR;
                CalibProcess.vector_6500[1] = vG;
                CalibProcess.vector_6500[2] = vB;
            }
            else if (cct == 9300)
            {
                CalibProcess.vector_9300[0] = vR;
                CalibProcess.vector_9300[1] = vG;
                CalibProcess.vector_9300[2] = vB;
                if (!enableFlashWrite)
            {
                    Console.WriteLine("[EEPROM] 開始全面寫入 5400K, 6500K, 9300K 區塊 (包含 R, G, B 三通道)...");
                    for (byte ch = 0; ch < 3; ch++)
                    {
                        await WriteBlockAsync(0x36, ch, backup_5400_Y_ints);
                        await WriteBlockAsync(0x41, ch, backup_6500_Y_ints);
                        await WriteBlockAsync(0x5D, ch, backup_9300_Y_ints);
                    }
                }
            }

            blackWindow.canvasRoot.Children.Remove(wBox);
            Console.WriteLine($"✓ [{cct}K] 基礎量測完成\n");

            return new BaseData
            {
                MeasY = measList.ToArray(),
                Cv = cv
            };
        }

        async Task WriteBlockAsync(byte targetCtCode, byte targetCh, int[] sourceData) // 負責把陣列轉換成封包寫回ADboard
        {
            var mainWin = (MainWindow)Application.Current.MainWindow;
            var i2c = mainWin?.GetI2CController();
            var protocol = new CalibrationProtocol(i2c);
            LUTData dummyLut = new LUTData(256, 65535);
            Array.Clear(dummyLut.RLUT, 0, dummyLut.RLUT.Length);
            Array.Clear(dummyLut.GLUT, 0, dummyLut.GLUT.Length);
            Array.Clear(dummyLut.BLUT, 0, dummyLut.BLUT.Length);
            // 動態指派正確的 Buffer 
            int[] targetBuffer;
            if (targetCh == 0) targetBuffer = dummyLut.RLUT;
            else if (targetCh == 1) targetBuffer = dummyLut.GLUT;
            else targetBuffer = dummyLut.BLUT;

            if (targetCtCode == 0x36) //5400(Y)
            {
                for (int i = 0; i < 20; i++) dummyLut.RLUT[i] = sourceData[i];
                dummyLut.RLUT[20] = CalibProcess.vector_5400[0]; dummyLut.RLUT[21] = CalibProcess.vector_5400[1]; dummyLut.RLUT[22] = CalibProcess.vector_5400[2];
                dummyLut.RLUT[24] = CalibProcess.vector_6500[0]; dummyLut.RLUT[25] = CalibProcess.vector_6500[1]; dummyLut.RLUT[26] = CalibProcess.vector_6500[2];
                dummyLut.RLUT[28] = CalibProcess.vector_9300[0]; dummyLut.RLUT[29] = CalibProcess.vector_9300[1]; dummyLut.RLUT[30] = CalibProcess.vector_9300[2];
                dummyLut.RLUT[31] = 9300; //  STM32 的交握密碼
            }
            else
            {
                // 其他填滿 32 個亮度點
                for (int i = 0; i < 32; i++) targetBuffer[i] = sourceData[i];
            }

            // 發送 4 個封包
            for (byte idx = 0; idx < 4; idx++)
            {
                byte retCode = 255;
                int retryCount = 3;
                while (retryCount > 0)
                {
                    retCode = i2c.DDCCI_Set_CommandGammaCT(0x4E, targetCtCode, targetCh, idx, dummyLut);

                    if (retCode == 0)
                    {
                        Console.WriteLine($"[I2C 成功] 區塊 0x{targetCtCode:X2} 封包 {idx}/3 已發送並確認！");
                        break;
                    }

                    Console.WriteLine($"[I2C 警告] 區塊 0x{targetCtCode:X2} 封包 {idx}/3 失敗 (碼:{retCode})，準備重試... 剩餘次數: {retryCount - 1}");
                    await Task.Delay(800);
                    retryCount--;
                }

                // 如果 3 次都失敗，印出致命錯誤，日後查 Log 一目了然
                if (retCode != 0)
                {
                    Console.WriteLine($"[I2C 致命錯誤] 區塊 0x{targetCtCode:X2} 封包 {idx}/3 徹底失敗！");
                }

                // 給 Flash 充足的寫入時間 (統一拉長到 1000 比較保險)
                await Task.Delay(1000);
            }
            Console.WriteLine($"      5400K -> R={dummyLut.RLUT[20]}, G={dummyLut.RLUT[21]}, B={dummyLut.RLUT[22]}");
            Console.WriteLine($"      6500K -> R={dummyLut.RLUT[24]}, G={dummyLut.RLUT[25]}, B={dummyLut.RLUT[26]}");
            Console.WriteLine($"      9300K -> R={dummyLut.RLUT[28]}, G={dummyLut.RLUT[29]}, B={dummyLut.RLUT[30]}");
        }
        private async Task FlushBaseDataToFlashAsync(int currentCCT, string currentMode)//ian add 260618 先清除Flash再燒入Flash
        {
            if (!enableFlashWrite)//檢查 Flash 寫入開關是否開啟
            {
                Console.WriteLine($"[I2C 測試模式] enableFlashWrite 為 false，已跳過 Flash 的清除與寫入。");
                return;
            }
            var mainWin = (MainWindow)Application.Current.MainWindow;
            var i2c = mainWin?.GetI2CController();

            bool isAllMeasured = _baseCache.ContainsKey(5400) &&
                                 _baseCache.ContainsKey(6500) &&
                                 _baseCache.ContainsKey(9300);
            bool isFinalStep = false;
            if (isDoingDICOMCalib)
            {
                // 情況 A：如果有測 DICOM，強制等 DICOM 的 9300K 跑完才放行
                if (currentMode == "DICOM" && currentCCT == 9300)
                {
                    isFinalStep = true;
                }
            }
            // 確保硬體連線正常，且三個色溫的暫存陣列都有資料
            if (i2c != null && isAllMeasured && !_isI1DataFlushed && isFinalStep)
            {
                var protocol = new CalibrationProtocol(i2c);

                Console.WriteLine($"[I2C] 正在清除 i1display Flash 區塊...");
                await protocol.GammaErase_i1display();

                Console.WriteLine("[I2C] 開始全面回寫 6 包資料...");

                // 寫入 Native RGB
                await WriteBlockAsync(0x00, 0, GlobalCalibData.Native_R);
                await WriteBlockAsync(0x00, 1, GlobalCalibData.Native_G);
                await WriteBlockAsync(0x00, 2, GlobalCalibData.Native_B);

                // 寫入三個色溫的亮度矩陣
                await WriteBlockAsync(0x36, 0, backup_5400_Y_ints);
                await WriteBlockAsync(0x41, 1, backup_6500_Y_ints);
                await WriteBlockAsync(0x5D, 2, backup_9300_Y_ints);

                Console.WriteLine($"[I2C] 基礎資料 Flash 全面回寫成功！");
                _isI1DataFlushed = true;
            }
            else if (_isI1DataFlushed)
            {
                Console.WriteLine($"[I2C] i1display 基礎資料已寫入過，跳過重複擦寫。");
            }
            else
            {
                Console.WriteLine($"[狀態] 量測資料不齊全 (尚未集齊 5400/6500/9300) 或 I2C 異常，暫不寫入 Flash。");
            }
        }
        private async Task CalibrateAllGammaAsync(IList<CalibTarget> gammaTargets, CancellationToken token)
        {
            //  確保所有目標色溫的基礎數據都已量測完畢
            foreach (var target in gammaTargets)
            {
                token.ThrowIfCancellationRequested();
                int cct = target.CCT;

                if (!_baseCache.TryGetValue(cct, out BaseData baseData))
                {
                    Console.WriteLine($"[流程] 開始量測基礎資料: {cct}K");
                    baseData = await MeasureGammaBaseOnceAsync(cct, token);
                    _baseCache[cct] = baseData;
                }
            }
            // 呼叫共用的 Flash 寫入邏輯
            await FlushBaseDataToFlashAsync(9300, "GAMMA");
            //基礎資料寫入完成後，根據量測結果產生 LUT 並燒錄
            foreach (var target in gammaTargets)
            {
                token.ThrowIfCancellationRequested();
                int cct = target.CCT;
                double gamma = target.Gamma;

                // 從快取中取出剛才量好的資料
                var baseData = _baseCache[cct];

                calib.GenerateLUTFromMeasuredY(
                    cct,
                    measuredY: baseData.MeasY.ToList(),
                    redLum: panelColorTempData.redLum,
                    greenLum: panelColorTempData.greenLum,
                    blueLum: panelColorTempData.blueLum,
                    colorVector: baseData.Cv,
                    targetGamma: gamma);

                Console.WriteLine($"產生 LUT：{cct}K @ Gamma={gamma:F2}");
                await calib.PrintLUTAsHexText(mTarget, gamma);
            }
        }

        private async Task CalibrateAllDICOMAsync(IList<CalibTarget> dicomTargets, double jndScale, CancellationToken token)
        {
            // ==========================================
            // Phase 1: 確保所有目標色溫的基礎數據都已量測完畢
            // ==========================================
            foreach (var target in dicomTargets)
            {
                token.ThrowIfCancellationRequested();
                int cct = target.CCT;

                // -------- 先取得or建立基礎量測資料 --------
                if (!_baseCache.TryGetValue(cct, out BaseData baseData))
                {
                    Console.WriteLine($"[流程-DICOM] 開始量測基礎資料: {cct}K");
                    baseData = await MeasureDICOMBaseOnceAsync(cct, token);
                    _baseCache[cct] = baseData;
                }
            }

            // ==========================================
            // Phase 2: 呼叫共用的 Flash 寫入邏輯
            // ==========================================
            //await FlushBaseDataToFlashAsync();
            // ==========================================
            // Phase 3: 產生 DICOM LUT 並輸出
            // ==========================================
            foreach (var target in dicomTargets)
            {
                token.ThrowIfCancellationRequested();
                int cct = target.CCT;

                // 從快取中取出剛才量好的資料
                var baseData = _baseCache[cct];

                // -------- 產生 LUT --------
                calib.GenerateLUTFromMeasuredY_DICOM(
                    cct,
                    measuredY: baseData.MeasY.ToList(),
                    redLum: panelColorTempData.redLum,
                    greenLum: panelColorTempData.greenLum,
                    blueLum: panelColorTempData.blueLum,
                    colorVector: baseData.Cv,
                    JND_Scale: jndScale);

                Console.WriteLine($" 產生 DICOM LUT：{cct}K @ JND={jndScale:F2}");

                var mainWin = (MainWindow)Application.Current.MainWindow;
                var calibration = mainWin.Calibration.Content as Calibration;

                token.ThrowIfCancellationRequested();
                await calib.PrintDicomLUTAsHexText(cct, jndScale);
            }
        }
        public async Task dicomCalib(double JND_Scale, int targetCCT, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var mainWin = (MainWindow)Application.Current.MainWindow;
            var blackWindow = Application.Current.Windows.OfType<BlackWindow>().FirstOrDefault(w => w.IsVisible);
            if (blackWindow == null)
            {
                MessageBox.Show("找不到 BlackWindow，無法進行 Gamma 校正");
                return;
            }

            blackWindow.textblockStatus.Text = "Calibration DICOM  " + targetCCT;

            Rectangle whiteBox = null;
            whiteBox = blackWindow.CreateCenteredWhiteBox();
            blackWindow.canvasRoot.Children.Add(whiteBox);

            List<gammaMeasureData> measureDataList = new List<gammaMeasureData>();

            var calibrationPage = mainWin.Calibration.Content as Calibration;

            int sample_num = 32;
            // double ambientLight = 0.0;//zh 250715
            string ambientText = calibrationPage.textBoxAmbientLight.Text;

            if (!double.TryParse(ambientText, out double ambientLight))
            {
                Debug.WriteLine($"Diff Rate \"{calibrationPage.textBoxAmbientLight.Text}\" is in valid");
                return;
            }
            Console.WriteLine("ambientLight in  dicomCalib " + ambientLight);
            // ✅ Step 1: 黑畫面測量，當作黑點
            whiteBox.Fill = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            await Task.Delay(500);//zh 250627
            _sensor.MeasYxyz(out SensorMeasureYxy_t result);
            double blackPointY = result.Y;

            double correctedY = Math.Max(0, result.Y - blackPointY);

            gammaMeasureData measureDataLow = new gammaMeasureData
            {
                gray_level = 0,
                x = Math.Round(result.x, 4),
                y = Math.Round(result.y, 4),
                //Y = Math.Round(result.Y, 4)   //gpt   正確
                Y = Math.Round(result.Y, 4) + ambientLight,//zh 260627

            };
            measureDataList.Add(measureDataLow);
            calibrationPage?.AddCalibrationLog($"\nCalibration DICOM Target : ", "\n" + targetCCT, "", "");
            calibrationPage?.AddCalibrationLog($"   Level", "      x", "     y", "     Y");
            calibrationPage?.AddCalibrationLog("Level 0", result.x.ToString("F4"), result.y.ToString("F4"), result.Y.ToString("F2"));
            token.ThrowIfCancellationRequested();
            // ✅ Step 2: 實測 32 點灰階 Y，扣掉黑點
            for (int level = 1; level < sample_num; level++)
            {
                int gray = 255 * level / (sample_num - 1);
                byte grayByte = Convert.ToByte(gray);
                whiteBox.Fill = new SolidColorBrush(Color.FromRgb(grayByte, grayByte, grayByte));
                blackWindow.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
                await Task.Delay(500);

                bool ok = _sensor.MeasYxyz(out result);
                if (!ok)
                {
                    Console.WriteLine($"[GammaCalib] 第 {level} 階測量失敗");
                    continue;
                }
                Console.WriteLine($"[GammaCalib] 第 {level} 階測量 {result.Y}");
                gammaMeasureData measureData = new gammaMeasureData
                {
                    gray_level = gray,
                    x = Math.Round(result.x, 4),
                    y = Math.Round(result.y, 4),

                    Y = Math.Round(result.Y, 4) + ambientLight,//zh 250715
                };
                measureDataList.Add(measureData);
                UpdateUIAction?.Invoke(result);
                // 同步推文字到左側 ListBox
                //Event_ConsoleWriteLine?.Invoke($"Calibrate  {result}");

                calibrationPage?.AddCalibrationLog($"Level {gray}", measureData.x.ToString("F4"), measureData.y.ToString("F4"), measureData.Y.ToString("F2"));
            }

            blackWindow.canvasRoot.Children.Remove(whiteBox);


            // ✅ Step 3: 取得 gain 值（對應 calibrate 中使用的）
            GetInitialRGB(mTarget, out byte rGain, out byte gGain, out byte bGain);
            token.ThrowIfCancellationRequested();

            // ✅ Step 3: 測 color vector
            // ColorVectorMeasured = await MeasureColorVector(rGain, gGain, bGain);


            //zh251203
            double targetX = 0.313, targetY = 0.349;
            if (mTarget == 9300) { targetX = 0.283; targetY = 0.297; }
            else if (mTarget == 5400) { targetX = 0.343; targetY = 0.359; }

            //AUO G215HAN 1000nits
            //double targetX = 0.300, targetY = 0.310;   // 6500 K = D65
            //if (mTarget == 9300) { targetX = 0.276; targetY = 0.283; }
            //else if (mTarget == 5400) { targetX = 0.325; targetY = 0.335; }

            PatternRGB fineTunedWhite = await FineTuneWhitePointToTargetAsync(targetX, targetY, token, mTarget);
            ColorVectorMeasured = await MeasureColorVector(fineTunedWhite.R, fineTunedWhite.G, fineTunedWhite.B);

            //Dicom Color Vector
            // ==============================================================================
            double real_rY = ColorVectorMeasured.rVector * ColorVectorMeasured.MaxLuminance;
            double real_gY = ColorVectorMeasured.gVector * ColorVectorMeasured.MaxLuminance;
            double real_bY = ColorVectorMeasured.bVector * ColorVectorMeasured.MaxLuminance;

            // 自己算出真正的物理總和 (rY + gY + bY)
            double real_sumY = real_rY + real_gY + real_bY;
            if (real_sumY <= 0) real_sumY = 1e-6; // 避免除以零

            // 算出 OSD 專用的正確比例！
            int vR = (int)Math.Round((real_rY / real_sumY) * 10000.0);
            int vG = (int)Math.Round((real_gY / real_sumY) * 10000.0);
            int vB = (int)Math.Round((real_bY / real_sumY) * 10000.0);
            // 1. 不管量測哪個 CCT，先把專屬的 32 個亮度點備份起來！
            for (int i = 0; i < 32; i++)

            {
                int val = (int)Math.Round(measureDataList[i].Y * 100.0);
                if (mTarget == 5400) backup_5400_Y_ints[i] = val;
                else if (mTarget == 6500) backup_6500_Y_ints[i] = val;
                else if (mTarget == 9300) backup_9300_Y_ints[i] = val;
                if (i == 0 || i == 16 || i == 31)
                {
                    Console.WriteLine($"[DEBUG] [{mTarget}K ] Index [{i:D2}]: 原 Y = {measureDataList[i].Y:F4} -> 存入 = {val}");
                }
            }
            // 根據目前色溫，更新對應的全域變數
            if (mTarget == 5400)
            {
                CalibProcess.vector_5400[0] = vR;
                CalibProcess.vector_5400[1] = vG;
                CalibProcess.vector_5400[2] = vB;

            }
            else if (mTarget == 6500)
            {
                CalibProcess.vector_6500[0] = vR;
                CalibProcess.vector_6500[1] = vG;
                CalibProcess.vector_6500[2] = vB;
            }
            else if (mTarget == 9300)
            {
                CalibProcess.vector_9300[0] = vR;
                CalibProcess.vector_9300[1] = vG;
                CalibProcess.vector_9300[2] = vB;
                if (!enableFlashWrite)
                {
                    Console.WriteLine("[EEPROM] 三個色溫皆已收集，開始寫入 0x36 區塊...");
                    await WriteBlockAsync(0x36, 0, backup_5400_Y_ints);
                }

            }
            _baseCache[targetCCT] = new BaseData
            {
                MeasY = measureDataList.Select(d => d.Y).ToArray(),
                Cv = ColorVectorMeasured
            };
            await FlushBaseDataToFlashAsync(targetCCT, "DICOM");
            // ==============================================================================
            token.ThrowIfCancellationRequested();
            // ✅ Step 4: 計算 Gamma
            double[] measY = measureDataList.Select(d => d.Y).ToArray();
            //zh 250715//double gamma_result = GammaStandard.getGamma(measY);
            //zh 250715//Console.WriteLine($"Gamma {mTarget}K Result: {gamma_result:F4}");
            token.ThrowIfCancellationRequested();
            calib.GenerateLUTFromMeasuredY_DICOM(mTarget,measuredY: measY.ToList(),redLum: this.panelColorTempData.redLum,greenLum: this.panelColorTempData.greenLum,blueLum: this.panelColorTempData.blueLum,
            colorVector: ColorVectorMeasured,JND_Scale: JND_Scale);
            await calib.PrintDicomLUTAsHexText(mTarget, JND_Scale);
        }
        private async Task<ColorVector> MeasureColorVector(int rPattern, int gPattern, int bPattern)
        {
            var mainWin = (MainWindow)Application.Current.MainWindow;
            var calibrationPage = mainWin.Calibration.Content as Calibration;
            var blackWindow = Application.Current.Windows.OfType<BlackWindow>().FirstOrDefault(w => w.IsVisible);



            setPatternColor(0, 0, 0);
            await Task.Delay(300);//zh 250627
            _sensor.MeasYxyz(out SensorMeasureYxy_t black);

            setPatternColor(rPattern, 0, 0);
            await Task.Delay(300);//zh 250627
            _sensor.MeasYxyz(out SensorMeasureYxy_t red);

            setPatternColor(0, gPattern, 0);
            await Task.Delay(300);//zh 250627
            _sensor.MeasYxyz(out SensorMeasureYxy_t green);

            setPatternColor(0, 0, bPattern);
            await Task.Delay(300);//zh 250627
            _sensor.MeasYxyz(out SensorMeasureYxy_t blue);

            //zh 250627 >
            setPatternColor(rPattern, rPattern, rPattern);
            await Task.Delay(300);//zh 250627
            _sensor.MeasYxyz(out SensorMeasureYxy_t wred);

            setPatternColor(gPattern, gPattern, gPattern);
            await Task.Delay(300);//zh 250627
            _sensor.MeasYxyz(out SensorMeasureYxy_t wgreen);

            setPatternColor(bPattern, bPattern, bPattern);
            await Task.Delay(300);//zh 250627
            _sensor.MeasYxyz(out SensorMeasureYxy_t wblue);

            //setPatternColor(rPattern, gPattern, bPattern);
            //await Task.Delay(300);
            //sensor_1.MeasYxyz(out SensorMeasureYxy_t wwhite);
            //zh 250627 <
            blackWindow.canvasRoot.Children.Remove(blackWindow.whiteBox);
            blackWindow.whiteBox = null;

            //  分別扣除黑點 Y 分量（建議改成 clamp 避免負值）
            double rY = Math.Max(0, red.Y - black.Y);
            double gY = Math.Max(0, green.Y - black.Y);
            double bY = Math.Max(0, blue.Y - black.Y);
            // zh 250627>
            double wrY = Math.Max(0, wred.Y - black.Y);
            double wgY = Math.Max(0, wgreen.Y - black.Y);
            double wbY = Math.Max(0, wblue.Y - black.Y);
            //double wwY = Math.Max(0, wwhite.Y - black.Y);
            // zh 250627<
            double total = rY + gY + bY;
            if (total <= 0) total = 1e-6; // avoid NaN

            double x = (rY * red.x + gY * green.x + bY * blue.x) / total;
            double y = (rY * red.y + gY * green.y + bY * blue.y) / total;

            double CT_n = (x - 0.3320) / (y - 0.1858);
            double CCT = -437 * CT_n * CT_n * CT_n + 3601 * CT_n * CT_n - 6831 * CT_n + 5517;

            Console.WriteLine($" Red:   x={red.x:F4}, y={red.y:F4}, Y={red.Y:F4}");
            Console.WriteLine($" Green: x={green.x:F4}, y={green.y:F4}, Y={green.Y:F4}");
            Console.WriteLine($" Blue:  x={blue.x:F4}, y={blue.y:F4}, Y={blue.Y:F4}");
            Console.WriteLine($" Black: x={black.x:F4}, y={black.y:F4}, Y={black.Y:F4}");

            Console.WriteLine($" wrY ={wrY:F4}, wgY ={wgY:F4}, wbY={wbY:F4}");

            Console.WriteLine($" rY  ={rY:F4}, gY  ={gY:F4}, bY ={bY:F4}");
            Console.WriteLine($"rVector={rY / total:F4}, gVector={gY / total:F4}, bVector={bY / total:F4}");


            //zh 250627>
            if (mTarget > 9000)
            {
                total = wbY;
            }
            else if ((mTarget > 6000) && (mTarget < 9000))
            {
                total = wgY;
            }
            else
            {
                total = wrY;
            }
            Console.WriteLine($"Total = {total:F4}, mTarget= {mTarget}");
            //zh 250627<
            return new ColorVector
            {
                rVector = rY / total,
                gVector = gY / total,
                bVector = bY / total,
                MaxLuminance = total
            };

        }

        private void setPatternColor(int r, int g, int b)
        {
            Console.WriteLine($"setPatternColor --> R = {r:000} , G = {g:000} , B = {b:000}");
            var blackWindow = Application.Current.Windows.OfType<BlackWindow>().FirstOrDefault(w => w.IsVisible);
            if (blackWindow != null)
            {
                // 清掉舊的內容
                blackWindow.canvasRoot.Children.Clear();

                // 建立新的白方框，然後設定顏色
                Rectangle whiteBox = blackWindow.CreateCenteredWhiteBox();
                whiteBox.Fill = new SolidColorBrush(Color.FromRgb((byte)r, (byte)g, (byte)b));

                blackWindow.canvasRoot.Children.Add(whiteBox);
                blackWindow.whiteBox = whiteBox;
            }
        }

        private async Task<PatternRGB> FineTuneWhitePointToTargetAsync(double targetX, double targetY, CancellationToken token, int targetCT)
        {
            token.ThrowIfCancellationRequested();
            int r = 128, g = 128, b = 128;
            //int r = 255, g = 255, b = 255;//zh 250619
            double toleranceX = 0.0009;
            double toleranceY = 0.0009;

            bool rLock = false, gLock = false;
            bool checkX = true, checkY = false;

            int[] gLevel = { 0, 0 }, gCnt = { 0, 0 };
            int[] bLevel = { 0, 0 }, bCnt = { 0, 0 };

            //     int maxIterations = 100;  ORG SETTING
            int maxIterations = 550;//zh 250618  為了加速時間

            var mainWin = (MainWindow)Application.Current.MainWindow;
            var blackWindow = Application.Current.Windows
                                  .OfType<BlackWindow>()
                                  .FirstOrDefault(w => w.IsVisible);


            blackWindow.textblockStatus.Text = "ColorCT-FineTuning @ " + targetCT;
            for (int i = 0; i < maxIterations; i++)
            {
                token.ThrowIfCancellationRequested();
                setPatternColor(r, g, b);
                await Task.Delay(100);

                bool ok = _sensor.MeasYxyz(out SensorMeasureYxy_t data);
                if (!ok)
                {
                    Console.WriteLine("[ColorCT-FineTune] 測量失敗，跳出");
                    break;
                }

                Console.WriteLine($"[ColorCT-FineTune] #{i:D3} x={data.x:F4}, y={data.y:F4}, Y={data.Y:F2}");
                UpdateUIAction?.Invoke(data);

                double dx = data.x - targetX;
                double dy = data.y - targetY;

                if (Math.Abs(dx) <= toleranceX) { checkX = false; checkY = true; gCnt[0] = gCnt[1] = 0; }
                else if (Math.Abs(dy) <= toleranceY || (gCnt[0] >= 3 && gCnt[1] >= 3))
                { checkX = true; checkY = false; }

                if (r == 255 && g == 255 && b == 255)
                {
                    if (dx > toleranceX) r--;
                    else if (dx < -toleranceX) { b--; rLock = true; }

                    if (dy > toleranceY) g--;
                    else if (dy < -toleranceY) { b--; gLock = true; }

                    checkX = true; checkY = false;
                }
                else if (dx > toleranceX && !rLock && checkX)
                {
                    r -= (Math.Abs(dx) > 0.005) ? 5 : 1;
                }
                else if (dx < -toleranceX && checkX)
                {
                    if (r < 255) r++;
                    else if (Math.Abs(dx) > 0.005) b -= 5;
                    else b--;
                }
                else if (dy > toleranceY && checkY)
                {
                    if (rLock)
                    {
                        if (gLock) gLock = false;

                        else if (Math.Abs(dy) > 0.005) g -= 5;
                        else
                        {
                            if (gLevel[0] == g) gCnt[0]++;
                            else if (gLevel[1] == g) gCnt[1]++;
                            else { gLevel[1] = gLevel[0]; gCnt[1] = gCnt[0]; gLevel[0] = g; gCnt[0] = 1; }
                            g--;
                        }
                    }
                    else if (b < 255) b++;
                    else g--;
                }
                else if (dy < -toleranceY && checkY)
                {
                    if (g < 255)
                    {
                        if (gLevel[0] == g) gCnt[0]++;
                        else if (gLevel[1] == g) gCnt[1]++;
                        else { gLevel[1] = gLevel[0]; gCnt[1] = gCnt[0]; gLevel[0] = g; gCnt[0] = 1; }
                        g++;
                    }
                    else
                    {
                        if (bLevel[0] == b) bCnt[0]++;
                        else if (bLevel[1] == b)
                        { bCnt[1]++; (bLevel[0], bLevel[1]) = (bLevel[1], bLevel[0]); (bCnt[0], bCnt[1]) = (bCnt[1], bCnt[0]); }
                        else { bLevel[1] = bLevel[0]; bCnt[1] = bCnt[0]; bLevel[0] = b; bCnt[0] = 1; }
                        b--;
                    }
                }
                else if (checkX)
                {
                    if (dx < -toleranceX)
                    {
                        if (r < 255) r++;
                        else b--;
                    }
                    else if (dx > toleranceX)
                    {
                        if (!rLock) r--;
                        else if (rLock && gLock)
                        {
                            if (g == 255) rLock = false;
                        }
                        else if (b < 255)
                        {
                            if (bLevel[0] == b) bCnt[0]++;
                            else if (bLevel[1] == b)
                            { bCnt[1]++; (bLevel[0], bLevel[1]) = (bLevel[1], bLevel[0]); (bCnt[0], bCnt[1]) = (bCnt[1], bCnt[0]); }
                            else { bLevel[1] = bLevel[0]; bCnt[1] = bCnt[0]; bLevel[0] = b; bCnt[0] = 1; }
                            b++;
                        }
                    }
                }
                else
                {
                    if (dy < -toleranceY)
                    {
                        if (g < 255) g++;
                        else b--;
                    }
                    else if (dy > toleranceY)
                    {
                        if (b < 255) b++;
                        else g--;
                    }
                }
                // Check range
                if ((Math.Abs(dx) <= toleranceX) && (Math.Abs(dy) <= toleranceY))
                {
                    Console.WriteLine($"[ColorCT-checkXY] i={i}, {checkX}, {checkY}");//zh 250618 add
                    Console.WriteLine($"[ColorCT-dXY] {Math.Abs(dx)}, {Math.Abs(dy)}");//zh 250618 add
                    break;
                }//zh 250618 add
            }


            Console.WriteLine($"[ColorCT-FineTune]  最終 RGB = ({r}, {g}, {b})");
            return new PatternRGB(r, g, b);
        }

        private async Task PopulateTargetList(CalibrationProtocol protocol, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            var mainWin = (MainWindow)Application.Current.MainWindow;
            var calibrationPage = mainWin.Calibration.Content as Calibration;


            var controller = mainWindow?.GetI2CController();
            if (controller == null)
            {
                Console.WriteLine("錯誤：無法取得 i2cController");
                return;
            }


            // 顯示原始支援字串
            Console.WriteLine("Supported CTs: " + string.Join(", ", protocol.SupportColorTemp));
            Console.WriteLine("Supported Gammas: " + string.Join(", ", protocol.SupportGamma));

            // 轉成數值列表
            var supportedCCTs = protocol.SupportColorTemp
                .Select(s => int.TryParse(s.Replace("K", ""), out var x) ? x : (int?)null)
                .Where(x => x.HasValue).Select(x => x.Value).ToList();

            // 轉成 Gamma 數值，並保留 DICOM
            var supportedGammas = new List<double>();
            foreach (var g in protocol.SupportGamma)
            {
                if (double.TryParse(g, out var val))
                {
                    supportedGammas.Add(val);
                }
                else if (string.Equals(g, "DICOM", StringComparison.OrdinalIgnoreCase))
                {
                    supportedGammas.Add(double.NaN);
                }
            }

            // 填 targetList 並印
            targetList.Clear();
            Console.WriteLine("=== 將要執行的校正項目 ===");

            if (!supportedGammas.Any())
            {
                foreach (var cct in supportedCCTs)
                {
                    Console.WriteLine($"  ‧ ColorTemp @ {cct}K");
                    // 這裡用 gamma = NaN 代表「純色溫」
                    targetList.Add(new CalibTarget(double.NaN, cct));
                }
            }


            foreach (var cct in supportedCCTs)
            {
                foreach (var gamma in supportedGammas)
                {
                    // gamma == NaN 代表 DICOM
                    if (double.IsNaN(gamma))
                        Console.WriteLine($"  ‧ DICOM @ {cct}K");
                    else
                        Console.WriteLine($"  ‧ Gamma {gamma:F2} @ {cct}K");

                    targetList.Add(new CalibTarget(gamma, cct));
                }
            }
            Console.WriteLine($"總共 {targetList.Count} 組，接下來將依序執行它們的校正流程。\n");
        }


        public void ResetCalibrationItems()
        {
            var mainWin = (MainWindow)Application.Current.MainWindow;
            var calibrationPage = mainWin.Calibration.Content as Calibration;


            calibrationPage.labelContrast.Content = "Contrast";
            calibrationPage.labelContrastJudge.Content = "N/A";
            calibrationPage.textBoxContrast.Text = "N/A";

            calibrationPage.labelUniformity.Content = "Uniformity";
            calibrationPage.labelUniformityJudge.Content = "N/A";
            calibrationPage.textBoxUniformity.Text = "N/A";

            calibrationPage.labelColorTemp1.Content = "Color Temp 1";
            calibrationPage.labelColorTemp1Judge.Content = "N/A";
            calibrationPage.textBoxColorTemp1.Text = "N/A";

            calibrationPage.labelColorTemp2.Content = "Color Temp 2";
            calibrationPage.labelColorTemp2Judge.Content = "N/A";
            calibrationPage.textBoxColorTemp2.Text = "N/A";

            calibrationPage.labelColorTemp3.Content = "Color Temp 3";
            calibrationPage.labelColorTemp3Judge.Content = "N/A";
            calibrationPage.textBoxColorTemp3.Text = "N/A";

            calibrationPage.labelGamma1.Content = "Gamma 1";
            calibrationPage.labelGamma1Judge.Content = "N/A";
            calibrationPage.textBoxGamma1.Text = "N/A";

            calibrationPage.labelGamma2.Content = "Gamma 2";
            calibrationPage.labelGamma2Judge.Content = "N/A";
            calibrationPage.textBoxGamma2.Text = "N/A";

            calibrationPage.labelGamma3.Content = "Gamma 3";
            calibrationPage.labelGamma3Judge.Content = "N/A";
            calibrationPage.textBoxGamma3.Text = "N/A";

            calibrationPage.labelGamma4.Content = "Gamma 4";
            calibrationPage.labelGamma4Judge.Content = "N/A";
            calibrationPage.textBoxGamma4.Text = "N/A";

            calibrationPage.labelDICOM.Content = "DICOM";
            calibrationPage.labelDICOMJudge.Content = "N/A";
            calibrationPage.textBoxDICOM.Text = "N/A";
        }
    }
}
