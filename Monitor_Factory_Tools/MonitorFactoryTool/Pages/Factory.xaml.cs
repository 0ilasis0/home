using MonitorFactoryTool.FactoryGrid;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
//using System.Reflection.Emit;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using static MonitorFactoryTool.MainWindow;
using static System.Net.Mime.MediaTypeNames;
using ONYX_DataType;
using System.IO.Ports;
using System.Threading;
using System.Security.Permissions;
using System.Net.Http;
using System.ComponentModel;
using FDTI_Factory_i2c;
using System.Xml.Serialization;
using System.Diagnostics.Eventing.Reader;

namespace MonitorFactoryTool.Pages
{
    public partial class Factory : Page
    {
        private I2CController i2cController;
        public static bool isMCCSmode = false;

        public Factory()
        {
            InitializeComponent();
            this.Loaded += Factory_Loaded;

            //260618 wu add for keyboard  event  to move the grid 
            this.Focusable = true;
            this.PreviewKeyDown += Factory_PreviewKeyDown;
            //260618 wu add for keyboard  event  to move the grid 

        }

        private void Factory_Loaded(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            i2cController = new I2CController(mainWindow.myFtdiDevice);

            //260618 wu add for keyboard  event  to move the grid 
            Keyboard.Focus(this);
            //260618 wu add for keyboard  event  to move the grid 

            buttonSet.IsEnabled = false;
            buttonGet.IsEnabled = false;
            foreach (Grid item in CommandList.Children)
            {
                if (item.Children.Count == 1)
                {
                    buttonLoop.IsEnabled = true;
                    break;
                }
                else
                {
                    buttonLoop.IsEnabled = false;
                }
            }

            if (selectedGrid != null)
            {
                selectedGrid.Background = Brushes.Transparent;
                selectedGrid = null;
            }
        }
        private void SetComboBoxItemByValue(ComboBox comboBox, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;


            foreach (var item in comboBox.Items)
            {
                var itemContent = item.ToString();
                if (itemContent?.Trim() == value?.Trim())
                {
                    comboBox.SelectedItem = item;
                    //MessageBox.Show($"Set {comboBox.Name} to {value}");
                    break;
                }
            }
        }

        private void AddItemsToComboBox(ComboBox comboBox, string[] items)
        {
            if (items == null) return;

            comboBox.Items.Clear();
            foreach (var item in items)
            {
                comboBox.Items.Add(new ComboBoxItem { Content = item });
            }
        }

