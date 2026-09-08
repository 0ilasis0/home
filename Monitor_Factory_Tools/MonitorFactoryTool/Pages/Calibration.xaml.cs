using MonitorFactoryTool.Pages;
using ONYX_DataType;
using OnyxSensor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
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
using Excel = Microsoft.Office.Interop.Excel;

using WinForms = System.Windows.Forms;

namespace MonitorFactoryTool.Pages
{
    /// <summary>
    /// Calibration.xaml 的互動邏輯
    /// </summary>
    public partial class Calibration : Page
    {

        private CalibProcess calibprocess;



        private SampleReport sampleReport = new SampleReport();
        public Calibration()
        {
            InitializeComponent();
            UpdateDateTime();



            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;


            if (mainWindow != null && mainWindow.IsDebugMode)
            {
                buttonCalibration.IsEnabled = true;
                System.Diagnostics.Trace.WriteLine("=== Calibration Page: Debug Mode Unlocked ===");
            }
            else
            {
                buttonCalibration.IsEnabled = false;
            }
        }



        private void buttonListExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string excelPath = "Calibtation_list_report_test_1.xlsx";
                //string excel_output_FilePath = "Gamma_report.xlsx";//zh add
                Excel.Application excelApp = new Excel.Application();
                Excel.Workbook workbook = excelApp.Workbooks.Add();
                Excel.Worksheet worksheet = workbook.Sheets[1];
                worksheet.Name = "Calibration";

                int row = 2;

                foreach (var item in listBoxCalibration.Items)
                {
                    if (item is Grid grid && grid.Children.Count == 4)
                    {
                        string level = ((TextBlock)grid.Children[0])?.Text ?? "";
                        string x = ((TextBlock)grid.Children[1])?.Text ?? "";
                        string y = ((TextBlock)grid.Children[2])?.Text ?? "";
                        string Y = ((TextBlock)grid.Children[3])?.Text ?? "";
                        worksheet.Cells[row, 1] = level;
                        worksheet.Cells[row, 2] = x;
                        worksheet.Cells[row, 3] = y;
                        worksheet.Cells[row, 4] = Y;

                        row++;
                    }
                }

                workbook.SaveAs(Environment.CurrentDirectory + excelPath);
                workbook.Close();
                excelApp.Quit();

                // 釋放 COM
                System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(excelApp);

                MessageBox.Show("Calibration log exported to Excel successfully!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void buttonVerify_Click(object sender, RoutedEventArgs e)
        {
            DoVerifyProcess(createNewWindow: true);  // 👈 從這裡呼叫
        }
        public void DoVerifyProcess(bool createNewWindow = true)
        {
            //Record time when verify
            labelDateContent.Content = DateTime.Now.ToString("yyyy/M/d HH:mm");

            // 單純verify 的話 createNewWindow == true，才顯示 SelectMonitorForm 並開 BlackWindow
            if (createNewWindow)
            {

                // SelectMonitorForm（WinForms）
                var selectForm = new Measure_Tool.SelectMonitorForm();

                // Setting setectedScreen
                if (selectForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var selectedScreen = selectForm.SelectedScreen;
                    var selectedSize = selectForm.SelectedScreenSize;

                    //System.Windows.MessageBox.Show($"你選擇了螢幕：{selectedScreen.DeviceName}\n尺寸：{selectedSize}\"");

                    //// 建立並顯示 BlackWindow 在選擇的螢幕上
                    var blackWindow = new BlackWindow(selectedScreen, selectedSize, BlackWindowSource.Verification);
                    blackWindow.Show();

                }
                else
                {
                    System.Windows.MessageBox.Show("取消選擇螢幕");
                }
            }
        }




        private void UpdateDateTime()
        {
            labelDateContent.Content = DateTime.Now.ToString("yyyy/M/d HH:mm");
        }
        public void SetContrast(string Judgecontrast, string DataContrast)
        {
            labelContrastJudge.Content = Judgecontrast;
            textBoxContrast.Text = DataContrast;
        }

        public void SetUniformity(string JudgeUniformity, string DataUniformity)
        {
            labelUniformityJudge.Content = JudgeUniformity;
            textBoxUniformity.Text = DataUniformity;
        }

        public void SetColorTemp(int index, string targetColorTempString, string cct)
        {
            switch (index)
            {
                case 0: labelColorTemp1.Content = "Color Temp " + targetColorTempString; textBoxColorTemp1.Text = cct; break;
                case 1: labelColorTemp2.Content = "Color Temp " + targetColorTempString; textBoxColorTemp2.Text = cct; break;
                case 2: labelColorTemp3.Content = "Color Temp " + targetColorTempString; textBoxColorTemp3.Text = cct; break;
                default:
                    throw new ArgumentException("Invalid ColorTemp index: must be 1, 2, or 3.");
            }
        }


        public void SetDICOM(string targetDICOMCTString, string DICOMResult, string DICOMAVG)
        {
            if (targetDICOMCTString != "6500")
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                labelDICOM.Content = "DICOM " + targetDICOMCTString;
                labelDICOMJudge.Content = DICOMResult;
                textBoxDICOM.Text = DICOMAVG + "%";
            });
        }


