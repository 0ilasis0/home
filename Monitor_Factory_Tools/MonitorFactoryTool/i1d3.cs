using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Management.Instrumentation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Navigation;


namespace OnyxSensor
{
    enum i1d3Status_t : int
    {
        i1d3Success = 0,        /**< Function Succeeded 		*/
        i1d3Err = -100, /**< Nonspecific error 			*/
        i1d3ErrInvalidDevicePtr = -101, /**< Device pointer is NULL		*/
        i1d3ErrNoDeviceFound = -102,    /**< No device found			*/

        // Errors passed through from calibrator class
        i1d3ErrFunctionNotAvailable = -504, /**< The requested Function is not supported by this device */
        i1d3ErrLockedCalibrator = -505, /**< The device is password-locked */
        i1d3ErrCalibratorAlreadyOpen = -508,    /**< The device is currently initialized */
        i1d3ErrCalibratorNotOpen = -509,    /**< No device is currently initialized */
        i1d3ErrTransactionError = -510, /**< The communications are out of sync  */
        i1d3ErrWrongDiffuserPosition = -512,    /**< The diffuser arm is in the wrong position for measurement */
        i1d3ErrIncorrectChecksum = -513,    /**< The calculated checksum is incorrect */
        i1d3ErrInvalidParameter = -517, /**< An invalid parameter was passed into the routine */
        i1d3ErrCalibratorError = -519,  /**< The device returned an error */
        i1d3ErrObsoleteFirmware = -520, /**< The firmware is obsolete */
        i1d3ErrCouldNotEnterBLMode = -521,   /**< Error entering bootloader mode */
        i1d3ErrUSBTimeout = -522,   /**< USB timed out waiting for response from device */
        i1d3ErrUSBCommError = -523, /**< USB communication error */
        i1d3ErrEEPROMWriteProtected = -524, /**< EEPROM-write protection error */

        // Errors passed through from matrix generator class
        i1d3ErrMGBadFile = -600,    /**< Couldn't open file */
        i1d3ErrMGTooFewColors = -601,   /**< Must have at least 3 colors in EDR file.*/
        i1d3ErrMGBadWavelengthIncrement = -602, /**< Currently we require 1nm wavelength increment*/
        i1d3ErrMGBadWavelengthEnd = -603,   /**< Currently we require up to at least 730nm.*/
        i1d3ErrMGBadWavelengthStart = -604, /**< Currently we require start at 380nm.*/
        i1d3ErrNoCMFFile = -605,    /**< Couldn't open CMF data file */
        i1d3ErrCMFFormatError = -606,   /**< Couldn't parse CMF data file */

        // Errors passed through from EDR Support class
        i1d3ErrEDRFileNotOpen = -700,   /**< Must open file before making other requests. */
        i1d3ErrEDRFileAlreadyOpen = -701,   /**< File already opened, close to open another file. */
        i1d3ErrEDRFileNotFound = -702,  /**< File not found. */
        i1d3ErrEDRSizeError = -703, /**< File too short. */
        i1d3ErrEDRHeaderError = -704,   /**< Header didn't have correct signature or file too short. */
        i1d3ErrEDRDataError = -705, /**< Data didn't load properly. */
        i1d3ErrEDRDataSignatureError = -706,    /**< Signature didn't match - corrupted file? */
        i1d3ErrEDRSpectralDataSignatureError = -707,    /**< Signature didn't match - corrupted file? */
        i1d3ErrEDRIndexTooHigh = -708,  /**< Requested more color data than available */
        i1d3ErrEDRNoYxyData = -709, /**< Can't request tri-stimulus */
        i1d3ErrEDRNoSpectralData = -710,    /**< Can't request spectral data in file without it. */
        i1d3ErrEDRNoWavelengthData = -711,  /**< No spectral data in file - in response to GetWavelengths */
        i1d3ErrEDRFixedWavelengths = -712,  /**< Evenly-spaced wavelengths */
        i1d3ErrEDRWavelengthTable = -713,   /**< Wavelengths are from table */
        i1d3ErrEDRParameterError = -714,    /**< Probably a null pointer to a call */

