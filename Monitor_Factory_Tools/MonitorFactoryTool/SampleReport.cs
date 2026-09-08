
using Microsoft.Office.Interop.Excel;
using MonitorFactoryTool;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using static OfficeOpenXml.ExcelErrorValue;
using Excel = Microsoft.Office.Interop.Excel;

public class SampleReport
{
    private static SampleReport _instance;
    public static SampleReport Instance => _instance ?? (_instance = new SampleReport());

    private double contrastRatio = -1;
    private string contrastResult = "N/A";

    // Uniformity 10%   80%
    private double[] uniformityWhite = new double[9];  // 80%
    private double[] uniformityBlack = new double[9];  // 10%

    // for center all black
    private double centerBlackx;
    private double centerBlacky;
    private double centerBlackY;

    // for 100% brightness 
    private double[] uniformityMax_x = new double[9];  // 100%brightness's  x
    private double[] uniformityMax_y = new double[9];
    private double[] uniformityMax_Y = new double[9];
    private string uniformityMaxResult;

    private string uniformityWhiteResult = "N/A";
    private string uniformityBlackResult = "N/A";
    private double white_data;
    private double black_data;



    // Color Temp、Gamma、DICOM measure result 
    private List<MaxEntry> maxs = new List<MaxEntry>();

    private List<ColorTempEntry> colorTemps = new List<ColorTempEntry>();
    private List<GammaEntry> gammas = new List<GammaEntry>();
    private DICOMEntry dicom;
    private FullBrightnessEntry fullBrightness = new FullBrightnessEntry(); // 100% brightness

