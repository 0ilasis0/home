using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xceed.Wpf.Toolkit;



namespace Measure_Tool
{
    public partial class SelectMonitorForm : Form
    {
        private Screen mScreen = null;
        private int mScreenSize = 0;
        public SelectMonitorForm()
        {
            InitializeComponent();
            this.DialogResult = DialogResult.Cancel;

            this.StartPosition = FormStartPosition.Manual;
            this.Left = 700;
            this.Top = 400;


            var allScreens = Screen.AllScreens;


            monitorComboBox.Items.Clear();
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                //monitorComboBox.Items.Add("Display " + (i + 1).ToString() + " : " + Screen.AllScreens[i].DeviceName);
                string deviceName = Screen.AllScreens[i].DeviceName;

                // 把 "\\.\" 替換成空白字串 (注意：在 C# 中反斜線 \ 需要用 @ 標示或寫成 \\\\)
                string cleanName = deviceName.Replace(@"\\.\", "");

                // 將乾淨的名稱 (例如 DISPLAY2) 加入選單
                monitorComboBox.Items.Add(cleanName); //0608 ian add 只顯示 \\.\DISPLAY1、\\.\DISPLAY2...

            }
            if (monitorComboBox.Items.Count > 0)
            {
                monitorComboBox.SelectedIndex = Screen.AllScreens.Length - 1;
            }
        }

        private void buttonContinue_Click(object sender, EventArgs e)
        {
            mScreen = Screen.AllScreens[monitorComboBox.SelectedIndex];
            mScreenSize = (int)numericUpDown1.Value;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public Screen SelectedScreen
        {
            get { return mScreen; }
        }

        public int SelectedScreenSize
        {
            get { return mScreenSize; }
        }

        private void monitorComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
        }
    }
}