        // Errors returned from i1Display3 devices
        i1d3ErrHW_Locked = 16,      /**< i1Display3 is Locked */                            //0x10
        i1d3ErrHW_I2CLowClock = 80,     /**< EEPROM access error: clock is low */                //0x50
        i1d3ErrHW_NACKReceived = 81,        /**< EEPROM access error: NACK received */                //0x51
        i1d3ErrHW_EEAddressInvalid = 96,        /**< Invalid EEPROM address */                            //0x60
        i1d3ErrHW_InvalidCommand = 128, /**< Invalid command to i1Display3 */                    //0x80
        i1d3ErrHW_WrongDiffuserPosition = 129,  /**< Diffuser is in wrong positon for measurement */    //0x81

        // Errors returned from i1Display3 Rev. B devices / i1d3DC devices
        i1d3ErrHW_InvalidParameter = 130,    /**< Invalid parameter passed to device */              //0x82
        i1d3ErrHW_PeriodeTimeOut = 131,    /**< Period measurement timed out */                    //0x83
        i1d3ErrHW_InvalidMeasurement = 132,    /**< No valid measurement data for get Yxy function */  //0x84
        i1d3ErrHW_MatrixChecksum = 144,    /**< Matrix is missing or corrupt */                    //0x90
        i1d3ErrHW_MatrixAmbient = 145     /**< Ambient matrix is missing or corrupt */            //0x91

    }

    public enum i1d3LED_Control_e //zh modify
    {
        // LED Control states
        i1d3LED_OFF = 0,                /**< LEDs are off */
        i1d3LED_FLASH = 1,              /**< LEDs flash */
        i1d3LED_PULSE = 3               /**< LEDs pulse */
    };

    public struct LED_CONFIG_t  //zh modify
    {
        public i1d3LED_Control_e Ctrl;
        public double dOff, dOn;
        public uint uCount;
    }

    public struct i1d3Yxy_t
    {
        public double Y;       /**< Y luminance data in Cd/m2, or Lux */
        public double x;       /**< x chrominance data */
        public double y;       /**< y chrominance data */
        public double z;		/**< z (internal use for computation purposes - Applications should not rely on this element to always be valid) */
    }
    enum i1d3MeasMode_t : int
    {
        i1d3MeasModeCRT = 0,                /**< CRT Measurement mode		*/
        i1d3MeasModeLCD = 1,                    /**< LCD Measurement mode		*/

        // Following modes not supported - maintained for X-Rite internal use only
        i1d3MeasModeLCDsim = 2,             /**< unsupported-for X-Rite internal use only*/
        i1d3MeasModeLCDseq = 3,             /**< unsupported-for X-Rite internal use only*/
        i1d3MeasModeCRTFixed = 4,           /**< unsupported-for X-Rite internal use only*/
        i1d3MeasModeCRTAutoDark = 5,        /**< unsupported-for X-Rite internal use only*/
        i1d3MeasModeLCDsimFixed = 6,        /**< unsupported-for X-Rite internal use only*/

        // New mode added 20Mar12 for plasmas and other pulsing displays
        i1d3MeasModeBurst = 7,              /**<
                                        Burst mode should be used for measuring plasma,
										CRTs and other pulsating displays. It provides more
										accurate results than standard CRT measurement mode
										especially on darker color patches.*/

        // New measurement mode added 17Apr14 for i1Display3 devices with firmware v2.28 and later
        i1d3MeasModeAIO = 8                 /**<
                                        AIO (All In One) mode provides better and faster results on any given
                                        display type. However it can only be used with firmware v2.16 (and later).
                                        For backward compatibility the old modes will still work. However using
                                        AIO mode is recommended for use with all display types. AIO mode is available
                                        as of firmware v2.28 (and later). AIO mode will also consider the integration
                                        time that has been set.

										\attention If i1d3MeasModeAIO is used with firmware prior to v2.28
										i1d3ErrFunctionNotAvailable will be returned.*/
    }


    public class i1d3 : SensorBase
    {
        private static i1d3 instance = null;
        private const string LTDLL_NAME = "i1d3SDK.dll";
        // private IntPtr i1d3Handle;

