using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FTD2XX_NET;

using MReqP = ONYX_DataType.MonitorRequestPackage;
using MReplyP = ONYX_DataType.MonitorReplyPackage;

using System.Management;
using static FDTI_Factory_i2c.I2CController;
using System.Runtime.InteropServices;
using MonitorFactoryTool;

using static MonitorFactoryTool.MainWindow;
using ONYX_DataType;
using System.Diagnostics;
using System.Data.SqlClient;
using MonitorFactoryTool.FactoryGrid;
using System.Collections;
using System.Reflection;
using MonitorFactoryTool.Pages;

namespace FDTI_Factory_i2c
{
    public class I2CController
    {

        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                      Definitions                                           #####################
        //###################################################################################################################################
        //###################################################################################################################################

        // ###### Driver defines ######
        FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;

        // ###### I2C Library defines ######
        const byte I2C_Dir_SDAin_SCLin = 0x00;
        const byte I2C_Dir_SDAin_SCLout = 0x01;
        const byte I2C_Dir_SDAout_SCLout = 0x03;
        const byte I2C_Dir_SDAout_SCLin = 0x02;
        const byte I2C_Data_SDAhi_SCLhi = 0x03;
        const byte I2C_Data_SDAlo_SCLhi = 0x01;
        const byte I2C_Data_SDAlo_SCLlo = 0x00;
        const byte I2C_Data_SDAhi_SCLlo = 0x02;
        // MPSSE clocking commands
        const byte MSB_FALLING_EDGE_CLOCK_BYTE_IN = 0x24;
        const byte MSB_RISING_EDGE_CLOCK_BYTE_IN = 0x20;
        const byte MSB_FALLING_EDGE_CLOCK_BYTE_OUT = 0x11;
        const byte MSB_DOWN_EDGE_CLOCK_BIT_IN = 0x26;
        const byte MSB_UP_EDGE_CLOCK_BYTE_IN = 0x20;
        const byte MSB_UP_EDGE_CLOCK_BYTE_OUT = 0x10;
        const byte MSB_RISING_EDGE_CLOCK_BIT_IN = 0x22;
        const byte MSB_FALLING_EDGE_CLOCK_BIT_OUT = 0x13;
        // Clock
        // const uint ClockDivisor = 74;          //目前設定74:400k(照pdf計算)       //299;//20K     //59; //100K     //49;// 120K     //199;// 30KHz
        //const uint ClockDivisor = 59;            //250602 wu modify to 100K bits

        public uint ClockDivisor { get; set; } = 59;   //250715 wu modify  預設100K:59   tool 選400K 就變74

        // Sending and receiving
        static uint NumBytesToSend = 0;
        static uint NumBytesToRead = 0;
        uint NumBytesSent = 0;
        static uint NumBytesRead = 0;
        static byte[] MPSSEbuffer = new byte[500];
        static byte[] InputBuffer = new byte[500];
        static byte[] InputBuffer2 = new byte[500];
        static uint BytesAvailable = 0;
        public static bool I2C_Ack = false;
        static byte AppStatus = 0;
        static byte I2C_Status = 0;
        public bool Running = true;
        static bool DeviceOpen = false;
        // GPIO
        static byte GPIO_Low_Dat = 0;
        static byte GPIO_Low_Dir = 0;
        static byte ADbusReadVal = 0;
        static byte ACbusReadVal = 0;

        private Factory factoryPage;
        private static Factory factoryWindow = new Factory();

        public static I2CController _instance = null;  //方便使用I2CController寫法 目前連結FactoryGridStyle2.xaml.cs
        public static I2CController Instance
        {
            get
            {
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }


        //Zhixiang add
        uint devcount = 0;
        static byte[] DDCBuffer = new byte[500];

        // Create new instance of the FTDI device class
        FTDI myFtdiDevice = new FTDI();

        // Create for FTDI USB unplug observe
        HotPlug hotPlug = new HotPlug();

        public I2CController(FTDI FtdiDevice)
        {
            myFtdiDevice = FtdiDevice;
            Instance = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Hello World! Zhixiang ");

            // For FTDI USB unplug observe
            hotPlug.Register(HotPlugDeviceInserted, HotPlugDeviceRemoved);
        }


        public void DisconnectProcess()
        {
            DeviceOpen = false;

            AppStatus = I2C_SetLineStatesIdle();
            if (AppStatus != 0)
            {
                Console.WriteLine("button_Disconnect_Click error!");
            }

            // Close the FTDI device and then close the window
            myFtdiDevice.Close();

            // For FTDI USB unplug observe close
            hotPlug.Unregister();
        }


        //###################################################################################################################################
        //###################################################################################################################################
        //##################                             I2C Layer                                                      #####################
        //###################################################################################################################################
        //###################################################################################################################################

        public byte I2C_ConfigureMpsse()
        {
            byte ADbusVal = 0;
            byte ADbusDir = 0;
            NumBytesToSend = 0;

            /***** Initial device configuration *****/

            ftStatus = FTDI.FT_STATUS.FT_OK;
            ftStatus |= myFtdiDevice.SetTimeouts(5000, 5000);
            ftStatus |= myFtdiDevice.SetLatency(16);
            ftStatus |= myFtdiDevice.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0x00, 0x00);
            ftStatus |= myFtdiDevice.SetBitMode(0x00, 0x00);
            ftStatus |= myFtdiDevice.SetBitMode(0x00, 0x02);         // MPSSE mode        

            if (ftStatus != FTDI.FT_STATUS.FT_OK)
                return 1; // error();

            /***** Flush the buffer *****/
            I2C_Status = FlushBuffer();

            /***** Synchronize the MPSSE interface by sending bad command 0xAA *****/
            NumBytesToSend = 0;
            MPSSEbuffer[NumBytesToSend++] = 0xAA;
            I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0) return 1; // error();
            NumBytesToRead = 2;
            I2C_Status = Receive_Data(2);
            if (I2C_Status != 0) return 1; //error();

