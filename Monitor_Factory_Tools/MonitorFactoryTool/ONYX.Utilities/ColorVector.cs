using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorFactoryTool.ONYX.Utilities
{
    public class ColorVector
    {
        public double rVector { get; set; }
        public double gVector { get; set; }
        public double bVector { get; set; }
        public double MaxLuminance { get; set; }

        public override string ToString()
        {
            return $"r: {rVector:F4}, g: {gVector:F4}, b: {bVector:F4}, MaxY: {MaxLuminance:F2}";
        }
    }


}
