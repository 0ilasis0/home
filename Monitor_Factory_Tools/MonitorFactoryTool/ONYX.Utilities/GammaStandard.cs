using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class GammaStandard
    {
        public static double[] gammaStandard(double lowLum, double highLum, double gamma, int size)
        {
            if (lowLum < 0)
            {
                return null;
            }
            else if (highLum < lowLum)
            {
                return null;
            }
            else if (size < 0)
            {
                return null;
            }

            double[] result = new double[size];
            double lumGap = highLum - lowLum;
            for (int i = 0; i < size; i++)
            {
                double expBase = ((double)i / (size - 1));
                result[i] = lowLum + lumGap * Math.Pow(expBase, gamma);
            }
            return result;
        }

        public static double[] DICOMStandard(double lowLum, double highLum, int size)
        {
            if (lowLum < 0)
            {
                return null;
            }
            else if (highLum < lowLum)
            {
                return null;
            }
            else if (size < 0)
            {
                return null;
            }

            double[] result = new double[size];
            double lowJND = JNDCalculator.LuminanceToJND(lowLum);
            double highJND = JNDCalculator.LuminanceToJND(highLum);

            // set boundary luminance to avoid the
            // error of the transform between JND and luminance
            result[0] = lowLum;
            result[size - 1] = highLum;
            for (int i = 1; i < (size - 1); i++)
            {
                double currentJND = lowJND + (highJND - lowJND) * i / (size - 1);
                result[i] = JNDCalculator.JNDToLuminance(currentJND);
            }
            return result;
        }

        public static double getGamma(double[] data)
        {
            double logPiYi, logPiSquare;
            double[] Pi, Yi;
            for (int i = 1; i < data.Length; i++)
            {
                data[i] = data[i] - data[0];
            }
            Pi = new double[data.Length - 1];
            Yi = new double[data.Length - 1];

            for (int i = 1; i < data.Length; i++)
            {
                Pi[i - 1] = (double)i / (data.Length - 1);
                Yi[i - 1] = (double)data[i] / data[data.Length - 1];
            }

            logPiYi = 0;
            logPiSquare = 0;
            for (int i = 0; i < Pi.Length; i++)
            {

                if ((Pi[i] < 0.0001) || (Yi[i] < 0.0001))
                {
                    continue;
                }

                logPiYi += (Math.Log(Pi[i]) * Math.Log(Yi[i]));
                logPiSquare += (Math.Log(Pi[i]) * Math.Log(Pi[i]));
            }
            return logPiYi / logPiSquare;
        }

        public static double gammaCalculte(double[] Pi, double[] Yi) 
        {
            if ((Pi == null) || (Yi == null))
            {
                return -1;
            }
            if (Pi.Length != Yi.Length)
            {
                return -1;
            }
            double sumPiYi = 0, sumPiPi = 0;
            double[] y = new double[Yi.Length];
            double[] x = new double[Pi.Length];
            for (int i = 0; i < y.Length; i++)
            {
                y[i] = (Yi[i] - Yi[0]) / (Yi[Yi.Length - 1] - Yi[0]);
            }
            for (int i = 0; i < y.Length; i++)
            {
                x[i] = Pi[i] / 255.0000;
            }
            for (int i = 0; i < Pi.Length; i++)
            {
                if ((x[i] < 0.0001) || (y[i] < 0.0001))
                {
                    continue;
                }
                sumPiYi += (Math.Log(x[i]) * Math.Log(y[i]));
                sumPiPi += (Math.Log(x[i]) * Math.Log(x[i]));
            }
            return sumPiYi / sumPiPi;
        }
        public static int getBestLevel(int start, double target, double[] data)
        {
            if (data == null || data.Length == 0)
                return 0;

            if (start >= data.Length)
                return data.Length - 1;

            for (int i = start; i < data.Length; i++)
            {
                if (target == data[i])
                {
                    return i;
                }
                else if (data[i] > target)
                {
                    if (i == 0) return 0; // 修正 i-1 可能小於 0

                    // 找最接近 target 的點
                    if (Math.Abs(data[i] - target) >= Math.Abs(data[i - 1] - target))
                        return i - 1;
                    else
                        return i;
                }
            }

            return data.Length - 1;
        }

        public static double[] getGammaStandard(double minLum, double maxLum, double gammaValue, uint LUTSize)
        {
            if (minLum < 0)
            {
                minLum = 0;
            }
            if (maxLum < minLum)
            {
                maxLum = minLum;
            }
            if (LUTSize <= 0)
            {
                return null;
            }



            double[] result = new double[LUTSize];
            double normalizeIndex = 0.0f;
            double lumGap = maxLum - minLum;
            Console.WriteLine("GammaStandard", "getGammaStandard", "minLum = " + minLum.ToString("F4") +
                                                                                     " maxLum = " + maxLum.ToString("F4") +
                                                                                     " gammaValue = " + gammaValue.ToString("F4"));
            for (int index = 0; index < LUTSize; index++)
            {
                normalizeIndex = (double)index / (LUTSize - 1);
                result[index] = minLum + lumGap * Math.Pow(normalizeIndex, gammaValue);

            }
            return result;
        }
        public static double[] getDICOMStandard(double minLum, double maxLum, double JND_Scale, uint LUTSize)
        {
            double[] result = new double[LUTSize];
            double minJND = 0;
            double maxJND = 0;
            double tempJND = 0;
            minJND = JNDCalculator.LuminanceToJND(minLum);
            maxJND = JNDCalculator.LuminanceToJND(maxLum);

            Console.WriteLine("getDICOMStandard .... minLum = " + minLum.ToString()
                + " , maxLum = " + maxLum.ToString() + " , JND_Scale = " + JND_Scale.ToString() + " , LUTSize = " + LUTSize.ToString());

            Console.WriteLine("minJND = " + minJND.ToString() + " , maxJND = " + maxJND.ToString());

            //指定第0階與最高階的亮度 以避免JND轉換的誤差
            //result[0] = minLum;
            //result[LUTSize - 1] = maxLum;

            //ProcessLog.getInstance().writeLine("GammaStandard", "getDICOMStandard", "minLum = " + minLum.ToString("F4") +
            //                                                                        " maxLum = " + maxLum.ToString("F4"));

            //for (int index = 1; index < (LUTSize - 1); index++)
            for (int index = 0; index <= (LUTSize - 1); index++)
            {
                tempJND = minJND + (maxJND - minJND) * index / (LUTSize - 1) * JND_Scale;
                result[index] = JNDCalculator.JNDToLuminance(tempJND);

                Console.WriteLine("JND(" + index.ToString("000") + ") = " + tempJND.ToString() + " , lum = " + result[index].ToString());
            }

            return result;
        }



    }
}
