using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU
{
    public class TTotalsRequest
    {
        public byte DH0 { get; set; }
        public byte DH1 { get; set; }
        public byte DH2 { get; set; }
        public byte RSV { get; set; }

        public byte[] DH3 = new byte[2];//{ get; set; }
        public byte[] ODR = new byte[2];//{ get; set; }
        public byte[] N1 = new byte[2];//{ get; set; }
        public byte[] R1 = new byte[2];//{ get; set; }
        public byte[] P1 = new byte[2];//{ get; set; }
        public byte[] N2 = new byte[2];//{ get; set; }
        public byte[] R2 = new byte[2];//{ get; set; }
        public byte[] P2 = new byte[2];//{ get; set; }

    }
}
