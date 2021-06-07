using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU.EZCashModule
{
    public class DispenseRequest
    {
        public string Barcode { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
    }
}
