using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class MonotoneCubicInterpolator
    {
        public static double[] interpolation(double[] src, int size)
        {
            if (size < src.Length)
            {
                return null;
            }
            else
            {
                int origin_size = src.Length;
                double dx = ((double)1 / (origin_size - 1));
                double[] dys = new double[origin_size - 1];
                double[] slope = new double[origin_size - 1];
                double[] c1s = new double[origin_size];
                double[] c2s = new double[origin_size];
                double[] c3s = new double[origin_size];
                double[] result = new double[size];

                // calculate slope
                for (int i = 0; i < origin_size - 1; i++)
                {
                    dys[i] = src[i + 1] - src[i];
                    slope[i] = dys[i] / dx;
                }

                // Get degree-1 coefficients
                c1s[0] = slope[0];
                for (int i = 0; i < origin_size - 2; i++)
                {
                    double m = slope[i], mNext = slope[i + 1];
                    if ((m * mNext) <= 0)
                    {
                        c1s[i] = 0;
                    }
                    else
                    {
                        double common = 2 * dx;
                        c1s[i] = (3 * common / (common + dx) / m) + ((common + dx) / mNext);
                    }
                }
                c1s[origin_size - 1] = slope[origin_size - 2];

                // Get degree-2 & degree-3 coefficients
                for (int i = 0; i < origin_size - 1; i++)
                {
                    double c1 = c1s[i], m = slope[i], invDx = 1 / dx, common = c1 + c1s[i + 1] - m - m;
                    c2s[i] = (m - c1 - common) * invDx;
                    c3s[i] = common * invDx * invDx;
                }

                result[0] = src[0];
                result[size - 1] = src[origin_size - 1];
                for (int i = 1; i < size - 1; i++)
                {
                    double x = (double)i / (size - 1);
                    int intervel = (int)(x / dx);
                    if (x == (dx * intervel))
                    {
                        result[i] = src[intervel];
                    }
                    else
                    {
                        double diff = x - (dx * intervel);
                        double diffsq = diff * diff;
                        result[i] = src[intervel] + c1s[intervel] * diff + c2s[intervel] * diffsq + c3s[intervel] * diff * diffsq;
                    }
                }
                return result;
            }
        }
    }
}
