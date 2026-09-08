using System;
using System.IO;
using System.Linq;

namespace ONYX_DataType
{
    public static class EnumExtensions
    {
        public static T ToEnum<T>(this byte value) where T : Enum
        {
            if (!Enum.IsDefined(typeof(T), value))
            {
                //throw new ArgumentException($"Value {value} is not defined in enum {typeof(T)}");
            }

            return (T)Enum.ToObject(typeof(T), value);
        }
    }

    public enum Destination : byte
    {
        ONYX_MONITOR_HOST_ADDR = 0x6F,  //PC
        ONYX_MONITOR_CLIENT_ADDR = 0x6E,  //AD Board
        ONYX_MONITOR_STM_ADDR = 0x4E,  //STM32
    }

    public enum Source : byte
    {
        ONYX_MONITOR_HOST_ADDR = 0x51, //PC
        ONYX_MONITOR_CLIENT_ADDR = 0x6E,  //AD Board 
        ONYX_MONITOR_STM_ADDR = 0x4E,  //STM32

    }

    public enum Length : byte
    {

    }

    public enum FactoryMode : byte
    {
        ONYX_FACTORY_CMD_C0 = 0xC0,    // Factory Code
        ONYX_FACTORY_CMD_D0 = 0xD0,    // Special Code
    }

    public enum OPCode : byte
    {
        ONYX_CMD_63_GENERAL_SET_COMMAND = 0x63,   // General set type
        ONYX_CMD_73_GENERAL_GET_COMMAND = 0x73,   // General get type
        ONYX_CMD_65_SPECIAL_SET_COMMAND = 0x65,   // Special set type
        ONYX_CMD_75_SPECIAL_GET_COMMAND = 0x75,   // Special get type
    }

    public enum VCCode : byte
    {
        ONYX_FCODE_DELAY = 0x99,//zh251117 add
        // For general set command (0x63)
        ONYX_FCODE_01_SET_POWER = 0x01,
        ONYX_FCODE_02_SET_FACTORY_MODE = 0x02,
        ONYX_FCODE_03_RESET_FACTORY = 0x03,
        ONYX_FCODE_04_SET_BURN_IN_MODE = 0x04,
        ONYX_FCODE_05_SET_INPUT_SOURCE = 0x05,
        ONYX_FCODE_06_SET_EDID_WP = 0x06,
        ONYX_FCODE_07_SET_COLOR_TMEP = 0x07,
        ONYX_FCODE_08_SET_GAMMA_DICOM = 0x08,
        ONYX_FCODE_09_SET_BACKLIGHT_LED_ON_OFF = 0x09,
        ONYX_FCODE_0A_SET_BRIGHTNESS = 0x0A,
        ONYX_FCODE_0B_SET_CONTRAST = 0x0B,
        ONYX_FCODE_0C_SET_OSD_MENU = 0x0C,
        ONYX_FCODE_0D_SET_SHARPNESS = 0x0D,
        ONYX_FCODE_0E_SET_OSD_LANGUAGE = 0x0E,
        ONYX_FCODE_0F_SET_RGB_GAIN = 0x0F,
        ONYX_FCODE_10_SET_AUDIO_MUTE = 0x10,
        ONYX_FCODE_11_SET_AUDIO_VOLUME = 0x11,
        ONYX_FCODE_15_SET_ALC_MODE = 0x15,

        // For general get command (0x73)
        ONYX_FCODE_02_GET_FACTORY_MODE = 0x02,
        ONYX_FCODE_04_GET_BURN_IN_MODE = 0x04,
        ONYX_FCODE_05_GET_INPUT_SOURCE = 0x05,
        ONYX_FCODE_06_GET_EDID_WP = 0x06,
        ONYX_FCODE_07_GET_COLOR_TMEP = 0x07,
        ONYX_FCODE_08_GET_GAMMA_DICOM = 0x08,
        ONYX_FCODE_09_GET_BACKLIGHT_LED_ON_OFF = 0x09,
        ONYX_FCODE_0A_GET_BRIGHTNESS = 0x0A,
        ONYX_FCODE_0B_GET_CONTRAST = 0x0B,
        ONYX_FCODE_0D_GET_SHARPNESS = 0x0D,
        ONYX_FCODE_0E_GET_OSD_LANGUAGE = 0x0E,
        ONYX_FCODE_0F_GET_RGB_GAIN = 0x0F,
        ONYX_FCODE_10_GET_AUDIO_MUTE = 0x10,
        ONYX_FCODE_11_GET_AUDIO_VOLUME = 0x11,
        ONYX_FCODE_12_GET_SERIAL_NUMBER = 0x12,
        ONYX_FCODE_13_GET_FIRMWARE_VERSION = 0x13,
        ONYX_FCODE_14_GET_TEMPERATURE = 0x14,
        ONYX_FCODE_15_GET_ALC_MODE = 0x15,

