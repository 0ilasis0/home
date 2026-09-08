using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class JNDCalculator
    {
        public static double LuminanceToJND(double lum)
        {
            double result = 0;
            if (lum < 0.05)
            {
                lum = 0.05;
            }
            double logLum = Math.Log10(lum);

            result = 71.498068 +
                     (94.593053 * logLum) +
                     (41.912053 * Math.Pow(logLum, 2)) +
                     (9.8247004 * Math.Pow(logLum, 3)) +
                     (0.28175407 * Math.Pow(logLum, 4)) +
                     (-1.1878455 * Math.Pow(logLum, 5)) +
                     (-0.18014349 * Math.Pow(logLum, 6)) +
                     (0.14710899 * Math.Pow(logLum, 7)) +
                     (-0.017046845 * Math.Pow(logLum, 8));
            return result;
        }

        public static double JNDToLuminance(double jnd)
        {
            if (jnd < 0)
            {
                jnd = 0;
            }
            else if (jnd > 1024)
            {
                jnd = 1024;
            }
            double result = 0;
            double lnJND = Math.Log(jnd);
            double logLum = ((-1.3011877) + (0.080242636 * lnJND) +
                             (0.13646699 * Math.Pow(lnJND, 2)) +
                             (-0.025468404 * Math.Pow(lnJND, 3)) +
                             (0.0013635334 * Math.Pow(lnJND, 4))) /
                             (1 + (-0.025840191 * lnJND) +
                             (-0.10320229 * Math.Pow(lnJND, 2)) +
                             (0.02874562 * Math.Pow(lnJND, 3)) +
                             (-0.0031978977 * Math.Pow(lnJND, 4)) +
                             (0.00012992634 * Math.Pow(lnJND, 5)));
            result = Math.Pow(10, logLum);
            return result;
        }
    }
}
