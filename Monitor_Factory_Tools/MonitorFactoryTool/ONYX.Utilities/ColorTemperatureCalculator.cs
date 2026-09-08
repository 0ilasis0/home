using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class ColorTemperatureCalculator
    {
        public static bool CT2xy(double CT, ref double x, ref double y)
        {
            double temp;
            double[] result = new double[2];
            temp = CT / 1000;
            if ((CT >= 4000) && (CT <= 7000))
            {
                x = -4.607 / Math.Pow(temp, 3) + 2.9678 / Math.Pow(temp, 2) + 0.09911 / temp + 0.244063;
            }
            else if ((CT > 7000) && (CT <= 25000))
            {
                x = -2.0064 / Math.Pow(temp, 3) + 1.9018 / Math.Pow(temp, 2) + 0.24748 / temp + 0.237040;
            }
            else
            {
                x = 0;
                y = 0;
                return false;
            }
            y = -3.0 * Math.Pow(x, 2) + 2.87 * x - 0.275;
            return true;
        }

        public static double xy2CT(double x, double y)
        {
            double temp, result;
            if ((0 == x) || (0 == y))
            {
                return 0;
            }

            temp = (x - 0.332F) / (0.1858F - y);

            result = ((437 * (float)(Math.Pow((double)temp, 3))) +
                      (3601 * (float)(Math.Pow((double)temp, 2))) +
                      (6831 * temp) + 5517);
            return result;
        }
    }
}
