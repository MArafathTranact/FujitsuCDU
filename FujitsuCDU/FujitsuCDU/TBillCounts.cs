using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU
{
    public class TBillCounts
    {
        public byte[,] CountedBills = new byte[4, 2];
        public byte[,] BillRejects = new byte[4, 2];
        public byte[,] CassStats = new byte[4, 2];
        public byte[,] ReqBills = new byte[4, 2];
        public byte[,] MaxRejects = new byte[4, 2];
        public byte[] PickRetries = new byte[4];
        public byte RSV { get; set; }
        public TSensorResponse SensorData { get; set; }

    }
}
