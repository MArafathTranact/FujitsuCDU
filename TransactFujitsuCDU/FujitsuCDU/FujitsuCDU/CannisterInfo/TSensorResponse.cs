using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU
{
    public class TSensorResponse
    {
        public byte PRS1 { get; set; }
        public byte PLS1 { get; set; }
        public byte PRS2 { get; set; }
        public byte PLS2 { get; set; }
        public byte PRS3 { get; set; }
        public byte PLS3 { get; set; }
        public byte PRS4 { get; set; }
        public byte PLS4 { get; set; }
        public byte BPRS { get; set; }
        public byte BPLS { get; set; }
        public byte GSRS { get; set; }
        public byte GSLS { get; set; }
        public byte CPS { get; set; }
        public byte RJS { get; set; }
        public byte Rsv1 { get; set; }
        public byte Rsv2 { get; set; }

    }
}
