using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class Point : System.Object
    {
        public double x;
        public double y;
        public Point()
        {
            x = 0;
            y = 0;
        }

        public Point(double _x, double _y)
        {
            x = _x;
            y = _y;
        }

        public static bool operator ==(Point p1, Point p2)
        {
            return p1.Equals(p2);
        }

        public static bool operator !=(Point p1, Point p2)
        {
            return !(p1 == p2);
        }

        public override int GetHashCode()
        {
            return (int)(x * y);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            Point p = obj as Point;
            if ((System.Object)p == null)
            {
                return false;
            }
            if ((x != p.x) || (y != p.y))
            {
                return false;
            }
            return true;

        }
    }
}
