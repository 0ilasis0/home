using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Drawing;

namespace ONYX.Utilities
{
    public class DrawCIE1931ColorSpace
    {

        public class NTSCsystem
        {
            public static double xRed = 0.67;
            public static double yRed = 0.33;
            public static double xGreen = 0.21;
            public static double yGreen = 0.71;
            public static double xBlue = 0.14;
            public static double yBlue = 0.08;
            public static double xWhite = 0.310;
            public static double yWhite = 0.316;
        }

        private void DrawHorseArea(DataTable dt, Graphics gg, Image img)
        {
            double x = 0, y = 0, z = 0;
            double[] xy;
            int[] rgb = new Int32[3];

            for (int i = 0; i <= img.Width; i += 1)
            {
                for (int j = 0; j <= img.Height; j += 1)
                {
                    xy = PixelToCoordinate(i, j, img.Width, img.Height);
                    x = xy[0];
                    y = xy[1];
                    z = 1 - x - y;

                    if (IsInsideHorseShape(dt, x, y) == true)
                    {
                        rgb = rgbData(x, y, z);
                        SolidBrush s = new SolidBrush(Color.FromArgb(rgb[0], rgb[1], rgb[2]));
                        RectangleF rec = new RectangleF(i, img.Height - j, 1, 1);
                        gg.FillRectangle(s, rec);
                    }
                }
            }
        }

        private void DrawBoundary(DataTable dt, Graphics gg, Image img)
        {
            int lamda = 0;
            double StartPointX = 0, StartPointY = 0, StartPointZ = 0;
            double EndPointX = 0, EndPointY = 0, EndPointZ = 0;
            double PositionX = 0, PositionY = 0;
            int[] rgb = new Int32[3];

            //繪製馬蹄形邊界
            for (int i = 0; i < dt.Rows.Count - 1; i++)
            {
                StartPointX = double.Parse(dt.Rows[i]["x"].ToString());
                StartPointY = double.Parse(dt.Rows[i]["y"].ToString());
                StartPointZ = double.Parse(dt.Rows[i]["z"].ToString());

                EndPointX = double.Parse(dt.Rows[i + 1]["x"].ToString());
                EndPointY = double.Parse(dt.Rows[i + 1]["y"].ToString());
                EndPointZ = double.Parse(dt.Rows[i + 1]["z"].ToString());

                rgb = rgbData(StartPointX, StartPointY, StartPointZ);

                SolidBrush s = new SolidBrush(Color.FromArgb(rgb[0], rgb[1], rgb[2]));
                gg.DrawLine(new Pen(s, 1), (float)StartPointX * img.Width, img.Height - (float)StartPointY * img.Height, (float)EndPointX * img.Width, img.Height - (float)EndPointY * img.Height);
            }

            //波長從420nm到680nm，繪製mark
            for (int i = 60; i <= 320; i++)
            {
                lamda = int.Parse(dt.Rows[i]["Lamda"].ToString());

                if (lamda % 10 == 0)//每10nm繪製mark
                {
                    if (lamda >= 460 && lamda <= 630)//430nm到450nm以及640nm到670nm不繪製，避免重疊
                    {
                        PositionX = double.Parse(dt.Rows[i]["x"].ToString());
                        PositionY = double.Parse(dt.Rows[i]["y"].ToString());
                        SolidBrush s = new SolidBrush(Color.White);
                        gg.DrawLine(new Pen(s), (float)PositionX * img.Width, img.Height - (float)PositionY * img.Height, (float)PositionX * img.Width, img.Height - (float)PositionY * img.Height - 10);
                        gg.DrawString(lamda.ToString(), new Font("Arial", 7), s, (float)PositionX * img.Width - 5, img.Height - (float)PositionY * img.Height - 20);
                    }

                    if (lamda == 420 || lamda == 680)
                    {
                        PositionX = double.Parse(dt.Rows[i]["x"].ToString());
                        PositionY = double.Parse(dt.Rows[i]["y"].ToString());
                        SolidBrush s = new SolidBrush(Color.White);
                        gg.DrawLine(new Pen(s), (float)PositionX * img.Width, img.Height - (float)PositionY * img.Height, (float)PositionX * img.Width, img.Height - (float)PositionY * img.Height - 10);
                        gg.DrawString(lamda.ToString(), new Font("Arial", 7), s, (float)PositionX * img.Width - 5, img.Height - (float)PositionY * img.Height - 20);
                    }
                }
            }
        }

