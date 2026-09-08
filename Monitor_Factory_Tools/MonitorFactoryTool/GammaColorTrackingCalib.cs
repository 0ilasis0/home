
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FDTI_Factory_i2c;
using Microsoft.Office.Interop.Excel;
using MonitorFactoryTool;
using MonitorFactoryTool.ONYX.Utilities;
using MonitorFactoryTool.Pages;
using ONYX.Utilities;

namespace MonitorFactoryTool
{
    public class GammaColorTrackingCalib
    {
        //>>wu add 250707
        private readonly int _entryCount;
        private readonly int _lutResolution;

        public GammaColorTrackingCalib(int entryCount, int lutResolution)
        {
            _entryCount = entryCount;
            _lutResolution = lutResolution;
        }
        //<<wu add 250707

        private Dictionary<(int cct, double gamma), LUTData> _lutTable = new Dictionary<(int, double), LUTData>(); //250610 wu add 為了解決燒錄的時候lut 沒抓到色溫配gamma 的table 

        private static readonly Dictionary<string, int> _fileIndexMap = new Dictionary<string, int>(); //250612 wu add PrintLUTAsHexText()裡面用來記錄輸出文件的


        public Task PrintLUTAsHexText(int cct, double gamma)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            var controller = mainWindow?.GetI2CController();
            var protocol = new CalibrationProtocol(controller);

            // 如果不是 debug 模式，就直接跳開，不會產生LUT txt 
            if (mainWindow == null || !mainWindow.IsDebugMode)
                return Task.CompletedTask;

            Console.WriteLine("gamma in PrintLUTAsHexText  fumction: " + gamma);

            // 251201 wu modify 只負責把算好的 LUT輸出成 txt，不要再下指令 get
            if (!_lutTable.TryGetValue((cct, gamma), out var lut))
            {
                Console.WriteLine($"[Error] LUT for {cct}K, gamma={gamma:F2} not found in _lutTable.");
                return Task.CompletedTask;
            }

            string[] channelNames = { "R", "G", "B" };
            int[][] channels = { lut.RLUT, lut.GLUT, lut.BLUT };

            // 251201 wu modify 不要用 ctCode/gmCode 當 key，現在用 CCT + Gamma 字串來管理 index
            string gmKey = double.IsNaN(gamma) ? "DICOM" : gamma.ToString("F1");
            string keyIndex = $"{cct}_{gmKey}";

            _fileIndexMap.TryGetValue(keyIndex, out int idx);
            _fileIndexMap[keyIndex] = idx + 1;

            string fileName = $"GammaLUT_CT{cct}_GAMMA{(int)(gamma * 10)}_{idx}.txt";
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            using (StreamWriter writer = new StreamWriter(fullPath, append: false))
            {
                writer.WriteLine($"// Gamma LUT for {cct}K, Gamma = {gamma:F2}");
                for (int ch = 0; ch < 3; ch++)
                {
                    writer.WriteLine($"code BYTE tblPostGamma{(int)(gamma * 10)}_{channelNames[ch]}_{cct}[] =");
                    writer.WriteLine("{");

                    int[] data = channels[ch];
                    for (int i = 0; i < data.Length; i++)
                    {
                        ushort val = (ushort)data[i];
                        writer.Write($"0x{val & 0xFF:X2},0x{(val >> 8) & 0xFF:X2}");

                        if (i < data.Length - 1)
                            writer.Write(", ");

                        if ((i + 1) % 8 == 0)
                            writer.WriteLine();
                    }

                    writer.WriteLine("};\n");
                }
            }

            Console.WriteLine($"[Gamma LUT]  已經儲存到 {fullPath} 裡面的 {fileName}");

            // 檢查 _lutTable 的 Key  (int cct, double gamma) 有哪些
            foreach (var key in _lutTable.Keys)
            {
                Console.WriteLine($"LUTTable Key => CCT: {key.cct}K, Gamma: {key.gamma:F2}");
            }
            return Task.CompletedTask;

        }
        public async Task PrintDicomLUTAsHexText(int cct, double JND_Scale)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            var controller = mainWindow?.GetI2CController();
            var protocol = new CalibrationProtocol(controller);

