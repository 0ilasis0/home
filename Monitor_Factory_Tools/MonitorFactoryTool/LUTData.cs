using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorFactoryTool
{
    public static class Parameters
    {
        public const int RED_CH = 0;
        public const int GREEN_CH = 1;
        public const int BLUE_CH = 2;
        public const int WHITE_CH = 2;
    }

    public class LUTData
    {
        public int[][] mLUTData; //250707 wu  modify to public 為了 FHD 4K 通用
        public uint mLUTResolution;
        public uint mLUTEntry;

        public LUTData(uint LUTEntry, uint LUTResolution)
        {
            mLUTEntry = LUTEntry;
            mLUTResolution = LUTResolution;
            mLUTData = new int[3][];

            for (int index = Parameters.RED_CH; index <= Parameters.BLUE_CH; index++)
            {
                mLUTData[index] = new int[LUTEntry];
            }
        }

        public bool SetLUTData(int channel, int index, int data)
        {
            if (channel > Parameters.BLUE_CH || channel < Parameters.RED_CH)
                return false;

            if (index >= mLUTEntry)
                return false;

            if (data > mLUTResolution)
                return false;

            mLUTData[channel][index] = data;
            return true;
        }

        public int[] RLUT => mLUTData[Parameters.RED_CH];

        public int[] GLUT => mLUTData[Parameters.GREEN_CH];

        public int[] BLUT => mLUTData[Parameters.BLUE_CH];
    }
}