        //[DllImport(LTDLL_NAME, EntryPoint = "i1d3Initialize", CallingConvention = CallingConvention.Cdecl)]
        //[DllImport(LTDLL_NAME, EntryPoint = "i1d3Destroy", CallingConvention = CallingConvention.Cdecl)]
        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern i1d3Status_t i1d3Initialize();
        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern i1d3Status_t i1d3Destroy();
        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern i1d3Status_t i1d3GetNumberOfDevices();
        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern i1d3Status_t i1d3DeviceOpen(IntPtr devHndl);
        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern i1d3Status_t i1d3DeviceQuickOpen(IntPtr devHndl);

        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern i1d3Status_t i1d3GetDeviceHandle(uint whichDevice, ref IntPtr devHndl);
        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern i1d3Status_t i1d3MeasureYxy(IntPtr devHndl, ref i1d3Yxy_t dXYZmeas);
        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern i1d3Status_t i1d3OverrideDeviceDefaults(uint vid, uint pid, byte[] productkey);
        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern i1d3Status_t i1d3SetMeasurementMode(IntPtr devHndl, i1d3MeasMode_t measMode);
        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern i1d3Status_t i1d3GetMeasurementMode(IntPtr devHndl, ref i1d3MeasMode_t measMode);
        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern i1d3Status_t i1d3SetLEDControl(IntPtr devHndl, i1d3LED_Control_e LEDconfig, double dOffTime, double dOnTime, uint ucCount);
        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]

