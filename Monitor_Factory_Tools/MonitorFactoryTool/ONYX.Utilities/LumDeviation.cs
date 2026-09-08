using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONYX.Utilities
{
    public class LumDeviation
    {
        public static double[] Deviation(double[] meas_data, double[] standard_data, int size) 
        {
            double[] deviations = new double[size - 1];
            double[] deltaL_avg_meas = new double[size - 1];
            double[] deltaL_avg_standard = new double[size - 1];
            for (int i = 0; i < size - 1; i++)
            {
                deltaL_avg_meas[i] = (meas_data[i + 1] - meas_data[i]) / (0.5 * (meas_data[i + 1] + meas_data[i]));
                deltaL_avg_standard[i] = (standard_data[i + 1] - standard_data[i]) / (0.5 * (standard_data[i + 1] + standard_data[i]));
                deviations[i] = Math.Round((deltaL_avg_standard[i] - deltaL_avg_meas[i]) / deltaL_avg_standard[i] * 100, 2);
            }
            return deviations;
        }
    }
}