        public void SetGamma(int index, string target, string result)
        {
            switch (index)
            {
                case 0: labelGamma1.Content = "Gamma " + target; textBoxGamma1.Text = result; break;
                case 1: labelGamma2.Content = "Gamma " + target; textBoxGamma2.Text = result; break;
                case 2: labelGamma3.Content = "Gamma " + target; textBoxGamma3.Text = result; break;
                case 3: labelGamma4.Content = "Gamma " + target; textBoxGamma4.Text = result; break;
            }
        }


        ///Log data in Calibration's listboxs
        public void AddCalibrationLog(string col1, string col2, string col3, string col4)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(170) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(150) });


            var text1 = new TextBlock() { Text = col1, VerticalAlignment = VerticalAlignment.Center };
            var text2 = new TextBlock() { Text = col2, VerticalAlignment = VerticalAlignment.Center };
            var text3 = new TextBlock() { Text = col3, VerticalAlignment = VerticalAlignment.Center };
            var text4 = new TextBlock() { Text = col4, VerticalAlignment = VerticalAlignment.Center };


            Grid.SetColumn(text1, 0);
            Grid.SetColumn(text2, 1);
            Grid.SetColumn(text3, 2);
            Grid.SetColumn(text4, 3);


            grid.Children.Add(text1);
            grid.Children.Add(text2);
            grid.Children.Add(text3);
            grid.Children.Add(text4);

            listBoxCalibration.Items.Add(grid);
            listBoxCalibration.ScrollIntoView(listBoxCalibration.Items[listBoxCalibration.Items.Count - 1]);
        }

        private void buttonCALSampleVerificationExport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "匯出驗證報告",
                FileName = "Sample_Report_test",
                DefaultExt = ".xlsx",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;

                // 確保副檔名為 .xlsx
                if (!filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    filePath += ".xlsx";
                }

                try
                {

                    SampleReport.Instance.Export(filePath);
                    MessageBox.Show("報告匯出完成！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("匯出失敗：" + ex.Message, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        private void buttonCalibration_Click(object sender, RoutedEventArgs e)
        {   // SelectMonitorForm（WinForms）
            var selectForm = new Measure_Tool.SelectMonitorForm();
            // Setting setectedScreen
            if (selectForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var selectedScreen = selectForm.SelectedScreen;
                var selectedSize = selectForm.SelectedScreenSize;

                //System.Windows.MessageBox.Show($"你選擇了螢幕：{selectedScreen.DeviceName}\n尺寸：{selectedSize}\"");

                // 建立並顯示 BlackWindow 在選擇的螢幕上
                var blackWindow = new BlackWindow(selectedScreen, selectedSize, BlackWindowSource.Calibration);
                blackWindow.Show();


            }
            else
            {
                System.Windows.MessageBox.Show("取消選擇螢幕");
            }
        }
    }
}
