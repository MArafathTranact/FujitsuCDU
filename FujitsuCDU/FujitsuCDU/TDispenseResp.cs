using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU
{
    public class TDispenseResp
    {
        public byte[,] CountedBills = new byte[4, 2];

        public byte[,] Rejections = new byte[4, 2];

        public byte[] CassStats = new byte[4];
        public byte CountOrderAssgn { get; set; }

        public byte[,] BillCounts = new byte[4, 2];

        public byte[,] MaxCountRejects = new byte[4, 2];

        public byte[] MaxPickRetries = new byte[4];

        public byte[] SensorLevelInfo = new byte[15];

        public byte[,] CountedBills2 = new byte[4, 2];

        public byte[,] Rejections2 = new byte[4, 2];

        public byte[,] CassStats2 = new byte[4, 15];

    }
}
