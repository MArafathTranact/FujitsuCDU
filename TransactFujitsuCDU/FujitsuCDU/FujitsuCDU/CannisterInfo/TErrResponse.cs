using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU
{
    public class TErrResponse
    {
        public byte DH0 { get; set; }
        public byte DH1 { get; set; }
        public byte DH2 { get; set; }
        public byte V1 { get; set; }
        public byte V2 { get; set; }
        public byte POM { get; set; }

        public byte[] ErrorAddress = new byte[2];//{ get; set; }

        public string[] ErrorCode = new string[2];//{ get; set; }

        public byte[] ErrorRegister = new byte[2]; //{ get; set; }

        public byte[] SensorRegister = new byte[5];//{ get; set; }

        public byte[] CassetteRegister = new byte[4];//{ get; set; }

        public byte[] CassStatusChanges = new byte[4];// { get; set; }

        public byte[,] BillLength = new byte[4, 2];//{ get; set; }//BillLength: Array[1..4, 0..1] of Byte;

        public byte[] BillThickness = new byte[4];//{ get; set; }



        //    ErrorRegister: Array[0..2] of Byte;
        //SensorRegister: Array[0..5] of Byte;
        //CassetteRegister: Array[1..4] of Byte;
        //POM: Byte;
        //BillLength: Array[1..4, 0..1] of Byte;
        //BillThickness: Array[1..4] of Byte;
        //CassStatusChanges: Array[1..4] of Byte;
    }
}
