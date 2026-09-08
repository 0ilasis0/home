using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Management.Instrumentation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Navigation;
using CASDK2;

namespace OnyxSensor
{
    public class ca410 : SensorBase
    {
        private static ca410 instance = null;

        //Declaration of objects
        static CASDK2Ca200 objCa200;
        static CASDK2Cas objCas;
        static CASDK2Ca objCa;
        static CASDK2Probes objProbes;
        static CASDK2OutputProbes objOutputProbes;
        static CASDK2Probe objProbe;
        static CASDK2Memory objMemory;
        static int err = 0;
        const int NOERR = 0;

        const int MAXPROBE = 10;

        const int MODE_Lvxy = 0;
        const int MODE_Tduv = 1;
        const int MODE_Lvdudv = 5;
        const int MODE_FMA = 6;
        const int MODE_XYZ = 7;
        const int MODE_JEITA = 8;
        const int MODE_LvPeld = 9;
        const int MODE_Waveform = 10;
        const int MODE_FMA2 = 11;
        const int MODE_JEITA2 = 12;
        const int MODE_Waveform2 = 13;

        const int RED = 0;
        const int GREEN = 1;
        const int BLUE = 2;
        const int WHITE = 3;

        static bool autoconnectflag = true; // auro or manual
        static bool triggerfinish = true;
        public static ca410 getInstance()
        {
            if (instance == null)
                instance = new ca410();

            return instance;
        }
        public ca410()
        {
            Console.WriteLine("New class ca410!");
        }
        public override bool Open()
        {
            objCa200 = new CASDK2Ca200();   // Generate application object

            autoconnectflag = true;
            if (GetErrorMessage(objCa200.AutoConnect()) != 0)
            {
                autoconnectflag = false;
                MessageBox.Show("Please connect ca410 sensor!");
            }
            // Substitute object variables
            //GetErrorMessage(objCa200.get_Cas(ref objCas));
            if (GetErrorMessage(objCa200.get_SingleCa(ref objCa)) != 0) autoconnectflag = false;
            //GetErrorMessage(objCa.get_Probes(ref objProbes));
            //GetErrorMessage(objCa.get_OutputProbes(ref objOutputProbes));
            if (GetErrorMessage(objCa.get_Memory(ref objMemory)) != 0) autoconnectflag = false;
            if (GetErrorMessage(objCa.get_SingleProbe(ref objProbe)) != 0) autoconnectflag = false;



            int freqmode = 4;   // SyncMode : INT 
            double freq = 60.0; //frequency = 60.0Hz
            int speed = 1;      //Measurement speed : FAST
            int Lvmode = 1;     //Lv : cd/m2
            if (GetErrorMessage(objCa.CalZero()) != 0) autoconnectflag = false;                       //Zero-Calibration           
            if (GetErrorMessage(objCa.put_DisplayProbe("P1")) != 0) autoconnectflag = false;          //Set display probe to P1           
            if (GetErrorMessage(objCa.put_SyncMode(freqmode, freq)) != 0) autoconnectflag = false;     //Set sync mode and frequency          
            if (GetErrorMessage(objCa.put_AveragingMode(speed)) != 0) autoconnectflag = false;        //Set measurement speed           
            if (GetErrorMessage(objCa.put_BrightnessUnit(Lvmode)) != 0) autoconnectflag = false;       //SetBrightness unit


            string PID = "";
            string dispprobe = "";
            int syncmode = 0;
            double syncfreq = 0.0;
            int measspeed = 0;

            //Get settings
            if (GetErrorMessage(objCa.get_PortID(ref PID)) != 0) autoconnectflag = false;                             //Get connection interface
            Console.WriteLine("PortID:" + PID);
            if (GetErrorMessage(objCa.get_DisplayProbe(ref dispprobe)) != 0) autoconnectflag = false;                 //Get display probe
            Console.WriteLine("DisplayProbe:" + dispprobe);
            if (GetErrorMessage(objCa.get_SyncMode(ref syncmode, ref syncfreq)) != 0) autoconnectflag = false;        //Get sync mode and frequency
            Console.WriteLine("SyncMode:" + syncmode + ",Syncfreq:" + syncfreq);
            if (GetErrorMessage(objCa.get_AveragingMode(ref measspeed)) != 0) autoconnectflag = false;                //Get measurement speed
            Console.WriteLine("MeasurementSpeed:" + measspeed);
            return autoconnectflag;
        }
        public override bool Close()
        {
            int status = 0;
            while (!triggerfinish)
            {
                System.Threading.Thread.Sleep(10);  //wait for completion of trigger measurement
            }
            //Disconnect CA-410
            if (autoconnectflag)
            {
                GetErrorMessage(objCa200.AutoDisconnect()); //Disconnect probe connected automatically
            }
            else
            {
                GetErrorMessage(objCa200.DisconnectAll());  //Disconnect probe connected manually
            }
            return true;
        }
        public override string SensorName
        {
            get
            {
                return "ca410";
            }
        }
        public override bool Config()
        {

            return true;
        }
        public override SensorMeasure_t Measure()
        {
            SensorMeasure_t result = new SensorMeasure_t();

            return result;
        }
        public override bool MeasYxyz(out SensorMeasureYxy_t result)
        {
            SetZeroCalEvent();
            int chnum = 1;      //CalibrationCH : 1
                                // Initialize result
            result = new SensorMeasureYxy_t
            {
                Y = 0,
                x = 0,
                y = 0,
                z = 0
            };

            if (GetErrorMessage(objMemory.put_ChannelNO(chnum))!=0) return false;
        

            // Initial Value
            int status = 0;
            result.Y = 0;
            result.x = 0;
            result.y = 0;
            result.z = 0;

            if (GetErrorMessage(objCa.put_DisplayMode(MODE_Lvxy))!= 0) return false; //Set mode:Color Lvxy

            if (GetErrorMessage(objCa.Measure()) != 0) return false;                  //Color measurement

            //Get Color result
            if (GetErrorMessage(objProbe.get_Lv(ref result.Y)) != 0) return false;
            if (GetErrorMessage(objProbe.get_sx(ref result.x)) != 0) return false;
            if (GetErrorMessage(objProbe.get_sy(ref result.y)) != 0) return false; 
          
            //if (status == 0)
            //{
            //    MessageBox.Show("ca410 connection failed");
            //    return false;
            //}

            return true;
        }

        private static int ExeCalZero(int dummy)
        {
            Console.WriteLine("Performing Zero Calibration");
            int status = objCa.CalZero();   //Zero calibration
            if (status != 0) Console.WriteLine($"Cal Zero Err!");
            return err;
        }

        ///<summary>
        ///[Set Zero Calibration event]
        ///This method set zero calibration event
        ///</summary>
        private static void SetZeroCalEvent()
        {
            Func<int, int> funczerocal = ExeCalZero;
            int status = objCa.SetExeCalZeroCallback(funczerocal);  //Set function for zero calibration event
            if (status != 0) Console.WriteLine($"SetZeroCalEvent Err!");
        }

        ///<summary>
        ///[Errorhandling]
        ///This method display Error message from Error number
        ///</summary>
        ///<param name = "errornum">Error number from API of SDK</param>
        private static int GetErrorMessage(int errornum)
        {
            string errormessage = "";
            if (errornum != 0)
            {
                //Get Error message from Error number
                err = GlobalFunctions.CASDK2_GetLocalizedErrorMsgFromErrorCode(0, errornum, ref errormessage);
                Console.WriteLine(errormessage);
                return errornum;
            }
            return 0;
        }
    }
}
