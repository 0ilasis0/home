using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ONYX.Utilities
{
    public class CalibrationItems
    {
        #region "Gamma Items"
        public static double GAMMA_DICOM = -1.0f;
        public static double GAMMA_DICOM_05 = -0.5f;
        public static double GAMMA_DICOM_15 = -1.5f;
        public static double GAMMA_FS = -2.0f;
        #endregion

        public const int CALIBRATION_TYPE_GAMMA_ONE_CURVE = 1;
        public const int CALIBRATION_TYPE_GAMMA_THREE_CURVE = 2;
        public const int CALIBRATION_TYPE_COLOR_TEMPERATURE = 3;
        public const int CALIBRATION_TYPE_ALS = 4;
        public const int CALIBRATION_TYPE_FRONT_SENSOR = 5;
        public const int CALIBRATION_TYPE_COLOR_FRONT_SENSOR = 6;
        public const int CALIBRATION_TYPE_UNIFORMITY = 7;
        public const int CALIBRATION_TYPE_FAST_COLOR_TEMPERATURE = 8;
        public const int CALIBRATION_TYPE_3D_LUT = 9;   // for color mapping
        public const int CALIBRATION_TYPE_GAMUT = 10;
        public const int CALIBRATION_TYPE_WIDE_UNIFORMITY = 11;

        public static readonly string DICOM_GAMMA = "DICOM";
        public static readonly string DICOM_GAMMA_05 = "DICOM 0.5";
        public static readonly string DICOM_GAMMA_15 = "DICOM 1.5";
        public static readonly string DICOM_GAMMA_FS = "DICOM FS";

        public string name;
        public double target;
        public byte switchIndex;
        public byte downloadIndex;
        public int type;

        public CalibrationItems(string _name, double _target, byte sIndex, byte dIndex, int _type)
        {
            name = _name;
            target = _target;
            switchIndex = sIndex;
            downloadIndex = dIndex;
            type = _type;
        }
    }
}