        private void DrawBottomLine(DataTable dt, Graphics gg, Image img)
        {
            double x = 0, y = 0, z = 0;
            double x1 = 0, y1 = 0, z1 = 0;
            int[] rgb = new Int32[3];

            double minWavelengthXCoordinate = 0;
            double minWavelengthYCoordinate = 0;
            double maxWavelengthXCoordinate = 0;
            double maxWavelengthYCoordinate = 0;

            //360nm (x,y)
            minWavelengthXCoordinate = double.Parse(dt.Rows[0]["x"].ToString());
            minWavelengthYCoordinate = double.Parse(dt.Rows[0]["y"].ToString());

            //830nm (x,y)
            maxWavelengthXCoordinate = double.Parse(dt.Rows[dt.Rows.Count - 1]["x"].ToString());
            maxWavelengthYCoordinate = double.Parse(dt.Rows[dt.Rows.Count - 1]["y"].ToString());

            double XCoordinateDistance = maxWavelengthXCoordinate - minWavelengthXCoordinate;
            double YCoordinateDistance = maxWavelengthYCoordinate - minWavelengthYCoordinate;
            double Slope = YCoordinateDistance / XCoordinateDistance;

            for (double i = minWavelengthXCoordinate; i < maxWavelengthXCoordinate; i += 0.002)
            {
                //直線方程式: y = (x - xmin) * Slope + ymin
                x = i;
                y = (x - minWavelengthXCoordinate) * Slope + minWavelengthYCoordinate;
                z = 1 - x - y;

                x1 = i + 0.002;
                y1 = (x1 - minWavelengthXCoordinate) * Slope + minWavelengthYCoordinate;
                z1 = 1 - x1 - y1;

                rgb = rgbData(x, y, z);

                SolidBrush s = new SolidBrush(Color.FromArgb(rgb[0], rgb[1], rgb[2]));
                gg.DrawLine(new Pen(s, 1), (float)x * img.Width, img.Height - (float)y * img.Height, (float)x1 * img.Width, img.Height - (float)y1 * img.Height);
            }
        }

        private double[] PixelToCoordinate(int x, int y, int width, int height)
        {
            double[] coordinate = new double[2];
            double xc, yc;
            xc = (double)x / (double)width;
            yc = (double)y / (double)height;
            coordinate[0] = xc;
            coordinate[1] = yc;
            return coordinate;
        }

