using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class TriLinearInterpolator
    {
        public static double getTriLinearInterpolation(double[][][] data, double x, double y, double z)
        {
            double result = 0;
            // crop input range
            if (x > 1)
            {
                x = 1;
            }
            else if(x < 0)
            {
                x = 0;
            }

            if (y > 1)
            {
                y = 1;
            }
            else if (y < 0)
            {
                y = 0;
            }

            if (z > 1)
            {
                z = 1;
            }
            else if (z < 0)
            {
                z = 0;
            }

            // find cubic 
            double xInterval = 1.0f / data.GetLength(0);
            double yInterval = 1.0f / data.GetLength(1);
            double zInterval = 1.0f / data.GetLength(2);

            int xIndex = (int)(x / xInterval);
            int yIndex = (int)(y / yInterval);
            int zIndex = (int)(z / zInterval);

            //interpolate X-axis
            double p1, p2, p3, p4;
            p1 = data[xIndex][yIndex][zIndex] + 
                 (x - (xInterval * xIndex)) / xInterval * 
                 (data[xIndex + 1][yIndex][zIndex] - data[xIndex][yIndex][zIndex]); 
            p2 = data[xIndex][yIndex + 1][zIndex] +
                 (x - (xInterval * xIndex)) / xInterval * 
                 (data[xIndex + 1][yIndex + 1][zIndex] - data[xIndex][yIndex + 1][zIndex]);
            p3 = data[xIndex][yIndex][zIndex + 1] +
                 (x - (xInterval * xIndex)) / xInterval * 
                 (data[xIndex + 1][yIndex][zIndex + 1] - data[xIndex][yIndex][zIndex + 1]);
            p4 = data[xIndex][yIndex + 1][zIndex + 1] +
                 (x - (xInterval * xIndex)) / xInterval * 
                 (data[xIndex + 1][yIndex + 1][zIndex + 1] - data[xIndex][yIndex + 1][zIndex + 1]);

            // interpolate Y-axis
            double p5, p6;
            p5 = p1 + (y - (yInterval * yIndex)) / yInterval * (p2 - p1);
            p6 = p3 + (y - (yInterval * yIndex)) / yInterval * (p4 - p3);

            // interpolate Z-axis
            result = p5 + (z - (zInterval * zIndex)) / zInterval * (p6 - p5);
            return result;
        }

        public static ColorCalculator.RGBColor getTriLinearInterpolation(ColorCalculator.RGBColor[][][] data, double x, double y, double z)
        {
            ColorCalculator.RGBColor result;
            // crop input range
            if (x > 1)
            {
                x = 1;
            }
            else if (x < 0)
            {
                x = 0;
            }

            if (y > 1)
            {
                y = 1;
            }
            else if (y < 0)
            {
                y = 0;
            }

            if (z > 1)
            {
                z = 1;
            }
            else if (z < 0)
            {
                z = 0;
            }

            // find cubic 
            double xInterval = 1.0f / data.GetLength(0);
            double yInterval = 1.0f / data.GetLength(1);
            double zInterval = 1.0f / data.GetLength(2);

            int xIndex = (int)(x / xInterval);
            int yIndex = (int)(y / yInterval);
            int zIndex = (int)(z / zInterval);

            //interpolate X-axis
            ColorCalculator.RGBColor p1, p2, p3, p4;
            double tempR, tempG, tempB;
            tempR = data[xIndex][yIndex][zIndex].R +
                    (x - (xInterval * xIndex)) / xInterval *
                    (data[xIndex + 1][yIndex][zIndex].R - data[xIndex][yIndex][zIndex].R);
            tempG = data[xIndex][yIndex][zIndex].G +
                    (x - (xInterval * xIndex)) / xInterval *
                    (data[xIndex + 1][yIndex][zIndex].G - data[xIndex][yIndex][zIndex].G);
            tempB = data[xIndex][yIndex][zIndex].B +
                    (x - (xInterval * xIndex)) / xInterval *
                    (data[xIndex + 1][yIndex][zIndex].B - data[xIndex][yIndex][zIndex].B);
            p1 = new ColorCalculator.RGBColor((int)Math.Round(tempR, 0, MidpointRounding.AwayFromZero),
                                              (int)Math.Round(tempG, 0, MidpointRounding.AwayFromZero),
                                              (int)Math.Round(tempB, 0, MidpointRounding.AwayFromZero));

            tempR = data[xIndex][yIndex + 1][zIndex].R +
                    (x - (xInterval * xIndex)) / xInterval *
                    (data[xIndex + 1][yIndex + 1][zIndex].R - data[xIndex][yIndex + 1][zIndex].R);
            tempG = data[xIndex][yIndex + 1][zIndex].G +
                    (x - (xInterval * xIndex)) / xInterval *
                    (data[xIndex + 1][yIndex + 1][zIndex].G - data[xIndex][yIndex + 1][zIndex].G);
            tempB = data[xIndex][yIndex + 1][zIndex].B +
                    (x - (xInterval * xIndex)) / xInterval *
                    (data[xIndex + 1][yIndex + 1][zIndex].B - data[xIndex][yIndex + 1][zIndex].B);
            p2 = new ColorCalculator.RGBColor((int)Math.Round(tempR, 0, MidpointRounding.AwayFromZero),
                                              (int)Math.Round(tempG, 0, MidpointRounding.AwayFromZero),
                                              (int)Math.Round(tempB, 0, MidpointRounding.AwayFromZero));

            tempR = data[xIndex][yIndex][zIndex + 1].R +
                    (x - (xInterval * xIndex)) / xInterval *
                    (data[xIndex + 1][yIndex][zIndex + 1].R - data[xIndex][yIndex][zIndex + 1].R);
            tempG = data[xIndex][yIndex][zIndex + 1].G +
                    (x - (xInterval * xIndex)) / xInterval *
                    (data[xIndex + 1][yIndex][zIndex + 1].G - data[xIndex][yIndex][zIndex + 1].G);
            tempB = data[xIndex][yIndex][zIndex + 1].B +
                    (x - (xInterval * xIndex)) / xInterval *
                    (data[xIndex + 1][yIndex][zIndex + 1].B - data[xIndex][yIndex][zIndex + 1].B);
            p3 = new ColorCalculator.RGBColor((int)Math.Round(tempR, 0, MidpointRounding.AwayFromZero),
                                              (int)Math.Round(tempG, 0, MidpointRounding.AwayFromZero),
                                              (int)Math.Round(tempB, 0, MidpointRounding.AwayFromZero));

            tempR = data[xIndex][yIndex + 1][zIndex + 1].R +
                    (x - (xInterval * xIndex)) / xInterval *
                    (data[xIndex + 1][yIndex + 1][zIndex + 1].R - data[xIndex][yIndex + 1][zIndex + 1].R);
            tempG = data[xIndex][yIndex + 1][zIndex + 1].G +
                    (x - (xInterval * xIndex)) / xInterval *
                    (data[xIndex + 1][yIndex + 1][zIndex + 1].G - data[xIndex][yIndex + 1][zIndex + 1].G);
            tempB = data[xIndex][yIndex + 1][zIndex + 1].B +
                    (x - (xInterval * xIndex)) / xInterval *
                    (data[xIndex + 1][yIndex + 1][zIndex + 1].B - data[xIndex][yIndex + 1][zIndex + 1].B);
            p4 = new ColorCalculator.RGBColor((int)Math.Round(tempR, 0, MidpointRounding.AwayFromZero),
                                              (int)Math.Round(tempG, 0, MidpointRounding.AwayFromZero),
                                              (int)Math.Round(tempB, 0, MidpointRounding.AwayFromZero));

            // interpolate Y-axis
            ColorCalculator.RGBColor p5, p6;
            tempR = p1.R + (y - (yInterval * yIndex)) / yInterval * (p2.R - p1.R);
            tempG = p1.G + (y - (yInterval * yIndex)) / yInterval * (p2.G - p1.G);
            tempB = p1.B + (y - (yInterval * yIndex)) / yInterval * (p2.B - p1.B);
            p5 = new ColorCalculator.RGBColor((int)Math.Round(tempR, 0, MidpointRounding.AwayFromZero),
                                              (int)Math.Round(tempG, 0, MidpointRounding.AwayFromZero),
                                              (int)Math.Round(tempB, 0, MidpointRounding.AwayFromZero));

            tempR = p3.R + (y - (yInterval * yIndex)) / yInterval * (p4.R - p3.R);
            tempG = p3.G + (y - (yInterval * yIndex)) / yInterval * (p4.G - p3.G);
            tempB = p3.B + (y - (yInterval * yIndex)) / yInterval * (p4.B - p3.B);
            p6 = new ColorCalculator.RGBColor((int)Math.Round(tempR, 0, MidpointRounding.AwayFromZero),
                                              (int)Math.Round(tempG, 0, MidpointRounding.AwayFromZero),
                                              (int)Math.Round(tempB, 0, MidpointRounding.AwayFromZero));

            // interpolate Z-axis
            tempR = p5.R + (y - (yInterval * yIndex)) / yInterval * (p6.R - p5.R);
            tempG = p5.G + (y - (yInterval * yIndex)) / yInterval * (p6.G - p5.G);
            tempB = p5.B + (y - (yInterval * yIndex)) / yInterval * (p6.B - p5.B);
            result = new ColorCalculator.RGBColor((int)Math.Round(tempR, 0, MidpointRounding.AwayFromZero),
                                              (int)Math.Round(tempG, 0, MidpointRounding.AwayFromZero),
                                              (int)Math.Round(tempB, 0, MidpointRounding.AwayFromZero));
            return result;
        }

    }
}
