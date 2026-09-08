using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class LinearInterpolator
    {
        public static double[] interpolation(double[] src, int size)
        {
            if (src.Length > size)
            {
                return null;
            }
            int origin_size = src.Length;
            double dx = 1.0f / (origin_size - 1);
            double[] result = new double[size];


            result[0] = src[0];
            result[size - 1] = src[origin_size - 1];
            for (int i = 1; i < (size - 1); i++)
            {
                double x = 1.0f * i / (size - 1);
                int intervel = (int)(x / dx);
                result[i] = src[intervel] + (src[intervel + 1] - src[intervel]) * ((x - (dx * intervel)) / dx);
            }
            return result;
        }
    }
}