        private bool IsInsideHorseShape(DataTable dt, double xc, double yc)
        {
            bool inside = false;
            double dx = 0, dy = 0;
            double n = 0, y = 0;
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0;

            try
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    x1 = double.Parse(dt.Rows[i]["x"].ToString());
                    y1 = double.Parse(dt.Rows[i]["y"].ToString());
                    x2 = double.Parse(dt.Rows[(i + 1) % dt.Rows.Count]["x"].ToString());
                    y2 = double.Parse(dt.Rows[(i + 1) % dt.Rows.Count]["y"].ToString());

                    dx = x2 - x1;
                    dy = y2 - y1;

                    //m = dy / dx;
                    //通過兩點的直線方程式:P1=(x1,y1),P2=(x2,y2),Slope=m
                    //m * (x-x1) - (y-y1) = 0;
                    //m = (y-y1)/(x-x1) = (y2-y1)/(x2-x1)
                    //相似三角形比例n = (x-x1)/(x2-x1) = (y-y1)/(y2-y1) = (x-x1)/dx = (y-y1)/dy

                    if ((xc < x1 || xc < x2) && (yc >= y1 || yc >= y2))
                    {
                        n = (xc - x1) / dx;
                        y = (n * dy) + y1;
                        if ((y <= yc) && n <= 1 && n >= 0) inside = !inside;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

            return inside;
        }

        private int[] rgbData(double x, double y, double z)
        {
            int[] rgb = new Int32[3];
            int r, g, b;
            double R, G, B;

            ArrayList aryRGB = new ArrayList();
            aryRGB = XYZtoRGB(x, y, z);
            
            R = Clamp((double)aryRGB[0], 0.0, 1.0);
            G = Clamp((double)aryRGB[1], 0.0, 1.0);
            B = Clamp((double)aryRGB[2], 0.0, 1.0);

            double maxRGB = MaxRGB(R, G, B);

            r = (Int32)Math.Round((255 * R / maxRGB));
            g = (Int32)Math.Round((255 * G / maxRGB));
            b = (Int32)Math.Round((255 * B / maxRGB));

            rgb[0] = r;
            rgb[1] = g;
            rgb[2] = b;

            return rgb;
        } 

        private ArrayList XYZtoRGB(double x, double y, double z)
        {
            ArrayList result = new ArrayList();
            double r, g, b;
            r = 3.2406 * x - 1.5372 * y - 0.4986 * z;
            g = -0.9689 * x + 1.8758 * y + 0.0415 * z;
            b = 0.0557 * x - 0.2040 * y + 1.0570 * z;
            result.Add(r);
            result.Add(g);
            result.Add(b);

            for (int index = 0; index < 3; index++)
            {
                if ((double)result[index] <= 0.031308)
                {
                    result[index] = (double)result[index] * 12.92;
                }
                else
                {
                    result[index] = 1.055 * Math.Pow((double)result[index], 1/2.4) - 0.055;
                }
            }
            return result;
        }

        private double Clamp(double input, double min, double max)
        {
            if (input <= min)
            {
                return min;
            }
            else if (input >= max)
            {
                return max;
            }
            else
            {
                return input;
            }
        }

        private double MaxRGB(double r, double g, double b)
        {
            double max = r;
            if (g >= max)
                max = g;
            if (b >= max)
                max = b;

            return max;
        }

        private DataTable TxtConvertToDataTable(string File, string TableName, string delimiter)
        {
            DataTable dt = new DataTable();
            DataSet ds = new DataSet();
            StreamReader s = new StreamReader(File, System.Text.Encoding.Default);
            string[] columns = s.ReadLine().Split(delimiter.ToCharArray());
            ds.Tables.Add(TableName);
            foreach (string col in columns)
            {
                bool added = false;
                string next = "";
                int i = 0;
                while (!added)
                {
                    string columnname = col + next;
                    columnname = columnname.Replace("#", "");
                    columnname = columnname.Replace("'", "");
                    columnname = columnname.Replace("&", "");

                    if (!ds.Tables[TableName].Columns.Contains(columnname))
                    {
                        ds.Tables[TableName].Columns.Add(columnname.ToUpper());
                        added = true;
                    }
                    else
                    {
                        i++;
                        next = "_" + i.ToString();
                    }
                }
            }

            string AllData = s.ReadToEnd();
            string[] rows = AllData.Split("\n".ToCharArray());

            foreach (string r in rows)
            {
                string[] items = r.Split(delimiter.ToCharArray());
                ds.Tables[TableName].Rows.Add(items);
            }

            s.Close();

            dt = ds.Tables[0];

            return dt;
        }

        public Image draw(int width, int height)
        {
            DataTable da = TxtConvertToDataTable(@".\utilities\CIE1931.csv", "tmp", ",");
            Image img = new Bitmap(width, height);
            //在image上繪圖
            Graphics gg = Graphics.FromImage(img);
            //繪圖品質設定
            gg.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            gg.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            gg.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            gg.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            gg.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            //先將背景色設定為黑色
            gg.FillRectangle(new SolidBrush(Color.Black), 0, 0, width, height);

            //繪製馬蹄形圖
            DrawHorseArea(da, gg, img);
            //繪製馬蹄形邊界圖
            DrawBoundary(da, gg, img);
            //繪製馬蹄形邊界底線圖
            DrawBottomLine(da, gg, img);

            return img;
        }
    }
}
