using System;
using System.Runtime.InteropServices;

namespace OnyxSensor
{
    public abstract class SensorBase
    {
        public abstract bool Open();
        public abstract bool Close();
        public abstract string SensorName { get; }
        public abstract bool Config();
        public abstract SensorMeasure_t Measure();
        public abstract bool MeasYxyz(out SensorMeasureYxy_t result);

    }
    public struct SensorMeasure_t
    {
        public double R;
        public double G;
        public double B;

        public double X;
        public double Y;
        public double Z;

        public double x;
        public double y;
        public double z;

        public double ud;
        public double vd;
        public double CT;
        public double duv;
        public double ColorTemperature;
        public string Note;
    }

    public struct SensorMeasureYxy_t
    {
        public double Y;       /**< Y luminance data in Cd/m2, or Lux */
        public double x;       /**< x chrominance data */
        public double y;       /**< y chrominance data */
        public double z;		/**< z (internal use for computation purposes - Applications should not rely on this element to always be valid) */
    }
}