        private void SetComboBoxSelectedItem(ComboBox comboBox, string savedValue)
        {
            if (string.IsNullOrEmpty(savedValue)) return;

            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem && comboBoxItem.Content.ToString() == savedValue)
                {
                    comboBox.SelectedItem = comboBoxItem;
                    break;
                }
            }
        }

        ///##################################################################################################################################
        //###################################################################################################################################
        //##################                                 data    processing                                         #####################
        //###################################################################################################################################
        //###################################################################################################################################



        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }
                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        ///##################################################################################################################################
        //###################################################################################################################################
        //##################                                 Grid dynamic adjustment                                    #####################
        //###################################################################################################################################
        //###################################################################################################################################

        public Grid UpdateFactoryView(string command, string style, string length, string rlength, string mode, string vccode, string description, List<string> dataValues)//zh add
        {
            command = command.Trim();
            Debug.WriteLine($"Factory 頁面 接收的 command是 : {command}");

            ggridName = command;
            string gridIndexName = "gridIndex";

            Grid targetGrid = null; //260223 Wu add for save/load script


            if (selectedGrid != null)
            {
                gridIndexName = selectedGrid.Name;
                selectedGrid.Children.Clear();
            }
            else
            {
                for (int i = 0; i < CommandList.Children.Count; i++)
                {
                    Grid grid = CommandList.Children[i] as Grid;
                    if (grid.Children.Count == 0)
                    {
                        gridIndexName = gridIndexName + i.ToString();
                        break;
                    }
                    else if (i == CommandList.Children.Count - 1 && grid.Children.Count == 1)
                    {
                        MessageBox.Show("The command account is full");
                    }
                }
            }

            switch (gridIndexName) //gridIndex to do 
            {
                //colume 0
                case "gridIndex0":
                    DrawingGridStyle(ggridName, gridIndex0, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex0;
                    break;
                case "gridIndex1":
                    DrawingGridStyle(ggridName, gridIndex1, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex1;
                    break;
                case "gridIndex2":
                    DrawingGridStyle(ggridName, gridIndex2, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex2;
                    break;
                case "gridIndex3":
                    DrawingGridStyle(ggridName, gridIndex3, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex3;
                    break;
                case "gridIndex4":
                    DrawingGridStyle(ggridName, gridIndex4, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex4;
                    break;
                case "gridIndex5":
                    DrawingGridStyle(ggridName, gridIndex5, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex5;
                    break;
                case "gridIndex6":
                    DrawingGridStyle(ggridName, gridIndex6, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex6;
                    break;
                case "gridIndex7":
                    DrawingGridStyle(ggridName, gridIndex7, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex7;
                    break;
                case "gridIndex8":
                    DrawingGridStyle(ggridName, gridIndex8, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex8;
                    break;
                case "gridIndex9":
                    DrawingGridStyle(ggridName, gridIndex9, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex9;
                    break;

                //colume 1
                case "gridIndex10":
                    DrawingGridStyle(ggridName, gridIndex10, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex10;
                    break;
                case "gridIndex11":
                    DrawingGridStyle(ggridName, gridIndex11, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex11;
                    break;
                case "gridIndex12":
                    DrawingGridStyle(ggridName, gridIndex12, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex12;
                    break;
                case "gridIndex13":
                    DrawingGridStyle(ggridName, gridIndex13, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex13;
                    break;
                case "gridIndex14":
                    DrawingGridStyle(ggridName, gridIndex14, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex14;
                    break;
                case "gridIndex15":
                    DrawingGridStyle(ggridName, gridIndex15, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex15;
                    break;
                case "gridIndex16":
                    DrawingGridStyle(ggridName, gridIndex16, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex16;
                    break;
                case "gridIndex17":
                    DrawingGridStyle(ggridName, gridIndex17, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex17;
                    break;
                case "gridIndex18":
                    DrawingGridStyle(ggridName, gridIndex18, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex18;
                    break;
                case "gridIndex19":
                    DrawingGridStyle(ggridName, gridIndex19, Convert.ToInt16(style), length, rlength, vccode, description, mode, dataValues);
                    targetGrid = gridIndex19;
                    break;
            }

            return targetGrid;
        }

        private void DrawingGridStyle(string gridName, Grid grid, int style, string length, string rlength, string vccode, string description, string type, List<string> dataValues)
        {
            if (style == 1)
            {
                FactoryGridStyle1 factoryGridStyle1 = new FactoryGridStyle1(gridName, type, vccode, description, length, rlength, dataValues);
                grid.Children.Add(factoryGridStyle1);
            }
            else if (style == 2)
            {
                FactoryGridStyle2 factoryGridStyle2 = new FactoryGridStyle2(gridName, type, vccode, description, length, rlength, dataValues);
                grid.Children.Add(factoryGridStyle2);
            }
            else if (style == 3)
            {
                FactoryGridStyle3 factoryGridStyle3 = new FactoryGridStyle3(gridName, type, vccode, description, length, rlength, dataValues);
                grid.Children.Add(factoryGridStyle3);
            }
            else if (style == 4)
            {
                FactoryGridStyle4 factoryGridStyle4 = new FactoryGridStyle4(gridName, type, vccode, description, length, rlength, dataValues);
                grid.Children.Add(factoryGridStyle4);
            }
        }

        ///##################################################################################################################################
        //###################################################################################################################################
        //##################                                 Mouse Down Event                                           #####################
        //###################################################################################################################################
        //###################################################################################################################################

        string ggridName = "";//zh for test
        private Grid selectedGrid; // 點選textbox所在的 Grid
        private List<string> secondLastByte;

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            //zh add
            Grid _grid = sender as Grid;

            // 滑鼠選到 Grid 後，把鍵盤拉回 Factory 頁面，
            Keyboard.Focus(this);



            int _row = (int)_grid.GetValue(Grid.RowProperty);
            int _column = (int)_grid.GetValue(Grid.ColumnProperty);
            string _gridName = _grid.Name;
            Debug.WriteLine($"{_gridName} grid name");
            Debug.WriteLine($"{_row} select row");
            Debug.WriteLine($"{_column} select colume");
            if (selectedGrid != null)   // There is a grid which has been selected 
            {
                if (selectedGrid.Name == _grid.Name) // If the same grid is selected 
                {
                    if (_grid.Background == System.Windows.Media.Brushes.Orange)
                    {
                        _grid.Background = System.Windows.Media.Brushes.Transparent;
                        selectedGrid = null;
                        mainWindow.listBoxCommand.Items.Clear();
                        setButtomButtonState("NULL");
                    }
                    else
                    {
                        selectedGrid = _grid;
                        _grid.Background = System.Windows.Media.Brushes.Orange;
                        mainWindow.listBoxCommand.Items.Clear();
                        if (_grid.Children.Count == 1)
                        {
                            BaseGridStyle baseGridStyle = _grid.Children[0] as BaseGridStyle;
                            SetTheTextToMainWindowListBoxCommand(baseGridStyle.GetDescription());
                            setButtomButtonState(baseGridStyle.GetType());
                        }
                    }
                }
                else
                {
                    selectedGrid.Background = System.Windows.Media.Brushes.Transparent;
                    selectedGrid = _grid;
                    _grid.Background = System.Windows.Media.Brushes.Orange;
                    mainWindow.listBoxCommand.Items.Clear();
                    if (_grid.Children.Count == 1)
                    {
                        BaseGridStyle baseGridStyle = _grid.Children[0] as BaseGridStyle;
                        SetTheTextToMainWindowListBoxCommand(baseGridStyle.GetDescription());
                        setButtomButtonState(baseGridStyle.GetType());
                    }
                }
                Debug.WriteLine($"{_grid.Name} is selected");

            }
            else    // There is no grid which has been selected
            {
                _grid.Background = System.Windows.Media.Brushes.Orange;
                Debug.WriteLine($"{_grid.Name} is selected");
                selectedGrid = _grid;
                mainWindow.listBoxCommand.Items.Clear();
                if (_grid.Children.Count == 1)
                {
                    BaseGridStyle baseGridStyle = _grid.Children[0] as BaseGridStyle;
                    SetTheTextToMainWindowListBoxCommand(baseGridStyle.GetDescription());
                    setButtomButtonState(baseGridStyle.GetType());
                }
            }
        }


        ///##################################################################################################################################
        //###################################################################################################################################
        //##################                                 bottom button                                              #####################
        //###################################################################################################################################
        //###################################################################################################################################

        private void setButtomButtonState(string commandType)
        {
            switch (commandType)
            {
                case "W/R":
                    buttonSet.IsEnabled = true;
                    buttonGet.IsEnabled = true;
                    break;
                case "W":
                    buttonSet.IsEnabled = true;
                    buttonGet.IsEnabled = false;
                    break;
                case "R":
                    buttonSet.IsEnabled = false;
                    buttonGet.IsEnabled = true;
                    break;
                case "NULL":
                    buttonSet.IsEnabled = false;
                    buttonGet.IsEnabled = false;
                    break;
                default:
                    buttonSet.IsEnabled = false;
                    buttonGet.IsEnabled = false;
                    break;
            }
        }

        private void buttonRemove_Click(object sender, RoutedEventArgs e)
        {
            if (selectedGrid != null)
            {
                selectedGrid.Children.Clear();
                selectedGrid.Tag = null;

                // If there is no command in grid, the loop button will be disabled 
                foreach (Grid item in CommandList.Children)
                {
                    if (item.Children.Count == 1)
                    {
                        buttonLoop.IsEnabled = true;
                        break;
                    }
                    else
                    {
                        buttonLoop.IsEnabled = false;
                    }
                }
                selectedGrid.Background = Brushes.Transparent;
                selectedGrid = null;
                buttonSet.IsEnabled = false;
                buttonGet.IsEnabled = false;
            }
            else
            {
                MessageBox.Show("No grid is selected.");
            }
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                // Read combobox items from mainWindow's combobox command list
                var selectedItem = mainWindow.comboCommand.SelectedItem as Item;
                if (selectedItem != null)
                {
                    // 接收回傳的 targetGrid 然後把 Item 存入 Tag
                    Grid targetGrid = UpdateFactoryView(selectedItem.Name, selectedItem.Style, selectedItem.Length, selectedItem.Rlength, selectedItem.Type, selectedItem.VCCode, selectedItem.Description, selectedItem.DataValues);
                    if (targetGrid != null)
                    {
                        targetGrid.Tag = selectedItem;
                    }
                }
            }
            // If there is any command in grid, the loop button will be enabled 
            foreach (Grid item in CommandList.Children)
            {
                if (item.Children.Count == 1)
                {
                    buttonLoop.IsEnabled = true;
                    break;
                }
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

        private void buttonSet_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            byte retval, retryCount = 0;//紀錄傳輸狀態

            if (selectedGrid != null)
            {
                if (selectedGrid.Children.Count == 1)
                {
                    BaseGridStyle gridStyle = selectedGrid.Children[0] as BaseGridStyle;
                    string tempCommandIsCksum = "";
                    foreach (byte b in gridStyle.SetCommandByteArray())
                    {
                        tempCommandIsCksum += b.ToString("X2");
                    }
                    byte cksum = check_sum(gridStyle.SetCommandByteArray());
                    tempCommandIsCksum += cksum.ToString("X2");
                    Debug.WriteLine("送出的 Set Command: " + tempCommandIsCksum + "\tUTF-8:" + System.Text.Encoding.UTF8.GetString(gridStyle.SetCommandByteArray()));
                    mainWindow.readDataLength = 9 + gridStyle.SetRlength(); // Reply command total byte = 9 (base rlength) + rlength (data rlength)

                    //判斷UART I2C send command 
                    if (mainWindow.comboModelPort.Text.Contains("COM"))
                        mainWindow.uartSendCommand(tempCommandIsCksum);
                    else if (mainWindow.comboModelPort.Text.Contains("I2C"))
                    {
                        do
                        {
                            retval = i2cController.DDCCI_Send_Command(gridStyle.SetCommandByteArray());
                            Debug.WriteLine($"ret val = {retval}, retryCount = {++retryCount}");
                        } while (retval != 0 && retryCount <= 10);
                    }
                    mainWindow.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        mainWindow.listBoxCommand.Items.Add(tempCommandIsCksum);
                    }));
                }
            }
        }

        private void buttonGet_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            if (selectedGrid != null)
            {
                if (selectedGrid.Children.Count == 1)
                {
                    BaseGridStyle gridStyle = selectedGrid.Children[0] as BaseGridStyle;
                    string tempCommandIsCksum = "";
                    foreach (byte b in gridStyle.GetCommandByteArray())
                    {
                        tempCommandIsCksum += b.ToString("X2");
                    }
                    byte cksum = check_sum(gridStyle.GetCommandByteArray());
                    tempCommandIsCksum += cksum.ToString("X2");
                    Debug.WriteLine("送出的 Get Command: " + tempCommandIsCksum + "\tUTF-8:" + System.Text.Encoding.UTF8.GetString(gridStyle.GetCommandByteArray()));
                    mainWindow.readDataLength = 9 + gridStyle.GetRlength();  // Reply command total byte = 9 (base rlength) + rlength (data rlength)
                    byte retval, retryCount = 0; //紀錄傳輸狀態

                    //判斷UART I2C send command 
                    if (mainWindow.comboModelPort.Text.Contains("COM"))
                    {
                        mainWindow.uartSendCommand(tempCommandIsCksum);
                    }
                    else if (mainWindow.comboModelPort.Text.Contains("I2C"))
                    {
                        do
                        {
                            retval = i2cController.DDCCI_Send_Command(gridStyle.GetCommandByteArray(), gridStyle.getReplyAndUpdateUI);
                            Debug.WriteLine($"ret val = {retval}, retryCount = {++retryCount}");  //紀錄傳輸狀態
                        } while (retval != 0 && retryCount <= 10);
                    }

                    mainWindow.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        mainWindow.listBoxCommand.Items.Add(tempCommandIsCksum);
                    }));
                }
            }
        }






        ///##################################################################################################################################
        //###################################################################################################################################
        //##################                                 Loop Function Processing                                   #####################
        //###################################################################################################################################
        //###################################################################################################################################

        // Loop task completion source
        public static TaskCompletionSource<bool> _receiveCompletionSource;
        public bool isAckE0;
        public byte[] replyCommandFromMainWindow;
        private bool LoopIsAuto;
        public event Action<bool> MCCSModeChanged;
        private int _runTimes = 0; //260127 wu add 

        private void UpdateRunTimesUI() //260127 wu add  
        {
            labelRunTimesCount.Content = _runTimes.ToString();
        }

        private async void buttonLoop_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            if (button.Content.ToString() == "Loop")
            {
                button.Content = "Stop Loop";
                LoopIsAuto = true;

                _runTimes = 0;
                labelRunTimesCount.Content = "0";
                UpdateRunTimesUI();

                // Crossing window to display information to listbox
                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                var selectedItem = mainWindow.comboCommand.SelectedItem as Item;
                if (selectedGrid != null)
                {
                    selectedGrid.Background = Brushes.Transparent;
                    selectedGrid = null;
                }
                mainWindow.listBoxCommand.Items.Clear();


                bool useRepeatCount = (clickRepeatCount.IsChecked == true);
                int targetCount = 0;

                // 打開 "times to run"，先驗證輸入合法
                if (useRepeatCount)
                {
                    if (!int.TryParse(textBoxRepeatCount.Text, out targetCount) || targetCount <= 0)
                    {
                        mainWindow.listBoxCommand.Items.Add("Error: Invalid repeat count (Must be > 0).");
                        button.Content = "Loop";
                        LoopIsAuto = false;
                        return;
                    }
                    mainWindow.listBoxCommand.Items.Add($"Mode: Run until {targetCount} times.");
                }
                else
                {
                    mainWindow.listBoxCommand.Items.Add("Mode: Infinite Loop.");
                }

                //  執行迴圈
                while (LoopIsAuto)
                {
                    mainWindow.listBoxCommand.Items.Add("Start command loop ......");


                    string delayTime = selectedItem != null ? selectedItem.Delay : "0";
                    await LOOPGridCommandsAsync(delayTime);

                    _runTimes++;

                    Dispatcher.Invoke(() =>
                    {
                        UpdateRunTimesUI();
                    });

                    if (useRepeatCount && _runTimes >= targetCount)
                    {
                        LoopIsAuto = false;
                        mainWindow.listBoxCommand.Items.Add("Target run times reached.");
                        MessageBox.Show("Target run times reached.");
                        break;
                    }

                    // 4) 檢查是否被手動停止
                    if (!LoopIsAuto)
                    {
                        mainWindow.listBoxCommand.Items.Add("Loop manually stopped.");
                        break;
                    }
                }

                // 迴圈結束後，復原按鈕文字
                button.Content = "Loop";
                LoopIsAuto = false;
            }
            else
            {
                // 按下 "Stop Loop" 時的處理
                button.Content = "Loop";
                LoopIsAuto = false;
            }
        }

        private async Task LOOPGridCommandsAsync(string delaytime)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            foreach (Grid item in CommandList.Children)
            {
                byte retval, retryCount = 0; //紀錄傳輸狀態
                if (item.Children.Count == 1)
                {
                    foreach (var command in item.Children)
                    {
                        // Reset the TaskCompletionSource for each command
                        createRCS();

                        item.SetValue(BackgroundProperty, System.Windows.Media.Brushes.Green);

                        BaseGridStyle gridStyle = command as BaseGridStyle;
                        string tempCommand = "";

                        if (gridStyle.LoopCommandByteArray()[4] == 0x12)   ///0x12代表MCCS 的Contrast ,目前這樣設定是方便測試用
                        {
                            //Console.WriteLine("是回復Contrast 的reply GET");
                            do
                            {
                                retval = i2cController.DDCCI_Send_Command(gridStyle.GetCommandByteArray(), gridStyle.getReplyAndUpdateUI);
                                Debug.WriteLine($"ret val = {retval}, retryCount = {++retryCount}");　　//記錄傳輸狀態
                            } while (retval != 0 && retryCount <= 10);
                        }
                        else
                        {
                            foreach (byte b in gridStyle.LoopCommandByteArray())
                            {
                                tempCommand += b.ToString("X2");
                            }
                            byte cksum = check_sum(gridStyle.LoopCommandByteArray());
                            Debug.WriteLine("Loop 按鈕執行的 Set Command: " + tempCommand + cksum.ToString("X2") + "\tUTF-8:" + BitConverter.ToString(gridStyle.LoopCommandByteArray()));
                            mainWindow.readDataLength = gridStyle.LoopRlength() + 9;
                            if (mainWindow.comboModelPort.Text.Contains("COM"))
                                mainWindow.uartSendCommand(tempCommand + cksum.ToString("X2"));
                            else if (mainWindow.comboModelPort.Text.Contains("I2C"))
                            {
                                switch (gridStyle.LoopCommandByteArray()[4])
                                {
                                    case (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND:
                                        do
                                        {
                                            //260325 wu add for testing delay E3
                                            retval = 0;
                                            //260325 wu add for testing delay E3

                                            if (gridStyle.LoopCommandByteArray()[6] == (byte)VCCode.ONYX_FCODE_05_SET_INPUT_SOURCE) /////05 SET_INPUT_SOURCE  
                                            {
                                                Console.WriteLine("讀取xml裡的delay 時間:" + delaytime);
                                                await Task.Delay(Convert.ToInt32(delaytime));
                                            }
                                            //zh251117 add>
                                            if (gridStyle.LoopCommandByteArray()[6] == (byte)VCCode.ONYX_FCODE_DELAY) /////x99 Delay  
                                            {
                                                Console.WriteLine("讀取Delay裡的時間: " + gridStyle.LoopCommandByteArray()[8]);
                                                await Task.Delay(Convert.ToInt32(gridStyle.LoopCommandByteArray()[8]) * 1000);//gui sec delay
                                                retval = 0;
                                                // continue;
                                                break;
                                            }
                                            //zh251117 add<

                                            ////260325 wu add for testing delay E3
                                            retval = i2cController.DDCCI_Send_Command(gridStyle.LoopCommandByteArray());
                                            Debug.WriteLine($"ret val = {retval}, retryCount = {++retryCount}");  //紀錄傳輸狀態
                                                                                                                  ////260325 wu add for testing delay E3


                                            //retval = i2cController.DDCCI_Send_Command(gridStyle.LoopCommandByteArray());
                                            //Debug.WriteLine($"ret val = {retval}, retryCount = {++retryCount}");  //紀錄傳輸狀態

                                        } while (retval != 0 && retryCount <= 10);
                                        if (_receiveCompletionSource != null)
                                        {
                                            _receiveCompletionSource.SetResult(true);
                                        }
                                        break;
                                    case (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND:
                                        do
                                        {
                                            retval = i2cController.DDCCI_Send_Command(gridStyle.LoopCommandByteArray(), gridStyle.getReplyAndUpdateUI);
                                            Debug.WriteLine($"ret val = {retval}, retryCount = {++retryCount}");  //紀錄傳輸狀態
                                        } while (retval != 0 && retryCount <= 10);
                                        if (_receiveCompletionSource != null)
                                        {
                                            _receiveCompletionSource.SetResult(true);
                                        }
                                        break;
                                }
                            }



                            // Wait for the command to complete with a timeout
                            if (await Task.WhenAny(_receiveCompletionSource.Task, Task.Delay(5000)) == _receiveCompletionSource.Task)
                            {
                                //if (gridStyle.LoopCommandByteArray()[4] == (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND)   ///ONYX 部分待做
                                //{ }
                                if (gridStyle.LoopCommandByteArray()[0] == 0x6E)  ///MCCS 部分
                                {
                                    Console.WriteLine("MCCS Debug用 長度是 6E  " + gridStyle.LoopCommandByteArray()[0]);
                                    await readReplyCommandAsyncFromMainWindow();
                                }
                                //gridStyle.getReplyAndUpdateUI(replyCommandFromMainWindow);
                                //i2cController.I2C_CommandAnalyzer(tempCommandIsCksum);
                            }
                            else
                            {
                                mainWindow.listBoxCommand.Items.Add("Command read timeout");
                            }
                        }
                        await Task.Delay(1000);
                        item.SetValue(BackgroundProperty, System.Windows.Media.Brushes.Transparent);

                        //mainWindow.Dispatcher.BeginInvoke(new Action(delegate
                        //{
                        //    mainWindow.listBoxCommand.Items.Add(tempCommandIsCksum);
                        //}));

                        if (!LoopIsAuto)
                        {
                            break;
                        }
                    }
                }

                if (!LoopIsAuto)
                {
                    break;
                }
            }
            mainWindow.listBoxCommand.Items.Add("Complete command loop ......");
        }
        private async Task LOOPGridCommandAsync()
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            int timeout = 5000;

            if (await Task.WhenAny(_receiveCompletionSource.Task, Task.Delay(timeout)) == (_receiveCompletionSource.Task))
            {
                if (!isAckE0)
                {
                    mainWindow.listBoxCommand.Items.Add("Error command send");
                    mainWindow.listBoxCommand.Items.Add("Stop command loop ......");
                    _receiveCompletionSource = null;
                    buttonLoop.IsEnabled = true;
                }
            }
            else
            {
                mainWindow.listBoxCommand.Items.Add("Command read timeout");
                mainWindow.listBoxCommand.Items.Add("Stop command loop ......");
                _receiveCompletionSource = null;
                buttonLoop.IsEnabled = true;
            }
        }
        public void factoryGridCommandIsComplete(byte[] replyCommand)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => factoryGridCommandIsComplete(replyCommand)));
                return;
            }

            replyCommandFromMainWindow = replyCommand;

            if (_receiveCompletionSource != null)
            {
                _receiveCompletionSource.SetResult(true);
            }
        }
        public void createRCS()
        {
            _receiveCompletionSource = new TaskCompletionSource<bool>();
        }

        private async Task readReplyCommandAsyncFromMainWindow()
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            replyCommandFromMainWindow = mainWindow.replyCMDtoFactory;
        }

        private void SetTheTextToMainWindowListBoxCommand(string strText)
        {
            // 260223 add:防呆檢查如果 strText 是 null 或完全空白，直接退出 
            if (string.IsNullOrEmpty(strText))
            {
                return;
            }


            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            // xml 裡的description以逗號分開
            string[] descriptionItems = strText.Split(',');

            this.Dispatcher.BeginInvoke(new Action(delegate
            {
                mainWindow.listBoxCommand.Items.Clear();
                foreach (var item in descriptionItems)
                {
                    // 加上 IsNullOrWhiteSpace 判斷，避免把空白字串加進 ListBox
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        mainWindow.listBoxCommand.Items.Add(item.Trim());
                    }
                }
            }));
        }

        //260618 wu add for keyboard  event  to move the grid 
        private void Factory_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 不攔截 Ctrl / Alt / Shift 組合鍵
            if (Keyboard.Modifiers != ModifierKeys.None)
                return;

            if (selectedGrid == null)
                return;

            bool moved = false;

            switch (e.Key)
            {
                case Key.Up:
                    moved = MoveSelectionByOffset(-1);
                    break;

                case Key.Down:
                    moved = MoveSelectionByOffset(1);
                    break;

                case Key.Left:
                    moved = MoveSelectionByOffset(-10);
                    break;

                case Key.Right:
                    moved = MoveSelectionByOffset(10);
                    break;

                case Key.Enter:
                    // Enter 等同按 Set
                    if (buttonSet.IsEnabled)
                    {
                        buttonSet_Click(buttonSet, new RoutedEventArgs(Button.ClickEvent));
                        e.Handled = true;
                    }
                    return;
            }

            if (moved)
            {
                e.Handled = true;
            }
        }

        private bool MoveSelectionByOffset(int offset)
        {
            if (selectedGrid == null)
                return false;

            int currentIndex = GetGridIndex(selectedGrid);
            if (currentIndex < 0)
                return false;

            int targetIndex = currentIndex + offset;
            if (targetIndex < 0 || targetIndex >= CommandList.Children.Count)
                return false;

            Grid targetGrid = CommandList.Children[targetIndex] as Grid;
            if (targetGrid == null)
                return false;

            SelectGridOnly(targetGrid);
            return true;
        }

        private int GetGridIndex(Grid grid)
        {
            if (grid == null || string.IsNullOrEmpty(grid.Name) || !grid.Name.StartsWith("gridIndex"))
                return -1;

            string indexText = grid.Name.Substring("gridIndex".Length);
            return int.TryParse(indexText, out int index) ? index : -1;
        }

        private void SelectGridOnly(Grid targetGrid)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;

            if (selectedGrid != null)
            {
                selectedGrid.Background = Brushes.Transparent;
            }

            selectedGrid = targetGrid;
            selectedGrid.Background = Brushes.Orange;

            mainWindow?.listBoxCommand.Items.Clear();

            if (targetGrid.Children.Count == 1)
            {
                BaseGridStyle baseGridStyle = targetGrid.Children[0] as BaseGridStyle;
                if (baseGridStyle != null)
                {
                    SetTheTextToMainWindowListBoxCommand(baseGridStyle.GetDescription());
                    setButtomButtonState(baseGridStyle.GetType());
                }
                else
                {
                    setButtomButtonState("NULL");
                }
            }
            else
            {
                setButtomButtonState("NULL");
            }
              
            Keyboard.Focus(this);
        }

        private bool MoveSelectedGridByOffset(int offset)
        {
            if (selectedGrid == null || selectedGrid.Children.Count != 1)
                return false;

            int currentIndex = GetGridIndex(selectedGrid);
            if (currentIndex < 0)
                return false;

            int targetIndex = currentIndex + offset;
            if (targetIndex < 0 || targetIndex > 19)
                return false;

            Grid targetGrid = CommandList.Children[targetIndex] as Grid;
            if (targetGrid == null || targetGrid == selectedGrid)
                return false;

            SwapSelectedGridTo(targetGrid);
            return true;
        }

   

        private void SwapSelectedGridTo(Grid targetGrid)
        {
            Grid sourceGrid = selectedGrid;

            var sourceChild = sourceGrid.Children[0];
            var sourceTag = sourceGrid.Tag;

            sourceGrid.Children.Clear();
            sourceGrid.Tag = null;

            // 目標grid 如果原本有 command，就跟目前選取的 command 互換
            if (targetGrid.Children.Count == 1)
            {
                var targetChild = targetGrid.Children[0];
                var targetTag = targetGrid.Tag;

                targetGrid.Children.Clear();

                sourceGrid.Children.Add(targetChild);
                sourceGrid.Tag = targetTag;
            }

            targetGrid.Children.Add(sourceChild);
            targetGrid.Tag = sourceTag;

            sourceGrid.Background = Brushes.Transparent;
            selectedGrid = targetGrid;
            selectedGrid.Background = Brushes.Orange;

            // 鍵盤一樣在 Factory 頁面
            Keyboard.Focus(this);
        }

        private void buttonMoveUp_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedGridByOffset(-1);
        }

        private void buttonMoveDown_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedGridByOffset(1);
        }
        //260618 wu add for keyboard  event  to move the grid 


        private void buttonSaveScript_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "XML files (*.xml)|*.xml",
                DefaultExt = ".xml",
                FileName = "FactoryScript.xml"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    XDocument doc = new XDocument(
                        new XElement("commands", new XAttribute("version", "V002B"),
                            new XElement("magic", "51")
                        )
                    );

                    foreach (Grid grid in CommandList.Children)
                    {
                        if (grid.Children.Count == 1 && grid.Tag is MainWindow.Item item)
                        {
                            XElement commandElement = new XElement("command", new XAttribute("ui", "factory"));
                            commandElement.Add(new XElement("name", item.Name ?? ""));
                            commandElement.Add(new XElement("type", item.Type ?? ""));
                            commandElement.Add(new XElement("length", item.Length ?? ""));
                            commandElement.Add(new XElement("code", "C0"));
                            commandElement.Add(new XElement("vccode", item.VCCode ?? ""));

                            // 重建 dataX 標籤
                            if (item.DataValues != null && item.DataValues.Count > 0)
                            {
                                for (int i = 0; i < item.DataValues.Count; i++)
                                {
                                    commandElement.Add(new XElement($"data{i}", item.DataValues[i]));
                                }
                            }
                            else
                            {
                                commandElement.Add(new XElement("data0", "00"));
                                commandElement.Add(new XElement("data1", "01"));
                            }

                            commandElement.Add(new XElement("model", item.Model ?? ""));
                            commandElement.Add(new XElement("style", item.Style ?? ""));
                            commandElement.Add(new XElement("prefix", item.Prefix ?? ""));
                            commandElement.Add(new XElement("wlength", item.Wlength ?? ""));
                            commandElement.Add(new XElement("rlength", item.Rlength ?? ""));

                            if (!string.IsNullOrEmpty(item.Delay))
                            {
                                commandElement.Add(new XElement("delay", item.Delay));
                            }

                            commandElement.Add(new XElement("description", item.Description ?? ""));

                            BaseGridStyle gridStyle = grid.Children[0] as BaseGridStyle;
                            if (gridStyle != null)
                            {
                                commandElement.Add(new XElement("currentValue", gridStyle.GetCurrentSetting()));
                            }

                            doc.Root.Add(commandElement);
                        }
                    }

                    doc.Save(saveFileDialog.FileName);
                    MessageBox.Show("Script saved successfully!", "Save Script", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving script: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private async void buttonLoadScript_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "XML files (*.xml)|*.xml",
                DefaultExt = ".xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    System.Xml.XmlDocument xmlDoc = new System.Xml.XmlDocument();
                    xmlDoc.Load(openFileDialog.FileName);

                    // 清空目前的畫面狀態
                    foreach (Grid grid in CommandList.Children)
                    {
                        grid.Children.Clear();
                        grid.Background = Brushes.Transparent;
                        grid.Tag = null;
                    }
                    selectedGrid = null;
                    buttonSet.IsEnabled = false;
                    buttonGet.IsEnabled = false;

                    // 讀取 XML 內容
                    System.Xml.XmlNodeList commandMagic = xmlDoc.GetElementsByTagName("magic");
                    System.Xml.XmlNodeList commandName = xmlDoc.GetElementsByTagName("name");
                    System.Xml.XmlNodeList commandType = xmlDoc.GetElementsByTagName("type");
                    System.Xml.XmlNodeList commandLength = xmlDoc.GetElementsByTagName("length");
                    System.Xml.XmlNodeList commandVCCode = xmlDoc.GetElementsByTagName("vccode");
                    System.Xml.XmlNodeList commandModel = xmlDoc.GetElementsByTagName("model");
                    System.Xml.XmlNodeList commandStyle = xmlDoc.GetElementsByTagName("style");
                    System.Xml.XmlNodeList commandPrefix = xmlDoc.GetElementsByTagName("prefix");
                    System.Xml.XmlNodeList commandWlength = xmlDoc.GetElementsByTagName("wlength");
                    System.Xml.XmlNodeList commandRlength = xmlDoc.GetElementsByTagName("rlength");
                    System.Xml.XmlNodeList commandDelay = xmlDoc.GetElementsByTagName("delay");
                    System.Xml.XmlNodeList commandDescription = xmlDoc.GetElementsByTagName("description");

                    string magicStr = commandMagic.Count > 0 ? commandMagic[0].InnerText : "51";

                    for (int i = 0; i < commandName.Count; i++)
                    {
                        var item = new MainWindow.Item
                        {
                            Magic = magicStr,
                            Name = commandName[i]?.InnerText,
                            Type = commandType[i]?.InnerText,
                            Length = commandLength[i]?.InnerText,
                            VCCode = commandVCCode[i]?.InnerText,
                            Model = (commandModel.Count > i) ? commandModel[i]?.InnerText : "",
                            Prefix = commandPrefix[i]?.InnerText,
                            Style = commandStyle[i]?.InnerText,
                            Wlength = commandWlength[i]?.InnerText,
                            Rlength = commandRlength[i]?.InnerText,
                            Delay = (commandDelay.Count > i) ? commandDelay[i]?.InnerText : "",
                            Description = (commandDescription.Count > i) ? commandDescription[i]?.InnerText ?? "" : "",
                            DataValues = new List<string>()
                        };

                        System.Xml.XmlNode commandNode = commandName[i].ParentNode;
                        for (int j = 0; ; j++)
                        {
                            System.Xml.XmlNode dataNode = commandNode.SelectSingleNode($"data{j}");
                            if (dataNode == null) break;
                            item.DataValues.Add(dataNode.InnerText);
                        }

                        Grid targetGrid = UpdateFactoryView(item.Name, item.Style, item.Length, item.Rlength, item.Type, item.VCCode, item.Description, item.DataValues);
                        System.Xml.XmlNode curValNode = commandNode.SelectSingleNode("currentValue");
                        string savedCurrentValue = curValNode != null ? curValNode.InnerText : "";
                        if (targetGrid != null)
                        {
                            targetGrid.Tag = item;

                            if (!string.IsNullOrEmpty(savedCurrentValue) && targetGrid.Children.Count > 0)
                            {
                                BaseGridStyle gridStyle = targetGrid.Children[0] as BaseGridStyle;
                                gridStyle?.RestoreSetting(savedCurrentValue);
                            }

                        }

                        await Task.Delay(50);
                    }

                    foreach (Grid grid in CommandList.Children)
                    {
                        if (grid.Children.Count == 1)
                        {
                            buttonLoop.IsEnabled = true;
                            break;
                        }
                    }

                    MessageBox.Show("Script loaded successfully!", "Load Script", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading script: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void clickMCCSmode_Checked(object sender, RoutedEventArgs e)
        {
            /////MCCS mode checkbox 是用click  而不是用check  這樣checkbox 取消勾選的時候才能繼續用
            if (clickMCCSmode.IsChecked == true)
            {
                isMCCSmode = true;
                clickMCCSmode.IsChecked = true;
                Console.WriteLine("MCCS　mode  ON");
            }
            else
            {
                isMCCSmode = false;
                clickMCCSmode.IsChecked = false;
                Console.WriteLine("MCCS　mode  OFF");
            }
        }

        public bool isMccsMode()
        {
            return isMCCSmode;
        }

        private void clickRepeatCount_Checked(object sender, RoutedEventArgs e)
        {
            textBoxRepeatCount.IsEnabled = (clickRepeatCount.IsChecked == true);
        }
    }
}