using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU
{
    public class TCommonResp
    {
        public byte DH0 { get; set; }
        public byte DH1 { get; set; }
        public byte DH2 { get; set; }

        public byte[] DH3 = new byte[2];
        public byte Reserved1 { get; set; }
        public byte Reserved2 { get; set; }
        public byte E1 { get; set; }
        public byte E2 { get; set; }
        public byte V1 { get; set; }
        public byte V2 { get; set; }

        public byte[] ErrorRegister = new byte[3];

        public byte[] SensorRegister = new byte[6];

        public byte[] CassetteRegister = new byte[4];
        public byte POM { get; set; }

        public byte[,] BillLength = new byte[4, 2];

        public byte[] BillThickness = new byte[4];

        public byte[] CassStatusChanges = new byte[4];

        public byte[] DateInfo = new byte[4];

        public byte[] VerInfo = new byte[6];

        public byte[] ErrAddr = new byte[4];

        public byte[] Reserved = new byte[4];

        public byte[] ErrReg2 = new byte[4];

        public byte[] SensorReg2 = new byte[4];

        public byte[,] BillLength2 = new byte[4, 2];

        public byte[] BillThickness2 = new byte[4];

        public byte[] CassStatusChanges2 = new byte[4];
        public string ErrorCode { get; set; }
    }
}