            // 如果不是 debug 模式，就直接跳開，不會產生LUT txt 
            if (mainWindow == null || !mainWindow.IsDebugMode)
                return;

            Console.WriteLine($"[DICOM LUT] JND_Scale = {JND_Scale:F1}");

            await protocol.GetColoetempDetailAndGammaDetail();

            if (!_lutTable.ContainsKey((cct, JND_Scale)))
            {
                Console.WriteLine($"[DICOM LUT Error] no LUT for {cct}K, scale={JND_Scale:F1}");
                return;
            }

            LUTData lut = _lutTable[(cct, JND_Scale)];
            string[] channelNames = { "R", "G", "B" };
            int[][] channels = { lut.RLUT, lut.GLUT, lut.BLUT };


            var dicomKey = $"{cct}_DICOM{(int)(JND_Scale * 10)}";
            _fileIndexMap.TryGetValue(dicomKey, out int dicomIdx);
            _fileIndexMap[dicomKey] = dicomIdx + 1;
            string fileName = $"DICOMLUT__CT{cct}_{(int)(JND_Scale * 10)}_{dicomIdx}.txt";


            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            using (StreamWriter writer = new StreamWriter(fullPath, append: false))
            {
                writer.WriteLine($"// DICOM LUT for {cct}K, JND_Scale = {JND_Scale:F2}");
                for (int ch = 0; ch < 3; ch++)
                {
                    writer.WriteLine($"code BYTE tblPostGamma{(int)(JND_Scale * 10)}_{channelNames[ch]}_{cct}[] =");
                    writer.WriteLine("{");

                    int[] data = channels[ch];
                    for (int i = 0; i < data.Length; i++)
                    {
                        ushort val = (ushort)data[i];
                        writer.Write($"0x{val & 0xFF:X2},0x{(val >> 8) & 0xFF:X2}");

                        if (i < data.Length - 1)
                            writer.Write(", ");

                        if ((i + 1) % 8 == 0)
                            writer.WriteLine();
                    }

                    writer.WriteLine("};\n");
                }
            }
            Console.WriteLine($"[DICOM LUT]  已經儲存到 {fullPath} 裡面的 {fileName}");
        }


        public LUTData GetLUTData(int cct, double gamma)
        {
            if (_lutTable.TryGetValue((cct, gamma), out var lut))
                return lut;
            throw new ArgumentException($"LUT 尚未產生: {cct}K, γ={gamma:F2}");
        }
        public LUTData GetDICOMLUTData(int cct, double jnd_scale)
        {
            if (_lutTable.TryGetValue((cct, jnd_scale), out var dicom_lut))
                return dicom_lut;
            throw new ArgumentException($"LUT 尚未產生: {cct}K, jnd_scale={jnd_scale:F2}");
        }



        public void GenerateLUTFromMeasuredY(
    int cct,
    List<double> measuredY,                      //  使用 32 階灰階量測值
    double[] redLum, double[] greenLum, double[] blueLum, // 16384 長度補點曲線
    ColorVector colorVector,                     //  用來分配 RGB luminance ratio
    double targetGamma
            )
        {
            double marginGamma = targetGamma + 0.00; //// getGammaStandard 手動加上0.05 避免gamma校正出來過低

            //>>wu add 250707
            int LUT_ENTRY_COUNT = _entryCount;
            int LUT_RESOLUTION = _lutResolution;
            //<<wu add 250707
            double minLum_phys = measuredY[0];
            double maxLum_phys = measuredY[measuredY.Count - 1];
            double[] standardLum_absolute = GammaStandard.getGammaStandard(minLum_phys, maxLum_phys, marginGamma, (uint)LUT_ENTRY_COUNT);

            Console.WriteLine("targetGamma in GenerateLUTFromMeasuredY  fumction: " + targetGamma, ":   marginGamma: " + marginGamma);

            LUTData lut = new LUTData((uint)LUT_ENTRY_COUNT, (uint)LUT_RESOLUTION);
            int[] rGamma = new int[LUT_ENTRY_COUNT];
            int[] gGamma = new int[LUT_ENTRY_COUNT];
            int[] bGamma = new int[LUT_ENTRY_COUNT];

            int rLastLevel = 0, gLastLevel = 0, bLastLevel = 0;

            for (int i = 0; i < LUT_ENTRY_COUNT; i++)
            {
                double Y_target_total_absolute = standardLum_absolute[i]; // 期望的總絕對輝度 (含黑點)


                double rTarget_channel_absolute = Y_target_total_absolute * colorVector.rVector;
                double gTarget_channel_absolute = Y_target_total_absolute * colorVector.gVector;
                double bTarget_channel_absolute = Y_target_total_absolute * colorVector.bVector;


                rLastLevel = GammaStandard.getBestLevel(rLastLevel, rTarget_channel_absolute, redLum);
                gLastLevel = GammaStandard.getBestLevel(gLastLevel, gTarget_channel_absolute, greenLum);
                bLastLevel = GammaStandard.getBestLevel(bLastLevel, bTarget_channel_absolute, blueLum);

                rGamma[i] = rLastLevel;
                gGamma[i] = gLastLevel;
                bGamma[i] = bLastLevel;

                lut.SetLUTData(0, i, rGamma[i]);
                lut.SetLUTData(1, i, gGamma[i]);
                lut.SetLUTData(2, i, bGamma[i]);
            }
            _lutTable[(cct, targetGamma)] = lut;


        }
        public void GenerateLUTFromMeasuredY_DICOM(
    int cct,
    List<double> measuredY,                      //  使用 32 階灰階量測值
    double[] redLum, double[] greenLum, double[] blueLum, //  16384 長度補點曲線
    ColorVector colorVector,                     //  用來分配 RGB luminance ratio
    double JND_Scale
            )
        {
            //>>wu add 250707
            int LUT_ENTRY_COUNT = _entryCount;
            int LUT_RESOLUTION = _lutResolution;
            //<<wu add 250707
            for (int i = 0; i < 32; i++)
                Console.WriteLine($"measuredY[{i}]  {measuredY[i]}");
            double minLum_phys = measuredY[0];
            double maxLum_phys = measuredY[measuredY.Count - 1];
            Console.WriteLine("[GenerateLUTFromMeasuredY]  MIN LUM" + minLum_phys + "MAX LUM" + maxLum_phys);

            double[] standardLum_absolute = GammaStandard.getDICOMStandard(minLum_phys, maxLum_phys, JND_Scale, (uint)LUT_ENTRY_COUNT);

            LUTData lut = new LUTData((uint)LUT_ENTRY_COUNT, (uint)LUT_RESOLUTION);
            int[] rGamma = new int[LUT_ENTRY_COUNT];
            int[] gGamma = new int[LUT_ENTRY_COUNT];
            int[] bGamma = new int[LUT_ENTRY_COUNT];

            int rLastLevel = 0, gLastLevel = 0, bLastLevel = 0;

            for (int i = 0; i < LUT_ENTRY_COUNT; i++)
            {
                //double Y_target_total_absolute = standardLum_absolute[i]; // 期望的總絕對輝度 (含黑點)
                double Y_target_total_absolute = standardLum_absolute[i] - minLum_phys; // 期望的總絕對輝度 (含黑點)//zh 250715


                double rTarget_channel_absolute = Y_target_total_absolute * colorVector.rVector;
                double gTarget_channel_absolute = Y_target_total_absolute * colorVector.gVector;
                double bTarget_channel_absolute = Y_target_total_absolute * colorVector.bVector;


                rLastLevel = GammaStandard.getBestLevel(rLastLevel, rTarget_channel_absolute, redLum);
                gLastLevel = GammaStandard.getBestLevel(gLastLevel, gTarget_channel_absolute, greenLum);
                bLastLevel = GammaStandard.getBestLevel(bLastLevel, bTarget_channel_absolute, blueLum);

                rGamma[i] = rLastLevel;
                gGamma[i] = gLastLevel;
                bGamma[i] = bLastLevel;

                lut.SetLUTData(0, i, rGamma[i]);
                lut.SetLUTData(1, i, gGamma[i]);
                lut.SetLUTData(2, i, bGamma[i]);
            }
            _lutTable[(cct, JND_Scale)] = lut;
        }
    }
}


