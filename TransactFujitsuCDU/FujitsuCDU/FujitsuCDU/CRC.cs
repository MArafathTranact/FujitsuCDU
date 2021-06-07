using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU
{
    public class CRC
    {
        private readonly int[] ROctets = { 0, 8, 4, 12, 2, 10, 6, 14, 1, 9, 5, 13, 3, 11, 7, 15 };

        private int Crc16(ref byte[] Buffer, int Length, int Polynom, int Initial)
        {
            int i;
            int j;
            for (i = 0; i < Length; i++)
            {
                Initial = Initial ^ ((int)Buffer[i] << 8);
                for (j = 0; j < 8; j++)
                {
                    if ((Initial & 0x8000) != 0)
                    {
                        Initial = (Initial << 1) ^ Polynom;
                    }
                    else
                    {
                        Initial <<= 1;
                    }
                }
            }
            return Initial & 0xffff;
        }


        private int ByteReverse(ref byte[] S, int Length)
        {
            for (int i = 0; i < Length; i++)
            {
                S[i] = (byte)((ROctets[S[i] & 0xf] << 4) + ROctets[S[i] >> 4]);
            }

            return Length;
        }

        public void FujiCRC(ref byte[] S, int Length, ref byte[] result)
        {
            ByteReverse(ref S, Length);
            int i = Crc16(ref S, Length, 0x1021, 0);
            ByteReverse(ref S, Length); //Restore original message
            result[0] = (byte)(i >> 8);
            result[1] = (byte)(i & 255);
            ByteReverse(ref result, 2);
        }

    }
}
