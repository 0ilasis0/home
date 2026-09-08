using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class ColorCalculator
    {
        public class CIEColor
        {
            private double mX, mY, mZ, mx, my;
            
            public CIEColor(double _X, double _Y, double _Z)
            {
                mX = _X;
                mY = _Y;
                mZ = _Z;
                mx = mX / (mX + mY + mZ);
                my = mY / (mX + mY + mZ);
            }

            public CIEColor(double _x, double _y, double _Y, byte dummy)
            {
                mx = _x;
                my = _y;
                mY = _Y;
                mX = mx / my * mY;
                mZ = (1 - mx - my) / my * mY;
            }

            public double X
            {
                get
                {
                    return mX;
                }
                set
                {
                    mX = value;
                    mx = mX / (mX + mY + mZ);
                    my = mY / (mX + mY + mZ);
                }
            }

            public double Y
            {
                get
                {
                    return mY;
                }
                set
                {
                    mY = value;
                    mx = mX / (mX + mY + mZ);
                    my = mY / (mX + mY + mZ);
                }
            }
            
            public double Z
            {
                get
                {
                    return mZ;
                }
                set
                {
                    mZ = value;
                    mx = mX / (mX + mY + mZ);
                    my = mY / (mX + mY + mZ);
                }
            }
            
            public double x
            {
                get
                {
                    return mx;
                }
                set
                {
                    mx = value;
                    mX = mx / my * mY;
                    mZ = (1 - mx - my) / my * mY;
                }
            }

            public double y
            {
                get
                {
                    return my;
                }
                set
                {
                    my = value;
                    mX = mx / my * mY;
                    mZ = (1 - mx - my) / my * mY;
                }
            }
        }
        
        public class LabColor
        {
            double ml;
            double ma;
            double mb;
            public LabColor()
            {
                ml = 0;
                ma = 0;
                mb = 0;
            }
            public LabColor(double _l, double _a, double _b)
            {
                ml = _l;
                ma = _a;
                mb = _b;
            }

            public double l
            {
                get
                {
                    return ml;
                }
                set
                {
                    ml = value;
                }
            }

            public double a
            {
                get
                {
                    return ma;
                }
                set
                {
                    ma = value;
                }
            }
            
            public double b
            {
                get
                {
                    return mb;
                }
                set
                {
                    mb = value;
                }
            }
        }

        public class RGBColor
        {
            int m_R;
            int m_G;
            int m_B;

            public RGBColor()
            {
                m_R = 0;
                m_G = 0;
                m_B = 0;
            }
            public RGBColor(int r, int g, int b)
            {
                if(r < 0 || r > 255)
                {
                    r = 0;
                }
                if(g < 0 || g > 255)
                {
                    g = 0;
                }
                if(b < 0 || b > 255)
                {
                    b = 0;
                }
                m_R = r; 
                m_G = g;
                m_B = b;
            }

            public int R
            {
                get
                {
                    return m_R;
                }
                set
                {
                    if (value < 0)
                        m_R = 0;
                    else if (value > 255)
                        m_R = 255;
                    else
                        m_R = value;
                }
            }
            public int G
            {
                get
                {
                    return m_G;
                }
                set
                {
                    if (value < 0)
                        m_G = 0;
                    else if (value > 255)
                        m_G = 255;
                    else
                        m_G = value;
                }
            }
            public int B
            {
                get
                {
                    return m_B;
                }
                set
                {
                    if (value < 0)
                        m_B = 0;
                    else if (value > 255)
                        m_B = 255;
                    else
                        m_B = value;
                }
            }

        }

        public static LabColor CIEtoLAB(CIEColor color)
        {
            LabColor result = new LabColor();
            const double E = 0.008856;
            const double K = 903.3;
            // Reference use D65
            const double Xr = 95.047;
            const double Yr = 100;
	        const double Zr = 108.883;

            double xr = color.X / Xr;
            double yr = color.Y / Yr;
            double zr = color.Z / Zr;

            double fx, fy, fz;

            if(xr > E)
            {
                fx = Math.Pow(xr, 1/3);
            }
            else
            {
                fx = (K * xr + 16) / 116;
            }

            if(yr > E)
            {
                fy = Math.Pow(yr, 1/3);
            }
            else
            {
                fy = (K * yr + 16) / 116;
            }

            if(zr > E)
            {
                fz = Math.Pow(zr, 1/3);
            }
            else
            {
                fz = (K * zr + 16) / 116;
            }
            result.l = 116 * fy -16;
            result.a = 500 * (fx - fy);
            result.b = 200 * (fy - fz);
            return result;
        }

        public static CIEColor LABtoCIE(LabColor color)
        {
            const double E = 0.008856;
            const double K = 903.3;
            // Reference use D65
            const double Xr = 95.047;
            const double Yr = 100;
	        const double Zr = 108.883;

            double fy = (color.l + 16) / 116;
            double fx = color.a / 500 + fy;
            double fz = fy - color.b / 200;

            double xr, yr, zr;

            if(Math.Pow(fx, 3) > E)
            {
                xr = Math.Pow(fx, 3);
            }
            else
            {
                xr = (116 * fx - 16) / K;
            }

            if(color.l > K * E)
            {
                yr = Math.Pow((color.l + 16) / 116, 3);
            }
            else
            {
                yr = color.l / K;
            }
        
            if(Math.Pow(fz, 3) > E)
            {
                zr = Math.Pow(fz, 3);
            }
            else
            {
                zr = (116 * fz - 16) / K;
            }
        
            CIEColor result = new CIEColor(xr * Xr, yr * Yr, zr * Zr);
            return result;
        }
    }
}
