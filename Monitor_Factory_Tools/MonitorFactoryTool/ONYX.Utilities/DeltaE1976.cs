using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class DeltaE1976
    {
        public static double DeltaL(LAB color1, LAB color2)
        {
            return Math.Abs(color2.L - color1.L);
        }

        public static double DeltaAB(LAB color1, LAB color2)
        {
            return Math.Sqrt(Math.Pow((color2.A - color1.A), 2) +
                             Math.Pow((color2.B - color1.B), 2));
        }

        public static double DeltaE(LAB color1, LAB color2)
        {
            return Math.Sqrt(Math.Pow((color2.L - color1.L), 2) +
                             Math.Pow((color2.A - color1.A), 2) +
                             Math.Pow((color2.B - color1.B), 2));
        }
    }
}
