using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static MonitorFactoryTool.MainWindow;

namespace MonitorFactoryTool.Pages
{
    /// <summary>
    /// Interaction logic for Pattern.xaml
    /// </summary>
    public partial class Pattern : Window
    {
        public Pattern()
        {
            InitializeComponent();
            MainWindow.AlwaysOnTopChanged += OnAlwaysOnTopChanged;

        }
        //mainwindow's always on top fcn.
        private void OnAlwaysOnTopChanged(bool isAlwaysOnTop)
        {
            this.Topmost = isAlwaysOnTop;
        }

        protected override void OnClosed(EventArgs e)
        {
            MainWindow.AlwaysOnTopChanged -= OnAlwaysOnTopChanged;
            base.OnClosed(e);
        }
        private void sliderPattern_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderPatternRed == null ||
                sliderPatternGreen == null ||
                sliderPatternBlue == null ||
                sliderPatternWhite == null ||
                recPattern == null)
                return;

            Byte r = (Byte)(sliderPatternRed.Value * sliderPatternWhite.Value / 255);
            Byte g = (Byte)(sliderPatternGreen.Value * sliderPatternWhite.Value / 255);
            Byte b = (Byte)(sliderPatternBlue.Value * sliderPatternWhite.Value / 255);


            recPattern.Fill = new SolidColorBrush(Color.FromRgb(r, g, b));
        }
    }
}
