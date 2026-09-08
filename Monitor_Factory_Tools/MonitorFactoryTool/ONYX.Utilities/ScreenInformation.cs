using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ONYX.Utilities
{
    public static class ScreenInformation
    {
        private const double ReferenceCm = 8.0;        // default white box width height 8cm
        private const double ReferenceInch = 21.0;     // default monitor 21 inches
        private const int ReferencePixel = 300;        // default 300px on 21 inches monitor 
        public static class SelectedMonitorInfo
        {
            public static Screen Screen { get; set; } = Screen.PrimaryScreen;
            public static int ScreenSizeInInches { get; set; } = 21; // default 21  inches
        }
        public static double GetWhiteSquareSizePixels(double screenWidthPx, double screenHeightPx, double screenDiagonalCm)
        {
            // Ratio conversion based on visual verification
            //  21inches diagonal screen as the standard, where a white square of 300px corresponds to 8 cm

            double ratio = screenDiagonalCm / (ReferenceInch * 2.54) * 0.8;     //0,8 wu add to adjust the ratio
            return ReferencePixel * ratio;
        }
    }
}

