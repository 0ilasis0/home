using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public static class DeltaE2000
    {
        public static double DeltaE(LAB target, LAB refer)
        {            
            double Lbar = (target.L + refer.L) / 2;
            double C1 = Math.Sqrt(Math.Pow(target.A, 2) + Math.Pow(target.B, 2));
            double C2 = Math.Sqrt(Math.Pow(refer.A, 2) + Math.Pow(refer.B, 2));

            double Cbar = (C1 + C2) / 2;
            double G = 0.5 * (1 - Math.Sqrt(Math.Pow(Cbar, 7) / (Math.Pow(Cbar, 7) + Math.Pow(25, 7))));

            double a1dot = target.A * (1 + G);
            double a2dot = refer.A * (1 + G);

            double C1dot = Math.Sqrt(Math.Pow(a1dot, 2) + Math.Pow(target.B, 2));
            double C2dot = Math.Sqrt(Math.Pow(a2dot, 2) + Math.Pow(target.B, 2));
            double Cbardot = (C1dot + C2dot) / 2;

            double h1dot = Math.Atan2(target.B, a1dot);
            double h2dot = Math.Atan2(refer.B, a2dot);
            double Hbardot = 0;
            if (Math.Abs(h1dot - h2dot) > Math.PI)
            {
                Hbardot = (h1dot + h2dot + 2 * Math.PI) / 2;
            }
            else
            {
                Hbardot = (h1dot + h2dot) / 2;
            }

            double T = 1 - 0.17 * Math.Cos(Hbardot - Math.PI / 6) +
                           0.24 * Math.Cos(2 * Hbardot) +
                           0.32 * Math.Cos(3 * Hbardot + Math.PI / 30) -
                           0.20 * Math.Cos(4 * Hbardot - Math.PI * 63 / 180);
            
            double deltahdot = 0;
            if (Math.Abs(h2dot - h1dot) <= Math.PI)
            {
                deltahdot = h2dot - h1dot;
            }
            else if ((Math.Abs(h2dot - h1dot) > Math.PI) && (h1dot >= h2dot))
            {
                deltahdot = h2dot - h1dot + 2 * Math.PI;
            }
            else
            {
                deltahdot = h2dot - h1dot - 2 * Math.PI;
            }
            double deltaldot = refer.L - target.L;
            double deltaCdot = C2dot - C1dot;
            double deltaHdot = 2 * Math.Sqrt(C1dot * C2dot) * Math.Sin(deltahdot / 2);

            double SL = 1 + ((0.015 * Math.Pow((Lbar - 50), 2)) / 
                             Math.Sqrt(20 + Math.Pow((Lbar - 50), 2)));
            double SC = 1 + 0.045 * Cbardot;
            double SH = 1 + 0.015 * Cbardot * T;

            double deltaTheta = 30 * Math.Exp(-1 * Math.Pow(((deltaHdot - (Math.PI * 275 / 180)) / 25), 2));
            double RC = 2 * Math.Sqrt(Math.Pow(Cbardot, 7) / (Math.Pow(Cbardot, 7) + Math.Pow(25, 7)));
            double RT = -1 * RC * Math.Sin(2 * deltaTheta);
            double deltaE = Math.Sqrt(Math.Pow((deltaldot / SL), 2) +
                                      Math.Pow((deltaCdot / SC), 2) +
                                      Math.Pow((deltaHdot / SH), 2) +
                                      RT * (deltaCdot / SC) * (deltaHdot / SH));

            return deltaE;
        }

        public static double DeltaAB(LAB target, LAB refer)
        {
            refer.L = target.L;
            return DeltaE(target, refer);
        }

        public static double DeltaL(LAB target, LAB refer)
        {
            target.A = refer.A;
            target.B = refer.B;
            return DeltaE(target, refer);
        }
    }

}
