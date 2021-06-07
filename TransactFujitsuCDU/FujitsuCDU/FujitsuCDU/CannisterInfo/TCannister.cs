using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FujitsuCDU.FujitsuCDUProcessor;

namespace FujitsuCDU
{
    class TCannister
    {

        public int HostCycleCount { get; set; }
        public int Dispensed { get; set; }
        public int Position { get; set; }
        public TCanState State { get; set; }
    }
}
