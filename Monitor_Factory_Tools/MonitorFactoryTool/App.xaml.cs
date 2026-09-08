using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MonitorFactoryTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //zh 250701>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //找 MonitorFactoryTool.exe 後面有沒有 -Ddebug 
            string s = string.Empty;
            for (int i = 0; i < e.Args.Length; i++)
            {
                s += e.Args[i];
            }

            MainWindow mainWindow = new MainWindow(s);


            if (s.Equals("-Ddebug"))
            {


                mainWindow.Title = mainWindow.Title + " Debug";
                try
                {
                    // Log 檔案路徑
                    string logPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Log.txt");


                    // 若檔案已存在就先刪除
                    if (File.Exists(logPath))
                    {
                        File.Delete(logPath);
                    }



                    // 建立 FileStream for Debug  writeline 用的
                    var fsDebug = new FileStream(
                        logPath,
                  FileMode.Create,
                        FileAccess.Write,
                        FileShare.ReadWrite);

                    // 清除系統預設的 Debug Listener，改為輸出到檔案的 Listener
                    Debug.Listeners.Clear();
                    Debug.Listeners.Add(new TextWriterTraceListener(fsDebug));
                    Debug.AutoFlush = true;  // 每次 WriteLine 之後就自動刷新

                    // 建立另一個 FileStream for Console  writeline 用的
                    var fsConsole = new FileStream(
                        logPath,
                FileMode.Create,
                        FileAccess.Write,
                        FileShare.ReadWrite);
                    var sw = new StreamWriter(fsConsole) { AutoFlush = true };
                    Console.SetOut(sw);
                    Console.SetError(sw);

                    Debug.WriteLine("==== Debug mode initialized ====");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"初始化 Debug 日誌失敗：{ex.Message}",
                        "Debug Init Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            // 顯示主視窗，啟動整個應用程式
            mainWindow.Show();
        }
    }
}