        private static extern i1d3Status_t i1d3SetIntegrationTime(IntPtr devHndl, double dSeconds);//zh 250709
        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern i1d3Status_t i1d3GetIntegrationTime(IntPtr devHndl, ref double dSeconds);//zh 250709
        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]

        private static extern i1d3Status_t i1d3SetTargetLCDTime(IntPtr devHndl, double dSeconds);//zh 250709
        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern i1d3Status_t i1d3GetTargetLCDTime(IntPtr devHndl, ref double dSeconds);//zh 250709
        [DllImport(@LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]

        private static extern i1d3Status_t i1d3DeviceClose(IntPtr devHndl);



        //private static extern i1d3Status_t i1d3GetDeviceInfo(IntPtr devHndl, ref i1d3DEVICE_INFO infostruct);
        //[DllImport(LTDLL_NAME, CallingConvention = CallingConvention.Cdecl)]



        public static i1d3 getInstance()
        {
            if (instance == null)
                instance = new i1d3();

            return instance;
        }

        public i1d3()
        {
            Console.WriteLine("New class i1d3!");
        }


        public override bool Open()
        {

            try
            {
                //i1d3DEVICE_INFO info = new i1d3DEVICE_INFO();
                i1d3Status_t i1d3_status = i1d3Status_t.i1d3Success;
                i1d3MeasMode_t i1d3_mode = 0;
                double dSeconds = 0.0;//zh 250709
                i1d3Destroy();
                i1d3_status = (i1d3Status_t)i1d3Initialize();//zh add
                i1d3_status = i1d3GetDeviceHandle(0, ref m_hi1d3);
                i1d3_status = i1d3DeviceQuickOpen(m_hi1d3);
                byte[] ucOEM = { 0xD4, 0x9F, 0xD4, 0xA4, 0x59, 0x7E, 0x35, 0xCF, 0 };
                // Set measure mode 
                i1d3_status = i1d3SetMeasurementMode(m_hi1d3, i1d3MeasMode_t.i1d3MeasModeLCD);
                // Set productkey
                i1d3OverrideDeviceDefaults(0, 0, ucOEM);
                // Open i1d3
                i1d3_status = i1d3DeviceOpen(m_hi1d3);
                // Check status and measmode
                i1d3_status = i1d3GetMeasurementMode(m_hi1d3, ref i1d3_mode);
                Console.WriteLine($"i1d3 Measure mode {i1d3_mode}");

                //i1d3_status = i1d3SetIntegrationTime(m_hi1d3, 1.0);//zh 250709
                i1d3_status = i1d3GetIntegrationTime(m_hi1d3, ref dSeconds);//zh 250709
                Console.WriteLine($"i1d3 Measure Integration Time {dSeconds}");

                i1d3_status = i1d3SetTargetLCDTime(m_hi1d3, 0.02);//zh 250709 default is 0.2
                i1d3_status = i1d3GetTargetLCDTime(m_hi1d3, ref dSeconds);//zh 250709
                Console.WriteLine($"i1d3 Measure Target LCD Time {dSeconds}");
                // Set LED control
                LED_CONFIG_t m_LEDconfig;
                m_LEDconfig.Ctrl = i1d3LED_Control_e.i1d3LED_PULSE;
                m_LEDconfig.dOn = 2;
                m_LEDconfig.dOff = 2;
                i1d3_status = i1d3SetLEDControl(m_hi1d3, m_LEDconfig.Ctrl, m_LEDconfig.dOff, m_LEDconfig.dOn, 255);

                //Console.WriteLine($"i1d3 Open statue {i1d3_status}");

                if ((i1d3_status != i1d3Status_t.i1d3Success) && (i1d3GetNumberOfDevices() == 0))
                {
                    MessageBox.Show("Please connect i1D3 sensor!");
                    return false;
                }


            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        public override bool Close()
        {
            try
            {
                i1d3Status_t ild3_status = i1d3SetLEDControl(m_hi1d3, 0, 0, 0, 0);
                ild3_status = (i1d3Status_t)i1d3DeviceClose(m_hi1d3);
                Console.WriteLine($"i1d3 Close statue {ild3_status}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }
        public override string SensorName
        {
            get
            {
                return "i1d3";
            }
        }
        public override bool Config()
        {
            //to do
            return true;
        }

        public override SensorMeasure_t Measure()
        {
            SensorMeasure_t result = new SensorMeasure_t();

            return result;
        }
        private IntPtr m_hi1d3;

        public override bool MeasYxyz(out SensorMeasureYxy_t result)
        {
            // Initial Value
            result.Y = 0;
            result.x = 0;
            result.y = 0;
            result.z = 0;
            i1d3Status_t i1d3_status = i1d3Status_t.i1d3Success;
            //i1d3_status result_Yxyz = new i1d3Yxy_t();
            i1d3Yxy_t result_Yxyz = new i1d3Yxy_t();
            i1d3_status = i1d3MeasureYxy(m_hi1d3, ref result_Yxyz);
            Console.WriteLine($"MeasYxyz i1d3 status : {i1d3_status}");


            // 1. 執行量測  //ian add
            Console.WriteLine($"MeasYxyz i1d3 status : {i1d3_status}");

            // --- 新增邏輯：根據亮度動態調整 TargetLCDTime ---
            // 如果亮度 > 120 nits，設定為 0.2，否則設定為 0.02
            double targetTime = (result_Yxyz.Y > 120.0) ? 0.2 : 0.02;
            i1d3SetTargetLCDTime(m_hi1d3, targetTime);
            Console.WriteLine($"亮度: {result_Yxyz.Y} nits, 已設定 TargetLCDTime 為: {targetTime}s");
            // -------------------------------------------------- //ian add

            string hexCode = ((int)i1d3_status).ToString("X2");       // 狀態code
            string nameCode = i1d3_status.ToString();                  // enum 裡的成員名稱

            string msg = $"i1d3MeasureYxy 狀態碼: 0x{hexCode} ({(int)i1d3_status}) [{nameCode}]";
            Console.WriteLine(msg);
            if (i1d3_status != i1d3Status_t.i1d3Success)
                Console.WriteLine(msg, "i1d3 錯誤狀態");

            // Set LED control
            LED_CONFIG_t m_LEDconfig;
            m_LEDconfig.Ctrl = i1d3LED_Control_e.i1d3LED_FLASH;
            m_LEDconfig.dOn = 2;
            m_LEDconfig.dOff = 0;
            i1d3_status = i1d3SetLEDControl(m_hi1d3, m_LEDconfig.Ctrl, m_LEDconfig.dOff, m_LEDconfig.dOn, 1);
            result.Y = result_Yxyz.Y;
            result.y = result_Yxyz.y;
            result.x = result_Yxyz.x;
            result.z = result_Yxyz.z;
            if (i1d3_status != i1d3Status_t.i1d3Success)
            {
                Console.WriteLine($"MeasYxyz i1d3 status : {i1d3_status}");
                Console.WriteLine($"{result_Yxyz}");
                return false;
            }
            //Console.WriteLine($"{result_Yxyz.Y}, {result_Yxyz.x}, {result_Yxyz.y}, {result_Yxyz.z}");
            m_LEDconfig.Ctrl = i1d3LED_Control_e.i1d3LED_PULSE;
            m_LEDconfig.dOn = 2;
            m_LEDconfig.dOff = 2;
            i1d3_status = i1d3SetLEDControl(m_hi1d3, m_LEDconfig.Ctrl, m_LEDconfig.dOff, m_LEDconfig.dOn, 255);
            return true;
        }

    }
}

