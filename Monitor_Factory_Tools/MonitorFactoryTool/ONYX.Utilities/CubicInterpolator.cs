using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class CubicInterpolator
    {
        public static double[] interpolation(double[] srcX, double[] srcY, int size)
        {
            if (srcX.Length != srcY.Length)
            {
                return null;
            }
            int origin_size = srcX.Length;
            double[] result = new double[size];
            double[] Hi = new double[origin_size];
            double[] Alpha = new double[origin_size];
            double[] Li = new double[origin_size];
            double[] Zi = new double[origin_size];
            double[] Ui = new double[origin_size];
            double[] Ai = new double[origin_size];
            double[] Bi = new double[origin_size];
            double[] Ci = new double[origin_size];
            double[] Di = new double[origin_size];

            for (int i = 0; i < origin_size; i++)
            {
                Ai[i] = srcY[i];
            }

            for (int i = 0; i < (origin_size - 1); i++)
            {
                Hi[i] = srcX[i + 1] - srcX[i];
            }

            for (int i = 1; i < (origin_size - 1); i++)
            {
                Alpha[i] = (3 * (Ai[i + 1] - Ai[i]) / Hi[i]) - (3 * (Ai[i] - Ai[i - 1]) / Hi[i - 1]);
            }

            Li[0] = 1;
            Zi[0] = 0;
            Ui[0] = 0;

            for (int i = 1; i < (origin_size - 1); i++)
            {
                Li[i] = 2 * (srcX[i + 1] - srcX[i - 1]) - (Hi[i - 1] * Ui[i - 1]);
                Ui[i] = Hi[i] / Li[i];
                Zi[i] = (Alpha[i] - Hi[i - 1] * Zi[i - 1]) / Li[i];
            }

            Li[origin_size - 1] = 1;
            Zi[origin_size - 1] = 0;
            Ci[origin_size - 1] = 0;

            for (int i = (origin_size - 2); i >= 0; i--)
            {
                Ci[i] = Zi[i] - (Ui[i] * Ci[i + 1]);
                Bi[i] = (Ai[i + 1] - Ai[i]) / Hi[i] - Hi[i] * (Ci[i + 1] + 2 * Ci[i]) / 3;
                Di[i] = (Ci[i + 1] - Ci[i]) / 3 / Hi[i];
            }

            int last_level = 0;
            result[0] = srcY[0];
            result[size - 1] = srcY[origin_size - 1];
            for (int i = 1; i < size - 1; i++)
            {
                double x = srcX[0] + (srcX[origin_size - 1] - srcX[0]) * i / (size - 1);
                for (int j = last_level; j < origin_size - 1; j++)
                {
                    if (x >= srcX[j] && x < srcX[j + 1])
                    {
                        double dx = x - srcX[j];
                        result[i] = Ai[j] + dx * Bi[j] + dx * dx * Ci[j] + dx * dx * dx * Di[j];
                        last_level = j;
                        break;
                    }
                }
            }
            return result;
        }

        public static double[] interpolation(double[] src, int size)
        {
            if (src.Length > size)
            {
                return null;
            }

            int origin_size = src.Length;
            double[] result = new double[size];
            double[] Hi = new double[origin_size];
            double[] Alpha = new double[origin_size];
            double[] Li = new double[origin_size];
            double[] Zi = new double[origin_size];
            double[] Ui = new double[origin_size];
            double[] Ai = new double[origin_size];
            double[] Bi = new double[origin_size];
            double[] Ci = new double[origin_size];
            double[] Di = new double[origin_size];
            double[] srcX = new double[origin_size];

            for (int i = 0; i < origin_size; i++)
            {
                srcX[i] = 1.0f * i / (origin_size - 1);
            }

            for (int i = 0; i < origin_size; i++)
            {
                Ai[i] = src[i];
            }

            for (int i = 0; i < (origin_size - 1); i++)
            {
                Hi[i] = srcX[i + 1] - srcX[i];
            }

            for (int i = 1; i < (origin_size - 1); i++)
            {
                Alpha[i] = (3 * (Ai[i + 1] - Ai[i]) / Hi[i]) - (3 * (Ai[i] - Ai[i - 1]) / Hi[i - 1]);
            }

            Li[0] = 1;
            Zi[0] = 0;
            Ui[0] = 0;

            for (int i = 1; i < (origin_size - 1); i++)
            {
                Li[i] = 2 * (srcX[i + 1] - srcX[i - 1]) - (Hi[i - 1] * Ui[i - 1]);
                Ui[i] = Hi[i] / Li[i];
                Zi[i] = (Alpha[i] - Hi[i - 1] * Zi[i - 1]) / Li[i];
            }

            Li[origin_size - 1] = 1;
            Zi[origin_size - 1] = 0;
            Ci[origin_size - 1] = 0;

            for (int i = (origin_size - 2); i >= 0; i--)
            {
                Ci[i] = Zi[i] - (Ui[i] * Ci[i + 1]);
                Bi[i] = (Ai[i + 1] - Ai[i]) / Hi[i] - Hi[i] * (Ci[i + 1] + 2 * Ci[i]) / 3;
                Di[i] = (Ci[i + 1] - Ci[i]) / 3 / Hi[i];
            }

            int last_level = 0;
            result[0] = src[0];
            result[size - 1] = src[origin_size - 1];
            for (int i = 1; i < size - 1; i++)
            {
                double x = srcX[0] + (srcX[origin_size - 1] - srcX[0]) * i / (size - 1);
                for (int j = last_level; j < origin_size - 1; j++)
                {
                    if (x >= srcX[j] && x < srcX[j + 1])
                    {
                        double dx = x - srcX[j];
                        result[i] = Ai[j] + dx * Bi[j] + dx * dx * Ci[j] + dx * dx * dx * Di[j];
                        last_level = j;
                        break;
                    }
                }
            }
            return result;
        }

        public static bool getCubicInterpolationParameter(double[] srcX, double[] srcY, ref double[] Ai, ref double[] Bi, ref double[] Ci, ref double[] Di)
        {
            try
            {
                if (srcX.Length != srcY.Length)
                {
                    return false;
                }
                int origin_size = srcX.Length;
                double[] Hi = new double[origin_size];
                double[] Alpha = new double[origin_size];
                double[] Li = new double[origin_size];
                double[] Zi = new double[origin_size];
                double[] Ui = new double[origin_size];
                Ai = new double[origin_size];
                Bi = new double[origin_size];
                Ci = new double[origin_size];
                Di = new double[origin_size];

                for (int i = 0; i < origin_size; i++)
                {
                    Ai[i] = srcY[i];
                }

                for (int i = 0; i < (origin_size - 1); i++)
                {
                    Hi[i] = srcX[i + 1] - srcX[i];
                }

                for (int i = 1; i < (origin_size - 1); i++)
                {
                    Alpha[i] = (3 * (Ai[i + 1] - Ai[i]) / Hi[i]) - (3 * (Ai[i] - Ai[i - 1]) / Hi[i - 1]);
                }

                Li[0] = 1;
                Zi[0] = 0;
                Ui[0] = 0;

                for (int i = 1; i < (origin_size - 1); i++)
                {
                    Li[i] = 2 * (srcX[i + 1] - srcX[i - 1]) - (Hi[i - 1] * Ui[i - 1]);
                    Ui[i] = Hi[i] / Li[i];
                    Zi[i] = (Alpha[i] - Hi[i - 1] * Zi[i - 1]) / Li[i];
                }

                Li[origin_size - 1] = 1;
                Zi[origin_size - 1] = 0;
                Ci[origin_size - 1] = 0;

                for (int i = (origin_size - 2); i >= 0; i--)
                {
                    Ci[i] = Zi[i] - (Ui[i] * Ci[i + 1]);
                    Bi[i] = (Ai[i + 1] - Ai[i]) / Hi[i] - Hi[i] * (Ci[i + 1] + 2 * Ci[i]) / 3;
                    Di[i] = (Ci[i + 1] - Ci[i]) / 3 / Hi[i];
                }
            }
            catch(Exception ex)
            {
                Console.Write(ex.Message);
                return false;
            }
            return true;
        }
    }
}
