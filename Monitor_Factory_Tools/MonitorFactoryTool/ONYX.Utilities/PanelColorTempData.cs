using ONYX.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorFactoryTool.ONYX.Utilities
{
    public class PanelColorTempData
    {
        public ColorData m_black;
        public ColorData m_white;

        public ColorData m_red;
        public ColorData m_green;
        public ColorData m_blue;

        public double[] whiteLum;
        public double[] LumCompensation;

        public double[] redLum;
        public double[] greenLum;
        public double[] blueLum;

        // LUT
        public double[] whiteLUT;
        public double[] redLUT;
        public double[] greenLUT;
        public double[] blueLUT;

        // raw data to target
        public double[] RawColorTemperatureTarget;

        // Color Temperature R, G, B Level
        public int RawRedLevel;
        public int RawGreenLevel;
        public int RawBlueLevel;

        public double[] RawWhiteGrayLevel;
        public double[] RawWhiteLum;
        public double[] RawRedLum;
        public double[] RawGreenLum;
        public double[] RawBlueLum;


        //        public Matrix CSC;
        public ColorVector colorVector;

        public double[] getLum(int ch)
        {
            if (Parameters.RED_CH == ch)
                return redLum;
            else if (Parameters.GREEN_CH == ch)
                return greenLum;
            else if (Parameters.BLUE_CH == ch)
                return blueLum;
            else if (Parameters.WHITE_CH == ch)
                return whiteLum;
            else
                return null;
        }
    }
}
