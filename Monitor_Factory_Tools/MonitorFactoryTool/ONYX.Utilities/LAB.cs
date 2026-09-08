using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class LAB
    {
        public const int REF_WHITE_D50 = 0;
        public const int REF_WHITE_D65 = 1;
        public const int REF_WHITE_D75 = 2;

        private double m_l = 100;
        private double m_a = 0;
        private double m_b = 0;

        private double[,] m_refWhite = {{0.964220, 1.0, 0.825210},
                                        {0.950470, 1.0, 1.088830},
                                        {0.949720, 1.0, 1.226380}};

        private double[] mDefaultxy = { 0.31271, 0.32902 };
        private double[] mDefaultXYZ = { 0.950470, 1.0, 1.088830 };

        public LAB(ColorData input, int refWhite)
        {
            translate(input.X, input.Y, input.Z, refWhite);
        }

        public LAB(ColorData input, int refWhite, double refLum)
        {
            translate(input.X, input.Y, input.Z, refWhite, refLum);
        }

        public LAB(ColorData input, ColorData refer)
        {
            translate(input.X, input.Y, input.Z, refer.X, refer.Y, refer.Z);
        }

        public LAB(double x, double y, double Y, int refWhite, int dummy)
        {
            double X = 0;
            double Z = 0;
            if (x == 0 || y == 0)
            {
                x = mDefaultxy[0];
                y = mDefaultxy[1];
            }
            X = x / y * Y;
            Z = (1 - x - y) / y * Y;
            translate(X, Y, Z, refWhite);
        }

        public LAB(double x, double y, double Y, int refWhite, int dummy, double refLum)
        {
            double X = 0;
            double Z = 0;
            if (x == 0 || y == 0)
            {
                x = mDefaultxy[0];
                y = mDefaultxy[1];
            }
            X = x / y * Y;
            Z = (1 - x - y) / y * Y;
            translate(X, Y, Z, refWhite, refLum);
        }

        public LAB(double X, double Y, double Z, int refWhite)
        {
            translate(X, Y, Z, refWhite);
        }

        public LAB(double X, double Y, double Z, int refWhite, double refLum)
        {
            translate(X, Y, Z, refWhite, refLum);
        }

        public double L
        {
            get { return m_l; }
            set { m_l = value; }
        }

        public double A
        {
            get { return m_a; }
            set { m_a = value; }
        }

        public double B
        {
            get { return m_b; }
            set { m_b = value; }
        }

        private void translate(double X, double Y, double Z, int refWhite)
        {
            const double e = 0.008856;
            const double k = 903.3;

            double Xr = 0;
            double Yr = 0;
            double Zr = 0;

            double xr = 0;
            double yr = 0;
            double zr = 0;

            double fx, fy, fz;

            if (X == 0)
            {
                X = mDefaultXYZ[0] * Y;
            }

            if (Z == 0)
            {
                Z = mDefaultXYZ[2] * Y;
            }

            Xr = m_refWhite[refWhite, 0] * Y;
            Yr = m_refWhite[refWhite, 1] * Y;
            Zr = m_refWhite[refWhite, 2] * Y;

            xr = X / Xr;
            yr = Y / Yr;
            zr = Z / Zr;

            if (xr > e)
            {
                fx = Math.Pow(xr, 1.0f / 3);
            }
            else
            {
                fx = (k * xr + 16) / 116;
            }

            if (yr > e)
            {
                fy = Math.Pow(yr, 1.0f / 3);
            }
            else
            {
                fy = (k * yr + 16) / 116;
            }

            if (zr > e)
            {
                fz = Math.Pow(zr, 1.0f / 3);
            }
            else
            {
                fz = (k * zr + 16) / 116;
            }
            m_l = 116 * fy - 16;
            m_a = 500 * (fx - fy);
            m_b = 200 * (fy - fz);
        }

        private void translate(double X, double Y, double Z, int refWhite, double refY)
        {
            const double e = 0.008856;
            const double k = 903.3;

            double Xr = m_refWhite[refWhite, 0] * refY;
            double Yr = m_refWhite[refWhite, 1] * refY;
            double Zr = m_refWhite[refWhite, 2] * refY;

            double xr = 0;
            double yr = 0;
            double zr = 0;

            double fx, fy, fz;

            if (X == 0)
            {
                X = mDefaultXYZ[0] * Y;
            }

            if (Z == 0)
            {
                Z = mDefaultXYZ[2] * Y;
            }

            xr = X / Xr;
            yr = Y / Yr;
            zr = Z / Zr;

            if (xr > e)
            {
                fx = Math.Pow(xr, 1.0f / 3);
            }
            else
            {
                fx = (k * xr + 16) / 116;
            }

            if (yr > e)
            {
                fy = Math.Pow(yr, 1.0f / 3);
            }
            else
            {
                fy = (k * yr + 16) / 116;
            }

            if (zr > e)
            {
                fz = Math.Pow(zr, 1.0f / 3);
            }
            else
            {
                fz = (k * zr + 16) / 116;
            }
            m_l = 116 * fy - 16;
            m_a = 500 * (fx - fy);
            m_b = 200 * (fy - fz);
        }   
 
        private void translate(double X, double Y, double Z, double Xr, double Yr, double Zr)
        {
            const double e = 0.008856;
            const double k = 903.3;

            double xr = 0;
            double yr = 0;
            double zr = 0;

            double fx, fy, fz;

            if (Xr == 0)
            {
                Xr = mDefaultXYZ[0] * Yr;
            }
            if (Zr == 0)
            {
                Zr = mDefaultXYZ[2] * Yr;
            }

            if (X == 0)
            {
                X = mDefaultXYZ[0] * Y;
            }

            if (Z == 0)
            {
                Z = mDefaultXYZ[2] * Y;
            }

            xr = X / Xr;
            yr = Y / Yr;
            zr = Z / Zr;

            if (xr > e)
            {
                fx = Math.Pow(xr, 1.0f / 3);
            }
            else
            {
                fx = (k * xr + 16) / 116;
            }

            if (yr > e)
            {
                fy = Math.Pow(yr, 1.0f / 3);
            }
            else
            {
                fy = (k * yr + 16) / 116;
            }

            if (zr > e)
            {
                fz = Math.Pow(zr, 1.0f / 3);
            }
            else
            {
                fz = (k * zr + 16) / 116;
            }
            m_l = 116 * fy - 16;
            m_a = 500 * (fx - fy);
            m_b = 200 * (fy - fz);
        }
    }
}
