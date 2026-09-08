using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class ColorData : System.Object
    {
        public static int MODE_XYZ = 0;
        public static int MODE_xyY = 1;
        private double mX;
        private double mY;
        private double mZ;

        private double mx;
        private double my;
        private double mz;

        public string SensorId;
        public string Note;
        // --------------------------------------------
        // Measure Pattern Level
        // --------------------------------------------

        /// <summary>Measure Pattern Level R</summary>
        public int LevelR;

        /// <summary>Measure Pattern Level G</summary>
        public int LevelG;

        /// <summary>Measure Pattern Level B</summary>
        public int LevelB;

        public ColorData ShallowCopy()
        {
            return (ColorData)this.MemberwiseClone();
        }

        public ColorData()
        {
            SensorId = Note = string.Empty;
        }

        public ColorData(double data1, double data2, double data3, int mode)
        {
            if (MODE_XYZ == mode)
            {
                mX = data1;
                mY = data2;
                mZ = data3;
                if ((0 == mX) || (0 == mZ))
                {
                    mX = 0;
                    mZ = 0;
                    mx = 0;
                    my = 0;
                    mz = 0;
                }
                else
                {
                    mx = mX / (mX + mY + mZ);
                    my = mY / (mX + mY + mZ);
                    mz = mZ / (mX + mY + mZ);
                }
            }
            else
            {
                mx = data1;
                my = data2;
                mY = data3;
                if ((0 == mx) || (0 == my))
                {
                    mx = 0;
                    my = 0;
                    mz = 0;
                    mX = 0;
                    mZ = 0;
                }
                else
                {
                    mz = 1 - mx - my;
                    mX = mx / my * mY;
                    mZ = mz / my * mY;
                }
            }
        }

        public ColorData(double CT, double Y)
        {
            double normalizeCT = CT / 1000;
            if (0 == normalizeCT)
            {
                mx = 0;
                my = 0;
                mz = 0;
                mX = 0;
                mY = Y;
                mZ = 0;
            }
            else if (normalizeCT < 4)
            {
                throw new Exception("The Color Temperature is out of range");
            }
            else if (normalizeCT <= 7)
            {
                mx = (-4.6070 / Math.Pow(normalizeCT, 3)) + (2.9678 / Math.Pow(normalizeCT, 2)) + (0.09911 / normalizeCT) + 0.244063;
                my = -3 * Math.Pow(mx, 2) + 2.87 * mx - 0.275;
                mz = 1 - mx - my;

                mX = mx / my * Y;
                mY = Y;
                mZ = mz / my * Y;
            }
            else if (normalizeCT <= 25)
            {
                mx = (-2.0064 / Math.Pow(normalizeCT, 3)) + (1.9018 / Math.Pow(normalizeCT, 2)) + (0.24748 / normalizeCT) + 0.237040;
                my = -3 * Math.Pow(mx, 2) + 2.87 * mx - 0.275;
                mz = 1 - mx - my;

                mX = mx / my * Y;
                mY = Y;
                mZ = mz / my * Y;
            }
            else
            {
                throw new Exception("The Color Temperature is out of range");
            }
        }

        public double X
        {
            get { return mX; }
        }

        public double Y
        {
            set { mY = value; }
            get { return mY; }
        }

        public double Z
        {
            get { return mZ; }
        }

        public double x
        {
            get { return mx; }
        }

        public double y
        {
            get { return my; }
        }

        public double z
        {
            get { return mz; }
        }

        public double[] xyYtoArray()
        {
            double[] result = new double[3];
            result[0] = mx;
            result[1] = my;
            result[2] = mY;
            return result;
        }

        public double[] XYZtoArray()
        {
            double[] result = new double[3];
            result[0] = mX;
            result[1] = mY;
            result[2] = mZ;
            return result;
        }

        public double CT
        {
            get { return ColorTemperatureCalculator.xy2CT(mx, my); }
        }

        public static ColorData operator -(ColorData colorA, ColorData colorB)
        {
            double DX, DY, DZ;
            DX = colorA.mX - colorB.mX;
            DY = colorA.mY - colorB.mY;
            DZ = colorA.mZ - colorB.mZ;
            return new ColorData(DX, DY, DZ, ColorData.MODE_XYZ);
        }

        public static ColorData operator +(ColorData colorA, ColorData colorB)
        {
            double DX, DY, DZ;
            DX = colorA.mX + colorB.mX;
            DY = colorA.mY + colorB.mY;
            DZ = colorA.mZ + colorB.mZ;
            return new ColorData(DX, DY, DZ, ColorData.MODE_XYZ);
        }

        public ColorData normalize(double NormalizeValue)
        {
            double normalizeX = mX / NormalizeValue;
            double normalizeY = mY / NormalizeValue;
            double normalizeZ = mZ / NormalizeValue;
            return new ColorData(normalizeX, normalizeY, normalizeZ, ColorData.MODE_XYZ);
        }

        public static ColorData operator /(double value, ColorData data)
        {
            double X = data.X / value;
            double Y = data.Y / value;
            double Z = data.Z / value;
            return new ColorData(X, Y, Z, ColorData.MODE_XYZ);
        }

        public static ColorData operator /(ColorData data, double value)
        {
            double X = data.X / value;
            double Y = data.Y / value;
            double Z = data.Z / value;
            return new ColorData(X, Y, Z, ColorData.MODE_XYZ);
        }

        public static ColorData operator *(double value, ColorData data)
        {
            double X = data.X * value;
            double Y = data.Y * value;
            double Z = data.Z * value;
            return new ColorData(X, Y, Z, ColorData.MODE_XYZ);
        }

        public static ColorData operator *(ColorData data, double value)
        {
            double X = data.X * value;
            double Y = data.Y * value;
            double Z = data.Z * value;
            return new ColorData(X, Y, Z, ColorData.MODE_XYZ);
        }

        public void adjustY(double offset)
        {
            mY = mY + offset;

            mz = 1 - mx - my;
            mX = mx / my * mY;
            mZ = mz / my * mY;
        }
    }

    public class stxF3_0006_0012
    {
        public float Lamp = 0.0f;
        public float Temp1 = 0.0f;
        public float Temp2 = 0.0f;
        public float BRC = 0.0f;
    }

}
