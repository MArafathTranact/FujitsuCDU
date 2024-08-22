using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU
{
    public class Device
    {
        public int dev_id { get; set; }
        public string description { get; set; }
        public string ip_address { get; set; }
        public int? PortNbr { get; set; }
        public int? online { get; set; }
        public string CompanyNbr { get; set; }
        public string load_file { get; set; }
        public string receipt_file { get; set; }
        public bool Connected { get; set; }
        public bool Reconnected { get; set; }
        public string address { get; set; }
        public string Payment_Type { get; set; }
        public string ChkCashCompanyNbr { get; set; }
        public bool? edge_atm { get; set; }
        public string coin_dev_id { get; set; }
        public string dev_typeID { get; set; }
        public int? Bills_In_Bundle { get; set; }
        public short? Comm_Port { get; set; }
        public int? Baud_Rate { get; set; }
        public string welcome1 { get; set; }
        public string welcome2 { get; set; }

        public bool? use_jpegger { get; set; }

        public string jpegger_addr { get; set; }
        public string camera_group { get; set; }
        public int? jpegger_port { get; set; }
        public int? capture_interval { get; set; }
        public int? retries { get; set; }
        public int? image_interval { get; set; }
        public int? num_images { get; set; }

        public string yardid { get; set; }

        public int? round_method { get; set; }

    }
}