            if ((InputBuffer2[0] == 0xFA) && (InputBuffer2[1] == 0xAA))
            {
                Console.WriteLine("Bad Command Echo successful");
            }
            else
            {
                return 1;            //error();
            }

            /***** Synchronize the MPSSE interface by sending bad command 0xAB *****/
            NumBytesToSend = 0;
            MPSSEbuffer[NumBytesToSend++] = 0xAB;
            I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0) return 1; // error();
            NumBytesToRead = 2;
            I2C_Status = Receive_Data(2);
            if (I2C_Status != 0) return 1; //error();

            if ((InputBuffer2[0] == 0xFA) && (InputBuffer2[1] == 0xAB))
            {
                Console.WriteLine("Bad Command Echo successful");
            }
            else
            {
                return 1;            //error();
            }

            NumBytesToSend = 0;
            MPSSEbuffer[NumBytesToSend++] = 0x8A; 	// Disable clock divide by 5 for 60Mhz master clock
            MPSSEbuffer[NumBytesToSend++] = 0x97;	// Turn off adaptive clocking
            MPSSEbuffer[NumBytesToSend++] = 0x8C; 	// Enable 3 phase data clock, used by I2C to allow data on both clock edges
            // The SK clock frequency can be worked out by below algorithm with divide by 5 set as off
            // SK frequency  = 60MHz /((1 +  [(1 +0xValueH*256) OR 0xValueL])*2)
            MPSSEbuffer[NumBytesToSend++] = 0x86; 	//Command to set clock divisor
            MPSSEbuffer[NumBytesToSend++] = (byte)(ClockDivisor & 0x00FF);	//Set 0xValueL of clock divisor
            MPSSEbuffer[NumBytesToSend++] = (byte)((ClockDivisor >> 8) & 0x00FF);	//Set 0xValueH of clock divisor
            MPSSEbuffer[NumBytesToSend++] = 0x85; 			// loopback off
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));  	// SDA and SCL set low but as input to mimic open drain
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLin | (GPIO_Low_Dir & 0xF8));	//

            MPSSEbuffer[NumBytesToSend++] = 0x80; 	//Command to set directions of lower 8 pins and force value on bits set as output 
            MPSSEbuffer[NumBytesToSend++] = (byte)(ADbusVal);
            MPSSEbuffer[NumBytesToSend++] = (byte)(ADbusDir);

            Console.WriteLine("ClockDivisor in I2Cconnect.cs I2C_ConfigureMpsse function: " + ClockDivisor);

            I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0)
            {
                return 1;            //error();
            }
            else
            {
                return 0;
            }
        }

        //###################################################################################################################################
        // Reads a byte over I2C 
        public byte I2C_ReadByte(bool ACK)
        {
            byte ADbusVal = 0;
            byte ADbusDir = 0;
            NumBytesToSend = 0;

            // Ensure line is definitely an input since FT2232H and FT4232H don't have open drain
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8)); // make data input
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions
            // Clock one byte of data in from the sensor
            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BYTE_IN;      // Clock data byte in
            MPSSEbuffer[NumBytesToSend++] = 0x00;
            MPSSEbuffer[NumBytesToSend++] = 0x00;                               // Data length of 0x0000 means 1 byte data to clock in

            // Change direction back to output and clock out one bit. If ACK is true, we send bit as 0 as an acknowledge
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));    // back to output
            MPSSEbuffer[NumBytesToSend++] = 0x80;                               // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                           // set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                           // set the directions

            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BIT_OUT;    // Clock data bit out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                              // Length of 0 means 1 bit
            if (ACK == true)
            {
                MPSSEbuffer[NumBytesToSend++] = 0x00;                          // Data bit to send is a '0'
            }
            else
            {
                MPSSEbuffer[NumBytesToSend++] = 0xFF;                          // Data bit to send is a '1'
            }

            // Put line states back to idle with SDA open drain high (set to input) 
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8));//make data input
            MPSSEbuffer[NumBytesToSend++] = 0x80;                               //       ' Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                            //      ' Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                             //     ' Set the directions

            // This command then tells the MPSSE to send any results gathered back immediately
            MPSSEbuffer[NumBytesToSend++] = 0x87;                                  //    ' Send answer back immediate command

            // send commands to chip
            I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0)
            {
                return 1;
            }

            // get the byte which has been read from the driver's receive buffer
            I2C_Status = Receive_Data(1);
            if (I2C_Status != 0)
            {
                return 1;
            }

            // InputBuffer2[0] now contains the results

            return 0;
        }


        //###################################################################################################################################
        // Sends I2C address followed by reading 2 bytes

        public byte I2C_Read2BytesWithAddr(byte Address)
        {
            byte ADbusVal = 0;
            byte ADbusDir = 0;
            NumBytesToSend = 0;

            // ------------------------------------ Address ------------------------------------

            Address <<= 1;
            Address |= 0x01;

            // Set directions of clock and data to output in preparation for clocking out a byte
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));// back to output
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions
            // clock out one byte
            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BYTE_OUT;        // clock data byte out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // 
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // Data length of 0x0000 means 1 byte data to clock in
            MPSSEbuffer[NumBytesToSend++] = Address;                         // Byte to send

            // Put line back to idle (data released, clock pulled low) so that sensor can drive data line
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8)); // make data input
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions

            // CLOCK IN ACK
            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BIT_IN;           // clock data byte in
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // Length of 0 means 1 bit

            // ------------------------------------ Clock in 1st byte and ACK ------------------------------------     

            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BYTE_IN;      // Clock data byte in
            MPSSEbuffer[NumBytesToSend++] = 0x00;
            MPSSEbuffer[NumBytesToSend++] = 0x00;                               // Data length of 0x0000 means 1 byte data to clock in

            // Send a 0 bit as an acknowledge
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));//back to output
            MPSSEbuffer[NumBytesToSend++] = 0x80;                               //       ' Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                            //      ' Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                             //     ' Set the directions

            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BIT_OUT;    // Clock data bit out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                              // Length of 0 means 1 bit
            MPSSEbuffer[NumBytesToSend++] = 0x00;                              // Sending 0 here as ACK

            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8));//make data input

            MPSSEbuffer[NumBytesToSend++] = 0x80;                               //       ' Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                            //      ' Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                             //     ' Set the directions

            // ------------------------------------ Clock in 2nd byte and NAK ------------------------------------

            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BYTE_IN;      // Clock data byte in
            MPSSEbuffer[NumBytesToSend++] = 0x00;
            MPSSEbuffer[NumBytesToSend++] = 0x00;                               // Data length of 0x0000 means 1 byte data to clock in

            // Send a 1 bit as a Nack
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));//back to output
            MPSSEbuffer[NumBytesToSend++] = 0x80;                               //       ' Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                            //      ' Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                             //     ' Set the directions

            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BIT_OUT;    // Clock data bit out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                              // Length of 0 means 1 bit
            MPSSEbuffer[NumBytesToSend++] = 0xFF;                              // Sending 1 here as NAK

            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8));//make data input

            MPSSEbuffer[NumBytesToSend++] = 0x80;                               //       ' Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                            //      ' Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                             //     ' Set the directions

            // This command then tells the MPSSE to send any results gathered back immediately
            MPSSEbuffer[NumBytesToSend++] = 0x87;                                //  ' Send answer back immediate command

            // Send off the commands
            I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0)
            {
                return 1;
            }

            // Read back the ack from the address phase and the 2 bytes read
            I2C_Status = Receive_Data(3);
            if (I2C_Status != 0)
            {
                return 1;
            }

            // Check if address phase was acked
            if ((InputBuffer2[0] & 0x01) == 0)
            {
                I2C_Ack = true;
            }
            else
            {
                I2C_Ack = false;
            }

            // Get the two data bytes to put back to the calling function - InputBuffer2[0..1] now contains the results
            InputBuffer2[0] = InputBuffer2[1];
            InputBuffer2[1] = InputBuffer2[2];

            return 0;

        }


        //###################################################################################################################################

        public byte I2C_SendDeviceAddrAndCheckACK(byte Address, bool Read)
        {

            byte ADbusVal = 0;
            byte ADbusDir = 0;
            NumBytesToSend = 0;

            Address <<= 1;
            if (Read == true)
                Address |= 0x01;

            // Set directions of clock and data to output in preparation for clocking out a byte
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));// back to output
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions
            // clock out one byte
            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BYTE_OUT;        // clock data byte out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // 
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // Data length of 0x0000 means 1 byte data to clock in
            MPSSEbuffer[NumBytesToSend++] = Address;                         // Byte to send

            // Put line back to idle (data released, clock pulled low) so that sensor can drive data line
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8)); // make data input
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions

            // CLOCK IN ACK
            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BIT_IN;           // clock data byte in
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // Length of 0 means 1 bit

            // This command then tells the MPSSE to send any results gathered (in this case the ack bit) back immediately
            MPSSEbuffer[NumBytesToSend++] = 0x87;                                //  ' Send answer back immediate command

            for (int i = 0; i < DEVICE_RETRY_COUNT; i++)//zh add
            {
                // send commands to chip
                I2C_Status = Send_Data(NumBytesToSend);
                if (I2C_Status != 0)
                {
                    return 1;
                }

                // read back byte containing ack
                I2C_Status = Receive_Data(1);
                if (I2C_Status != 0)
                {
                    return 1;            // can also check NumBytesRead
                }

                // if ack bit is 0 then sensor acked the transfer, otherwise it nak'd the transfer
                if ((InputBuffer2[0] & 0x01) == 0)
                {
                    I2C_Ack = true;
                    break;
                }
                else
                {
                    I2C_Ack = false;
                    continue;//zh add
                }
            }
            return 0;

        }

        //###################################################################################################################################
        // Writes one byte to the I2C bus

        public byte I2C_SendByteAndCheckACK(byte DataByteToSend)
        {
            byte ADbusVal = 0;
            byte ADbusDir = 0;
            NumBytesToSend = 0;

            //zh del 20250611//Console.Write(String.Format("0x{0:X2} ", DataByteToSend));
            //Console.Write(String.Format("0x{0:X2}, ", DataByteToSend));
            // Set directions of clock and data to output in preparation for clocking out a byte
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));// back to output
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions
            // clock out one byte
            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BYTE_OUT;        // clock data byte out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // 
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // Data length of 0x0000 means 1 byte data to clock in
            MPSSEbuffer[NumBytesToSend++] = DataByteToSend;                         // Byte to send

            // Put line back to idle (data released, clock pulled low) so that sensor can drive data line
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8)); // make data input
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions

            // CLOCK IN ACK
            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BIT_IN;           // clock data byte in
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // Length of 0 means 1 bit

            // This command then tells the MPSSE to send any results gathered (in this case the ack bit) back immediately
            MPSSEbuffer[NumBytesToSend++] = 0x87;                                //  ' Send answer back immediate command

            for (int i = 0; i < DEVICE_RETRY_COUNT; i++)//zh add
            {
                // send commands to chip
                I2C_Status = Send_Data(NumBytesToSend);
                if (I2C_Status != 0)
                {
                    return 1;
                }

                // read back byte containing ack
                I2C_Status = Receive_Data(1);
                if (I2C_Status != 0)
                {
                    return 1;            // can also check NumBytesRead
                }

                // if ack bit is 0 then sensor acked the transfer, otherwise it nak'd the transfer
                if ((InputBuffer2[0] & 0x01) == 0)
                {
                    I2C_Ack = true;
                    break;
                }
                else
                {
                    I2C_Ack = false;
                    continue;//zh add
                }
            }
            return 0;

        }

        //###################################################################################################################################
        // Sets I2C Start condition

        public byte I2C_SetStart()
        {
            byte Count = 0;
            byte ADbusVal = 0;
            byte ADbusDir = 0;
            NumBytesToSend = 0;

            // Both SDA and SCL high (setting to input simulates open drain high)
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLin | (GPIO_Low_Dir & 0xF8));

            for (Count = 0; Count < 6; Count++)
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }

            // SDA low, SCL high (setting to input simulates open drain high)
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLin | (GPIO_Low_Dir & 0xF8));

            for (Count = 0; Count < 6; Count++)	// Repeat commands to ensure the minimum period of the start setup time
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }

            // SDA low, SCL low
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));//
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));//as above

            for (Count = 0; Count < 6; Count++)	// Repeat commands to ensure the minimum period of the start setup time
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }

            // Release SDA (setting to input simulates open drain high)
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));//
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8));//as above

            MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction

            I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0)
                return 1;
            else
                return 0;

        }

        //###################################################################################################################################
        // Sets I2C Stop condition

        public byte I2C_SetStop()
        {
            byte Count = 0;
            byte ADbusVal = 0;
            byte ADbusDir = 0;
            NumBytesToSend = 0;

            // SDA low, SCL low
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));

            for (Count = 0; Count < 6; Count++)
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }


            // SDA low, SCL high (note: setting to input simulates open drain high)
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLin | (GPIO_Low_Dir & 0xF8));

            for (Count = 0; Count < 6; Count++)
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }

            // SDA high, SCL high (note: setting to input simulates open drain high)
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLin | (GPIO_Low_Dir & 0xF8));

            for (Count = 0; Count < 6; Count++)	// Repeat commands to hold states for longer time
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }

            // send the buffer of commands to the chip 
            I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0)
                return 1;
            else
                return 0;

        }

        //###################################################################################################################################
        // Sets GPIO values on low byte and puts I2C lines (bits 0, 1, 2) to idle outwith transaction state

        public byte I2C_SetLineStatesIdle()
        {
            byte ADbusVal = 0;
            byte ADbusDir = 0;
            NumBytesToSend = 0;

            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLin | (GPIO_Low_Dir & 0xF8));       // FT2232H/FT4232H use input to mimic open drain

            MPSSEbuffer[NumBytesToSend++] = 0x80;       // ADbus GPIO command
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;   // Set direction

            I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0)
                return 1;
            else
                return 0;
        }



        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                          D2xx Layer                                        #####################
        //###################################################################################################################################
        //###################################################################################################################################


        // Read a specified number of bytes from the driver receive buffer

        private byte Receive_Data(uint BytesToRead)
        {
            uint NumBytesInQueue = 0;
            uint QueueTimeOut = 0;
            uint Buffer1Index = 0;
            uint Buffer2Index = 0;
            uint TotalBytesRead = 0;
            bool QueueTimeoutFlag = false;
            uint NumBytesRxd = 0;

            // Keep looping until all requested bytes are received or we've tried 5000 times (value can be chosen as required)
            while ((TotalBytesRead < BytesToRead) && (QueueTimeoutFlag == false))
            {
                ftStatus = myFtdiDevice.GetRxBytesAvailable(ref NumBytesInQueue);       // Check bytes available

                if ((NumBytesInQueue > 0) && (ftStatus == FTDI.FT_STATUS.FT_OK))
                {
                    ftStatus = myFtdiDevice.Read(InputBuffer, NumBytesInQueue, ref NumBytesRxd);  // if any available read them

                    if ((NumBytesInQueue == NumBytesRxd) && (ftStatus == FTDI.FT_STATUS.FT_OK))
                    {
                        Buffer1Index = 0;

                        while (Buffer1Index < NumBytesRxd)
                        {
                            //Debug.WriteLine("InputBuffer[Buffer1Index]"+InputBuffer[Buffer1Index]);
                            InputBuffer2[Buffer2Index] = InputBuffer[Buffer1Index];     // copy into main overall application buffer
                            Buffer1Index++;
                            Buffer2Index++;
                        }
                        TotalBytesRead = TotalBytesRead + NumBytesRxd;                  // Keep track of total
                    }
                    else
                        return 1;

                    QueueTimeOut++;
                    if (QueueTimeOut == 5000)
                        QueueTimeoutFlag = true;
                    else
                        Thread.Sleep(0);                                                // Avoids running Queue status checks back to back
                }
            }
            // returning globals NumBytesRead and the buffer InputBuffer2
            NumBytesRead = TotalBytesRead;

            if (QueueTimeoutFlag == true)
                return 1;
            else
                return 0;
        }


        //###################################################################################################################################
        // Write a buffer of data and check that it got sent without error

        private byte Send_Data(uint BytesToSend)
        {

            NumBytesToSend = BytesToSend;

            // Send data. This will return once all sent or if times out
            ftStatus = myFtdiDevice.Write(MPSSEbuffer, NumBytesToSend, ref NumBytesSent);

            // Ensure that call completed OK and that all bytes sent as requested
            if ((NumBytesSent != NumBytesToSend) || (ftStatus != FTDI.FT_STATUS.FT_OK))
                return 1;   // error   calling function can check NumBytesSent to see how many got sent
            else
                return 0;   // success
        }


        //###################################################################################################################################
        // Flush drivers receive buffer - Get queue status and read everything available and discard data

        private byte FlushBuffer()
        {
            ftStatus = myFtdiDevice.GetRxBytesAvailable(ref BytesAvailable);	 // Get the number of bytes in the receive buffer
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
                return 1;

            if (BytesAvailable > 0)
            {
                ftStatus = myFtdiDevice.Read(InputBuffer, BytesAvailable, ref NumBytesRead);  	//Read out the data from receive buffer
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                    return 1;       // error
                else
                    return 0;       // all bytes successfully read
            }
            else
            {
                return 0;           // there were no bytes to read
            }
        }

        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                          DDCCI Layer                                        #####################
        //###################################################################################################################################
        //###################################################################################################################################

        //##################                                         GAMMA LUT                                        #####################
        bool OnyxFactoryCommand = true;
        bool OnyxFactoryCommand_ALC = false;
        bool OnyxFactoryCommand_GAMMA = true;


        public const Int32 DELAY_TIME = 30;
        public const byte DEST_ADDRESS = 0x37;//0x37 is 7 bit address, 0x6E is 8bit address
        public const byte SOURCE_ADDRESS = 0x51;
        public const byte DEVICE_RETRY_COUNT = 10;
        byte[] SetCommandArray = { DEST_ADDRESS * 2, SOURCE_ADDRESS, 0x84, 0x03, 0x10, 0x00, 0x64 };   //0x10:Brightness, 0x12:Contrast ,0x14:Color Preset(Temp) ,0x16:Red
        byte[] GetCommandArray = { DEST_ADDRESS * 2, SOURCE_ADDRESS, 0x82, 0x01, 0x10 };               //0x10:Brightness, 0x12:Contrast ,0x14:Color Preset(Temp) ,0x16:Red
        byte[] NullCommandArray = { DEST_ADDRESS * 2, SOURCE_ADDRESS, 0x80 };// zh 250212



        byte[] SetCommandArrayLUT = { DEST_ADDRESS * 2, SOURCE_ADDRESS, 0x98, 0xD0, 0x65, 0x07, 0x05, GAMA3, CT3, 0x02, 0x00 };//zh Factory Write Blue Gamma

        byte[] GetCommandArrayLUT1 = { DEST_ADDRESS * 2, SOURCE_ADDRESS, 0x88, 0xD0, 0x75, 0x07, 0x05, 0x12, 0x41, 0x00, 0x00 };//zh Factory Read Gamma checksum
        byte[] GetCommandArrayLUT2 = { DEST_ADDRESS * 2, SOURCE_ADDRESS, 0x86, 0xD0, 0x75, 0x07, 0x03, 0x00, 0x00 };//zh ALS Sensor
        byte[] NullCommandArrayLUT = { DEST_ADDRESS * 2, SOURCE_ADDRESS, 0x80 };// zh 250212

        byte gamma_checksum, gamma_reply_checksum = 0x00;

        public const byte GAMA1 = 0x12;//1.8
        public const byte GAMA2 = 0x14;//2.0
        public const byte GAMA3 = 0x16;//2.2
        public const byte GAMA4 = 0x1A;//2.6
        public const byte GAMA5 = 0x20;//DICOM

        public const byte CT1 = 0x36;//5400K
        public const byte CT2 = 0x41;//6500K
        public const byte CT3 = 0x5D;//9300K


        public byte check_sum(byte[] pArray)
        {
            byte checksum = 0x00;
            for (int i = 0; i < pArray.Length; i++)
            {
                byte data = pArray[i];
                checksum ^= data;
            }
            return checksum;
        }

        public byte check_sum_gamma(int[] pArray, int row)//zh 250707
        {

            for (int i = 16 * row; i < 16 + 16 * row; i++)
            {
                //byte data = pArray[i];
                byte data = (byte)pArray[i];//zh 250707
                //Console.Write(String.Format("0x{0:X}", data)+ " ");
                gamma_checksum ^= data;
            }
            //Console.Write("GAMMA Checksum is " + String.Format("0x{0:X} ", gamma_checksum));
            return gamma_checksum;
        }

        public byte DDCCI_Null_Message()// zh 250212
        {
            AppStatus = I2C_SetStart();
            if (AppStatus != 0) return 11;

            AppStatus = I2C_SendDeviceAddrAndCheckACK((byte)(DEST_ADDRESS), false);
            if (AppStatus != 0) return 22;
            if (I2C_Ack != true) { I2C_SetStop(); return 22; }
            AppStatus = I2C_SendByteAndCheckACK((byte)(SOURCE_ADDRESS));
            if (AppStatus != 0) return 33;
            if (I2C_Ack != true) { I2C_SetStop(); return 33; }

            AppStatus = I2C_SendByteAndCheckACK((byte)(NullCommandArray[2]));
            if (AppStatus != 0) return 44;

            AppStatus = I2C_SetStop();                                                  // I2C STOP
            if (AppStatus != 0) return 4;

            return 0;
        }

        public byte DDCCI_Send_Command(byte[] CommandArray, Action<byte[]> Func = null, bool isListboxDisplay = true)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;

            byte trycount = 255;
            int datalength = (int)(CommandArray[2] - 0x80);

            //return 多少都是方便觀察傳輸到什麼狀況會出錯
            AppStatus = I2C_SetStart();
            if (AppStatus != 0) return 1;

            AppStatus = I2C_SendDeviceAddrAndCheckACK((byte)(DEST_ADDRESS), false);
            if (AppStatus != 0)
            {
                Debug.WriteLine("return 2");
                return 2;
            }
            if (I2C_Ack != true) { I2C_SetStop(); return 3; }
            AppStatus = I2C_SendByteAndCheckACK((byte)(SOURCE_ADDRESS));
            if (AppStatus != 0) return 4;
            if (I2C_Ack != true) { I2C_SetStop(); return 5; }

            AppStatus = I2C_SendByteAndCheckACK((byte)(CommandArray[2]));
            if (AppStatus != 0) return 6;

            for (int i = 0; i < datalength; i++)
            {
                AppStatus = I2C_SendByteAndCheckACK((byte)(CommandArray[3 + i]));   // 3 is data start index
                if (AppStatus != 0) return (byte)(7 + i);
            }

            AppStatus = I2C_SendByteAndCheckACK((byte)(check_sum(CommandArray)));

            if (AppStatus != 0) return 11;
            if (I2C_Ack != true) { I2C_SetStop(); return 12; }

            AppStatus = I2C_SetStop();                                                      // I2C STOP
            if (AppStatus != 0) return 13;


            //以下GET command才會用到
            Thread.Sleep(DELAY_TIME);

            AppStatus = I2C_SetStart();
            if (AppStatus != 0) return 12;
            AppStatus = I2C_SendDeviceAddrAndCheckACK((byte)(DEST_ADDRESS), true);
            if (AppStatus != 0) return 13;

            string tempText = "";
            if (!factoryWindow.isMccsMode())
                tempText = "6F6E";
            Thread.Sleep(600);// zh 250715
            do
            {
                AppStatus = I2C_ReadByte(true);                                             // I2C READ (send Ack)
                if (AppStatus != 0) return 16;
                DDCBuffer[0] = InputBuffer2[0];                                                // Get the byte read

                trycount--;
            } while (DDCBuffer[0] != 0x6E && trycount != 0);

            int DDClenght = 17;

            for (int i = 0; i <= DDClenght; i++)
            {
                AppStatus = I2C_ReadByte(true);                                             // I2C READ (send Ack)
                if (AppStatus != 0) return 17;
                DDCBuffer[0] = InputBuffer2[0];                                             // Get the byte read

                if (i == 0)// read length
                {
                    DDClenght = DDCBuffer[0] - 0x80 + 1;                                    //FC之後的長度
                    Console.WriteLine($"\nDDCBuffer -> length: {DDCBuffer[0]:X2}");         //紀錄get 長度
                }

                tempText += String.Format("{0:X2}", DDCBuffer[0]);
            }

            AppStatus = I2C_SetStop();                                                  // I2C STOP
            if (AppStatus != 0) return 18;

            Console.WriteLine("Temptext: " + tempText); //紀錄command

            if (tempText.Length >= 4 && tempText.Substring(tempText.Length - 4, 2).Equals("E3", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("指令錯誤 E3 !!!");
            }
            else if (tempText.Length >= 4 && tempText.Substring(tempText.Length - 4, 2).Equals("E4", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("功能不支援 E4 !!!");
            }


            if (tempText.Length >= 0)
            {
                if (Func != null)
                    Func(StringToByteArray(tempText));

                if (tempText.Contains("88") && isListboxDisplay)    //////GET command 裡面有88 只有GET才顯示data數值　　SET 不會顯示data值 
                {
                    I2C_CommandAnalyzer(tempText);
                }
            }
            return 0;
        }




        ///wu 250606 add for burning LUT  to board   TEST!!!

        public byte DDCCI_Set_CommandGammaCT(Int32 gm, Int32 ct, Int32 ch, Int32 value, LUTData lut)
        {
            if (gm == 0x20)
                OnyxFactoryCommand_GAMMA = true;



            byte trycount = 255;
            SetCommandArrayLUT[7] = (byte)gm;
            SetCommandArrayLUT[8] = (byte)ct;
            SetCommandArrayLUT[9] = (byte)ch;
            int[] selectedChannelData = null; ;
            // byte[] lutAsByteArray = new byte[512];
            int[] lutAsByteArray = new int[2048];//zh 250707
            SetCommandArrayLUT[10] = (byte)value; //wu 0609 add 


            byte outgoingChecksum = 0;//zh add 250611

            AppStatus = I2C_SetStart();
            if (AppStatus != 0) return 1;

            AppStatus = I2C_SendDeviceAddrAndCheckACK((byte)(DEST_ADDRESS), false);
            if (AppStatus != 0) return 2;
            if (I2C_Ack != true) { I2C_SetStop(); return 3; }
            AppStatus = I2C_SendByteAndCheckACK((byte)(SOURCE_ADDRESS));
            if (AppStatus != 0) return 4;
            if (I2C_Ack != true) { I2C_SetStop(); return 5; }

            AppStatus = I2C_SendByteAndCheckACK((byte)(SetCommandArrayLUT[2]));
            if (AppStatus != 0) return 6;
            AppStatus = I2C_SendByteAndCheckACK((byte)(SetCommandArrayLUT[3]));
            if (AppStatus != 0) return 7;
            AppStatus = I2C_SendByteAndCheckACK((byte)(SetCommandArrayLUT[4]));
            if (AppStatus != 0) return 8;
            AppStatus = I2C_SendByteAndCheckACK((byte)(SetCommandArrayLUT[5]));
            if (AppStatus != 0) return 9;
            AppStatus = I2C_SendByteAndCheckACK((byte)(SetCommandArrayLUT[6]));
            if (AppStatus != 0) return 10;

            if (OnyxFactoryCommand == true)
            {
                AppStatus = I2C_SendByteAndCheckACK((byte)(SetCommandArrayLUT[7]));
                if (AppStatus != 0) return 9;
                AppStatus = I2C_SendByteAndCheckACK((byte)(SetCommandArrayLUT[8]));
                if (AppStatus != 0) return 10;
                AppStatus = I2C_SendByteAndCheckACK((byte)(SetCommandArrayLUT[9]));
                if (AppStatus != 0) return 11;

                if (OnyxFactoryCommand_ALC == true)
                {
                    for (int i = 0; i < 17; i++)
                    {
                        AppStatus = I2C_SendByteAndCheckACK((byte)(SetCommandArrayLUT[9 + i]));
                    }
                }


                if (OnyxFactoryCommand_GAMMA == true)
                {
                    SetCommandArrayLUT[10] = (byte)value;
                    AppStatus = I2C_SendByteAndCheckACK((byte)(SetCommandArrayLUT[10]));
                    if (AppStatus != 0) return 12;

                    switch (ch)
                    {
                        case 0:
                            selectedChannelData = lut.RLUT;
                            break;
                        case 1:
                            selectedChannelData = lut.GLUT;
                            break;
                        default: // case 2
                            selectedChannelData = lut.BLUT;
                            break;

                    }

                    // 2. 將完整的 int[256] 通道資料轉換為 byte[512]
                    //   byte[] lutAsByteArray = new byte[512];
                    for (int i = 0; i < selectedChannelData.Length; i++)
                    {
                        int pixelValue = selectedChannelData[i];
                        lutAsByteArray[i * 2] = (byte)(pixelValue & 0xFF);
                        lutAsByteArray[i * 2 + 1] = (byte)((pixelValue >> 8) & 0xFF);
                    }

                    // 3. 從轉換後的 byte[] 中，只發送該區段的 16 個位元組
                    int baseOffset = value * 16;
                    for (int i = 0; i < 16; i++)
                    {
                        //AppStatus = I2C_SendByteAndCheckACK(lutAsByteArray[baseOffset + i]);
                        AppStatus = I2C_SendByteAndCheckACK((byte)lutAsByteArray[baseOffset + i]);//zh 250707
                    }

                    //for checksum
                    //修正 Checksum 的呼叫語法，傳入轉換後的 byte[] 陣列
                    outgoingChecksum = check_sum_gamma(lutAsByteArray, value);//zh add 250611
                    AppStatus = I2C_SendByteAndCheckACK((byte)(check_sum(SetCommandArrayLUT) ^ outgoingChecksum));

                    Console.WriteLine("GAMMA Checksum is " + String.Format("0x{0:X2} ", gamma_checksum));
                    gamma_checksum = 0;
                }
            }
            else
            {
                AppStatus = I2C_SendByteAndCheckACK((byte)(check_sum(SetCommandArrayLUT)));
            }


            if (AppStatus != 0) return 11;
            //if (I2C_Ack != true) { I2C_SetStop(); return 12; }
            AppStatus = I2C_SetStop();                                                      // I2C STOP
            if (AppStatus != 0) return 13;

            //Console.WriteLine("Checksum is "+ String.Format("0x{0:X}", check_sum(SetCommandArray)));
            Thread.Sleep(DELAY_TIME);

            AppStatus = I2C_SetStart();
            if (AppStatus != 0) return 25;

            AppStatus = I2C_SendDeviceAddrAndCheckACK((byte)(DEST_ADDRESS), true);
            if (AppStatus != 0) return 26;

            Thread.Sleep(500);// zh modify 250611
            do
            {
                AppStatus = I2C_ReadByte(true);                                             // I2C READ (send Ack)
                if (AppStatus != 0) return 29;
                DDCBuffer[0] = InputBuffer2[0];                                                // Get the byte read
                trycount--;
            } while (DDCBuffer[0] != 0x6E && trycount != 0);
            Console.WriteLine("==Replay==");
            int DDClenght = 16;
            if (OnyxFactoryCommand_GAMMA == true)
                DDClenght = 25;
            for (int i = 0; i < DDClenght; i++)
            {
                AppStatus = I2C_ReadByte(true);                                             // I2C READ (send Ack)
                if (AppStatus != 0) return 30;
                DDCBuffer[0] = InputBuffer2[0];
                if (i == 0)// read lenght
                {
                    DDClenght = DDCBuffer[0] - 0x80 + 1;
                }
                else if (i > DDClenght)
                {
                    break;
                }
                if (i == 6)
                    gamma_reply_checksum = DDCBuffer[0];


                //Console.WriteLine(String.Format("r 0x{0:X2}", DDCBuffer[0]));
                //zh//Console.Write(String.Format("0x{0:X2}, ", DDCBuffer[0]));
                //zh del 20250611
                //Console.Write(String.Format("0x{0:X2}, ", DDCBuffer[0]));
            }
            Console.WriteLine("GAMMA reply Checksum is " + String.Format("0x{0:X2} ", gamma_reply_checksum));
            Console.WriteLine("");

            AppStatus = I2C_SetStop();                                                  // I2C STOP
            if (AppStatus != 0) return 31;

            I2C_SetLineStatesIdle();

            if (outgoingChecksum != gamma_reply_checksum)//zh add here 250611
            {
                Console.WriteLine(
                    $"!!! Checksum mismatch: sent 0x{outgoingChecksum:X2}, " +
                    $"reply 0x{gamma_reply_checksum:X2}"
                );
                //MessageBox.Show("Checksum 不一樣!");
                return 255;
            }
            //else
            //{
            //    Console.WriteLine("Checksum OK");
            //}

            return 0;
        }
        static string HexToAscii(string hex)
        {
            string ascii = "";
            for (int i = 0; i < hex.Length; i += 2)
            {
                // 取兩個十六進制字符，轉換為數值，然後轉換為 ASCII 字符
                ascii += (char)Convert.ToInt32(hex.Substring(i, 2), 16);
            }
            return ascii;
        }

        public void I2C_CommandAnalyzer(string strCommand) ////240919 新增 
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;

            DateTime now = DateTime.Now;
            mainWindow.SetTheTextToListBoxCommand($"[{now:HH:mm:ss}] Read:\t{strCommand}");


            // 更新 UI
            mainWindow.Dispatcher.BeginInvoke(new Action(delegate
            {
                mainWindow.listBoxCommand.SelectedIndex = mainWindow.listBoxCommand.Items.Count - 1;
                mainWindow.listBoxCommand.ScrollIntoView(mainWindow.listBoxCommand.SelectedItem);
            }));



            ////以下ONYX規格用，暫時沒有用到

            // 轉換為字節陣列
            //var byteArrCommand = StringToByteArray(strCommand);
            //Console.WriteLine("byteArrCommand: " + byteArrCommand);

            //mainWindow.SetTheTextToListBoxCommand("Get reply: " +strCommand);  //1007改成MCCS

            //MReplyP monitorReplyPackage = new MReplyP(byteArrCommand);

            //// 檢查回覆狀態
            //switch (monitorReplyPackage.Status)
            //{
            //    case Status.STATUS_SUCCESS:
            //        if (byteArrCommand[0] == (byte)Destination.ONYX_MONITOR_HOST_ADDR && byteArrCommand[1] == (byte)Source.ONYX_MONITOR_CLIENT_ADDR)
            //        {
            //            if (byteArrCommand[3] == (byte)FactoryMode.ONYX_FACTORY_CMD_C0)
            //            {
            //                mainWindow.SetTheTextToListBoxCommand("Transmission successful via I2C");

            //                if (byteArrCommand[4] == (byte)OPCode.ONYX_CMD_63_GENERAL_SET_COMMAND)
            //                {
            //                    // 處理設置命令的邏輯
            //                }
            //                else if (byteArrCommand[4] == (byte)OPCode.ONYX_CMD_73_GENERAL_GET_COMMAND)
            //                {
            //                    mainWindow.replyCMDtoFactory = byteArrCommand;
            //                    switch (byteArrCommand[6])
            //                    {
            //                        case (byte)VCCode.ONYX_FCODE_05_GET_INPUT_SOURCE:
            //                            string tempInputSource = monitorReplyPackage.Data[0] == 0x00 ? "HDMI" : "DP";
            //                            mainWindow.SetTheTextToListBoxCommand($"Input Source:\t{tempInputSource}");
            //                            mainWindow.SetTheTextToTextBoxInputSource(tempInputSource);
            //                            break;
            //                        case (byte)VCCode.ONYX_FCODE_12_GET_SERIAL_NUMBER:
            //                            string tempSerialNumber = System.Text.Encoding.ASCII.GetString(monitorReplyPackage.Data);
            //                            mainWindow.SetTheTextToListBoxCommand($"Serial Number:\t{tempSerialNumber}");
            //                            mainWindow.SetTheTextToTextBoxSerialNumber(tempSerialNumber);
            //                            break;
            //                        case (byte)VCCode.ONYX_FCODE_13_GET_FIRMWARE_VERSION:
            //                            string tempFirmwareVersion = System.Text.Encoding.ASCII.GetString(monitorReplyPackage.Data);
            //                            mainWindow.SetTheTextToListBoxCommand($"Firmware Version:\t{tempFirmwareVersion}");
            //                            mainWindow.SetTheTextToTextBoxFirmwareVersion(tempFirmwareVersion);
            //                            break;
            //                        default:
            //                            break;
            //                    }
            //                }
            //            }
            //        }
            //        if (_receiveCompletionSource != null)
            //        {
            //            _receiveCompletionSource.SetResult(true);
            //        }
            //        factoryWindow.factoryGridCommandIsComplete(StringToByteArray(strCommand));
            //        break;
            //    case Status.STATUS_CHKSUM_NG:
            //        mainWindow.SetTheTextToListBoxCommand($"The command sequence checksum incorrect via I2C");
            //        break;
            //    case Status.STATUS_TIMEOUT:
            //        mainWindow.SetTheTextToListBoxCommand($"Don't reply while time via I2C");
            //        break;
            //    case Status.STATUS_INVALID_CMD:
            //        mainWindow.SetTheTextToListBoxCommand($"The instructions code don't support via I2C");
            //        break;
            //    case Status.STATUS_INVALID_FUNC:
            //        mainWindow.SetTheTextToListBoxCommand($"There are instructions but no functions via I2C");
            //        break;
            //    default:
            //        break;
            //}
        }




        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                 USB plug/unplug observe Layer                              #####################
        //###################################################################################################################################
        //###################################################################################################################################
        public class HotPlug
        {
            public delegate void DelegateHotPlug(object sender, EventArrivedEventArgs e);
            public void Register(
           DelegateHotPlug DeviceInsertedEvent, DelegateHotPlug DeviceRemovedEvent)
            {
                insertQuery = new WqlEventQuery(
                "SELECT * FROM __InstanceCreationEvent " +
                "WITHIN 2 " +
                "WHERE TargetInstance ISA 'Win32_USBHub'"
                );
                insertWatcher = new ManagementEventWatcher(insertQuery);
                insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
                insertWatcher.Start();
                removeQuery = new WqlEventQuery(
                "SELECT * FROM __InstanceDeletionEvent " +
                "WITHIN 2 " +
                "WHERE TargetInstance ISA 'Win32_USBHub'"
                );
                removeWatcher = new ManagementEventWatcher(removeQuery);
                removeWatcher.EventArrived += new EventArrivedEventHandler(DeviceRemovedEvent);
                removeWatcher.Start();
            }
            public void Unregister()
            {
                insertWatcher.Stop();
                insertWatcher.Dispose();
                removeWatcher.Stop();
                removeWatcher.Dispose();
            }
            private WqlEventQuery insertQuery = null;
            private WqlEventQuery removeQuery = null;

            private ManagementEventWatcher insertWatcher = null;
            private ManagementEventWatcher removeWatcher = null;
        }
        private void HotPlugDeviceInserted(object sender, EventArrivedEventArgs e)
        {
            var instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];

            foreach (var property in instance.Properties)
            {
                if (property.Name.Equals("Caption"))
                {
                    if (!property.Value.Equals("USB Composite Device"))
                    {
                        // Query the D3XX devices connected to the system
                        // if no device is currently opened
                        Console.WriteLine("FTDI USD Insert !!!!!!!!!!!!!!");
                        break;
                    }
                }
            }

        }
        private void HotPlugDeviceRemoved(object sender, EventArrivedEventArgs e)
        {
            var instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            foreach (var property in instance.Properties)
            {
                if (property.Name.Equals("DeviceID"))
                {
                    string strValue = (string)property.Value;
                    if (strValue.Contains("VID_0403") && strValue.Contains("PID_6010"))
                    {
                        Console.WriteLine(property.Value);
                        Console.WriteLine("FTDI USD Remove !!!!!!!!!!!!!!");

                        //Close FTDI device
                        DeviceOpen = false;
                        //this.Dispatcher.Invoke(new Action(() =>
                        //{
                        //    DisconnectProcess();
                        //}));

                    }
                    break;
                }
            }
            //Console.WriteLine("Remove!!!");
        }
    }

}