    private readonly string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"SampleVerificationRecord_example_0421.xlsx");


    public class FullBrightnessEntry
    {
        public double x, y, Y;
        public double[] UniformityData;
        public string Result;
        

        
        public FullBrightnessEntry()
        {
            UniformityData = new double[9];
        }
    }

    public class ColorTempEntry
    {
        public int TargetCT;
        public double x, y, Y;
        public string Result;
        public string valuestr;
    }

    public class MaxEntry
    {
        public int TargetCT;
        public double x, y, Y;
        public string Result;
        public string valuestr;
        public double centerBlackx, centerBlacky, centerBlackY;
    }


    public class GammaEntry
    {
        public int ColorTemp; //260129 wu add for logging gamma 6500 in report
        public double Target, Measured;
        public string Result;
    }






    public class DICOMEntry
    {
        public double Target, Measured;
        public string Result;
    }

    // ========= Function to generate report =========
    public void SetContrast_report(double whiteY, double blackY)
    {
        if (blackY <= 0)
        {
            contrastRatio = 0;
            contrastResult = "FAIL";
        }
        else
        {
            contrastRatio = whiteY / blackY;
            contrastResult = contrastRatio >= 1000 ? "PASS" : "FAIL";
        }
    }


    public void SetUniformity_report(double[] white, string whiteResult, double[] black, string blackResult, double white_h, double black_h)
    {
        if (white.Length == 9 && black.Length == 9)
        {
            Array.Copy(white, uniformityWhite, 9);
            Array.Copy(black, uniformityBlack, 9);
            uniformityWhiteResult = whiteResult;
            uniformityBlackResult = blackResult;
            white_data = white_h;
            black_data = black_h;
        }
        else
        {
            MessageBox.Show("Uniformity data count error.");
        }
    }

    // 100% brightness use
    public void SetMax_report(double[] max_x, double[] max_y, double[] max_Y,
                              double centerX = 0, double centerY = 0, double centerLum = 0,
                              string maxResult = "")
    {
        if (max_x.Length == 9 && max_y.Length == 9 && max_Y.Length == 9)
        {
            Array.Copy(max_x, uniformityMax_x, 9);
            Array.Copy(max_y, uniformityMax_y, 9);
            Array.Copy(max_Y, uniformityMax_Y, 9);

            this.centerBlackx = centerX;
            this.centerBlacky = centerY;
            this.centerBlackY = centerLum;
        }
        else
        {
            MessageBox.Show("Max brightness data count error.");
        }
    }


    public void SetColorTemp_report(int targetCT, double x, double y, double Y, string result, string valuestr)
    {
        colorTemps.Add(new ColorTempEntry { TargetCT = targetCT, x = x, y = y, Y = Y, Result = result, valuestr = valuestr });
    }

    public void SetGamma_report(int currentCT, double target, double measured, string result)
    {
        // 儲存時把色溫也存進去
        gammas.Add(new GammaEntry { ColorTemp = currentCT, Target = target, Measured = measured, Result = result });
    }

    public void SetDICOM_report(int target, double measured, string result)
    {
        dicom = new DICOMEntry { Target = target, Measured = measured, Result = result };
    }

    // ========= export Excel =========
    public void Export(string filePath)
    {
        var excelApp = new Excel.Application();
        Excel.Workbook workbook = null;
        var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        try
        {
            workbook = excelApp.Workbooks.Open(templatePath);
            Excel.Worksheet sheet = workbook.Worksheets[1];

            sheet.Range["W7"].Value = (DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));


            sheet.Range["E5"].Value = mainWindow.textBoxModelSerialNumber.Text;


            sheet.Range["E8"].Value = mainWindow.textBoxModelFirmwareVersion.Text;





            // 1. Contrast
            sheet.Range["H31"].Value = contrastRatio;
            sheet.Range["J18"].Value = contrastRatio;
            sheet.Range["K31"].Value = contrastResult;


            ///// 2. Uniformity 
            int[] requiredPoints = { 0, 2, 4, 6, 8 };
            string[] pointNames = { "Up-Left Corner", "Up-Right Corner", "Center", "Down-Left Corner", "Down-Right Corner" };

            // 5 points 
            for (int i = 0; i < requiredPoints.Length; i++)
            {
                int pointIndex = requiredPoints[i];

                sheet.Cells[12 + i, 10].Value = uniformityMax_x[pointIndex]; // x
                sheet.Cells[12 + i, 15].Value = uniformityMax_y[pointIndex]; // y
                sheet.Cells[12 + i, 20].Value = uniformityMax_Y[pointIndex]; // Y
            }

            // Center(Black - RGB(0,0,0) 
            sheet.Cells[17, 10].Value = centerBlackx; // center all black RGB (0,0,0) x
            sheet.Cells[17, 15].Value = centerBlacky;
            sheet.Cells[17, 20].Value = centerBlackY;

            // 2. Uniformity
            for (int i = 0; i < 9; i++)
            {
                sheet.Cells[20, 7 + i * 2].Value = uniformityBlack[i];  //10%
                sheet.Cells[21, 7 + i * 2].Value = uniformityWhite[i];  //80%
            }
            sheet.Range["Y20"].Value = black_data;
            sheet.Range["Y21"].Value = white_data;


            if (white_data > black_data)
            {
                sheet.Range["H32"].Value = white_data;
            }
            else
            {
                sheet.Range["H32"].Value = black_data;
            }

            if ((uniformityWhiteResult.Contains("FAIL") | (uniformityBlackResult.Contains("FAIL"))))
            {
                sheet.Range["K32"].Value = "FAIL";
            }
            else
            {
                sheet.Range["K32"].Value = "PASS";
            }


            // 3. Color Temp
            for (int i = 0; i < colorTemps.Count && i < 3; i++)
            {
                var ct = colorTemps[i];

                sheet.Cells[33 + i, 1].Value = "Color Temp " + $"{ct.TargetCT}K";

                sheet.Cells[23 + i, 5].Value = $"{ct.TargetCT}K";
                sheet.Cells[23 + i, 13].Value = ct.x;
                sheet.Cells[23 + i, 17].Value = ct.y;
                sheet.Cells[23 + i, 21].Value = ct.Y;
                sheet.Cells[23 + i, 25].Value = ct.Result ?? "N/A";

                sheet.Cells[33 + i, 8].Value = ct.valuestr;
                sheet.Cells[33 + i, 11].Value = ct.Result ?? "N/A";
            }


            double[] targetValues = { 1.8, 2.0, 2.2, 2.6 }; //可能會出現的目標gamma
            int targetCT = 6500;   //要log 的gamma 色溫目標值 

            // 4. Gamma
            for (int i = 0; i < gammas.Count && i < 4; i++)
            {
                double targetG = targetValues[i];

                var g = gammas.FirstOrDefault(entry =>
                               Math.Abs(entry.Target - targetG) < 0.01 &&
                               entry.ColorTemp == targetCT);

                // Excel 欄位位置 s31開始
                int rowIndex = 31 + i;

                if (g != null)
                {
                    sheet.Cells[rowIndex, 14].Value = "Gamma " + $"{g.Target:F1}";
                    sheet.Cells[rowIndex, 19].Value = g.Measured.ToString("F4");
                    sheet.Cells[rowIndex, 22].Value = g.Result ?? "N/A";
                }
                else
                {
                    // 如果這組 Gamma 沒測到 (例如只測了 1.8，沒測 2.6)，或是沒測就顯示 N/A                 
                    sheet.Cells[rowIndex, 14].Value = "Gamma " + $"{targetG}:F1";
                    sheet.Cells[rowIndex, 19].Value = "N/A";
                    sheet.Cells[rowIndex, 22].Value = "N/A";
                }
            }

            // 5. DICOM
            if (dicom != null)
            {
                sheet.Cells[35, 14].Value = "DICOM " + $"{dicom.Target}";


                sheet.Cells[35, 19].Value = dicom.Measured.ToString("F4") + $"%";
                sheet.Cells[35, 22].Value = dicom.Result ?? "N/A";
            }

            workbook.SaveAs(filePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Export failed: " + ex.Message);
        }
        finally
        {
            workbook?.Close(false);
            excelApp.Quit();
            Marshal.ReleaseComObject(excelApp);
        }
    }
}
