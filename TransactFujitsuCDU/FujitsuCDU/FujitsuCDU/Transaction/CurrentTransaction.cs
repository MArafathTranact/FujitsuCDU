using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU.Transaction
{
    public class CurrentTransaction
    {
        public int VoidCode { get; set; }

        public int[] CannDisp = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        public decimal Dispensed { get; set; }
    }
}
