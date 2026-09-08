using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class Vector2D : System.Object
    {
        public double x;
        public double y;

        public Vector2D(double _x, double _y)
        {
            x = _x;
            y = _y;
        }

        public Vector2D(Point p1, Point p2)
        {
            x = p2.x - p1.x;
            y = p2.y - p1.y;
        }

        public double Length()
        {
            return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
        }

        public static Vector2D operator +(Vector2D v1, Vector2D v2)
        {
            Vector2D result = new Vector2D((v1.x + v2.x), (v1.y + v2.y));
            return result;
        }

        public static Vector2D operator -(Vector2D v1, Vector2D v2)
        {
            Vector2D result = new Vector2D((v1.x - v2.x), (v1.y - v2.y));
            return result;
        }

        public static Vector2D operator *(double value, Vector2D v)
        {
            Vector2D result = new Vector2D((value * v.x), (value * v.y));
            return result;
        }

        public static bool operator ==(Vector2D v1, Vector2D v2)
        {
            return ((v1.x == v2.x) && (v1.y == v2.y));
        }

        public static bool operator !=(Vector2D v1, Vector2D v2)
        {
            return !(v1 == v2);
        }

        public static double Dot(Vector2D v1, Vector2D v2)
        {
            double result;
            result = v1.x * v2.x + v1.y * v2.y;
            return result;
        }

        public static double Cross(Vector2D v1, Vector2D v2)
        {
            double result;
            result = v1.x * v2.y - v1.y * v2.x;
            return result;
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

            Vector2D v = obj as Vector2D;
            if ((System.Object)v == null)
            {
                return false;
            }
            if ((x != v.x) || (y != v.y))
            {
                return false;
            }
            return true;
        }

        public Vector2D GetUnitVector()
        {
            Vector2D result = new Vector2D(1, 1);

            result.x = x / this.Length();
            result.y = y / this.Length();

            return result;
        }
    }
}