        // For special set command (0x65)
        ONYX_FCODE_00_SET_HDCP_KEY = 0x00,
        ONYX_FCODE_01_SET_EDID = 0x01,
        ONYX_FCODE_02_SET_ALC_CORRECT = 0x02,
        ONYX_FCODE_05_SET_WRITE_GAMMA_LUT = 0x05,
        ONYX_FCODE_06_SET_Factory_RGB_Gain = 0x06,
        ONYX_FCODE_0A_SET_GAMMA_ERASE = 0x0B,//zh251124 add

        // For special get command (0x75)
        ONYX_FCODE_00_GET_HDCP_KEY = 0x00,
        ONYX_FCODE_01_GET_EDID = 0x01,
        ONYX_FCODE_02_GET_ALC_CORRECT = 0x02,
        ONYX_FCODE_03_GET_ALC_SENSOR = 0x03,
        ONYX_FCODE_04_GET_ALC_DETAIL = 0x04,
        ONYX_FCODE_05_GET_READ_GAMMA_LUT = 0x05,
        ONYX_FCODE_06_GET_Factory_RGB_Gain = 0x06,
        ONYX_FCODE_07_GET_COLOR_TEMP_DETAIL = 0x07,
        ONYX_FCODE_08_GET_GAMMA_DETAIL = 0x08,
        ONYX_FCODE_09_GET_PANEL_DETAIL = 0x09,
    }

    public enum Status : byte
    {
        STATUS_SUCCESS = 0xE0,
        STATUS_CHKSUM_NG = 0xE1,
        STATUS_TIMEOUT = 0xE2,
        STATUS_INVALID_CMD = 0xE3,
        STATUS_INVALID_FUNC = 0xE4,
    }

    public class MonitorRequestPackage
    {
        public byte Destination { get; }
        public byte Source { get; }
        public byte Length { get; }
        public FactoryMode FactoryMode { get; }
        public OPCode OPCode { get; }
        public VCCode VCCode { get; }
        public byte[] Data { get; }
        public byte CheckSum { get; }
        public byte[] Serialize { get; }

        public MonitorRequestPackage(byte destination, byte source, FactoryMode _FactoryMode, OPCode _OPCode, VCCode _VCCode, byte[] data)
        {
            Destination = destination;
            Source = source;
            Length = (byte)((data.Length + 4) | 0x80);
            FactoryMode = _FactoryMode;
            OPCode = _OPCode;
            VCCode = _VCCode;
            Data = data;
            CheckSum = 0x00;
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write(Destination);
                    bw.Write(Source);
                    bw.Write(Length);
                    bw.Write((byte)FactoryMode);
                    bw.Write((byte)OPCode);
                    bw.Write((byte)0x07);
                    bw.Write((byte)VCCode);
                    bw.Write(Data);
                    bw.Write((byte)0x00);
                    Serialize = ms.ToArray();
                }
            }
            foreach (var item in Serialize)
            {
                CheckSum ^= item;
            }
            Serialize[Serialize.Length - 1] = CheckSum;
        }
        public override string ToString()
        {
            return $"Dest: {Destination:X2}, Src: {Source:X2}, Len: {Length:X2}, FactoryMode: {FactoryMode}, OPCode: {OPCode}, VCCode: {VCCode}, Data: {BitConverter.ToString(Data):X2}, CheckSum: {CheckSum:X2}";
        }
    }

    public class MonitorReplyPackage
    {
        public byte Destination { get; }
        public byte Source { get; }
        public byte Length { get; }
        public FactoryMode FactoryMode { get; }
        public OPCode OPCode { get; }
        public VCCode VCCode { get; }
        public byte[] Data { get; }
        public Status Status { get; }
        public byte CheckSum { get; }
        public byte[] Serialize { get; }

        public MonitorReplyPackage(byte[] arr)
        {
            Destination = arr[0];
            Source = arr[1];
            Length = arr[2];
            FactoryMode = (FactoryMode)arr[3];
            OPCode = arr[4].ToEnum<OPCode>();
            VCCode = arr[6].ToEnum<VCCode>();
            Data = arr.Skip(7).Take(arr.Length - 9).ToArray();
            Status = arr[arr.Length - 2].ToEnum<Status>();
            CheckSum = arr[arr.Length - 1];
            Serialize = arr;
        }
        public override string ToString()
        {
            return $"Dest: {Destination:X2}, Src: {Source:X2}, Len: {Length:X2}, FactoryMode: {FactoryMode}, OPCode: {OPCode}, VCCode: {VCCode}, Data: {BitConverter.ToString(Data):X2}, Status: {Status}, CheckSum: {CheckSum:X2}";
        }
    }
    public enum BlackWindowSource   //20250502 wu add  判斷blackwindow的進入點 
    {
        Calibration,
        Verification
    }
}
