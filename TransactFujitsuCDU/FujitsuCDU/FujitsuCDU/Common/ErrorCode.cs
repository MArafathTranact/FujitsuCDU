using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU.Common
{
    public class ErrorCode
    {
        public string Error { get; set; }
        public string MappingCan { get; set; }
        public string ErrorDescription { get; set; }

        public ErrorCode MapErrorCan(string errorcode)
        {
            var errordetails = GetErrorDeails();
            return errordetails.Where(x => x.Error == errorcode).FirstOrDefault();
        }

        private List<ErrorCode> GetErrorDeails()
        {

            var errorList = new List<ErrorCode>() {
                new ErrorCode() { Error = "1800", MappingCan = "01" ,ErrorDescription="1st Cassette pickup error"} ,
                new ErrorCode() { Error = "2800", MappingCan = "02" ,ErrorDescription="2nd Cassette pickup error"},
                new ErrorCode() { Error = "3800", MappingCan = "03" ,ErrorDescription="3rd Cassette pickup error"},
                new ErrorCode() { Error = "4800", MappingCan = "04" ,ErrorDescription="4th Cassette pickup error"},
                new ErrorCode() { Error = "1C00", MappingCan = "05" ,ErrorDescription="5th Cassette pickup error"},
                new ErrorCode() { Error = "2C00", MappingCan = "06" ,ErrorDescription="6th Cassette pickup error"},
                new ErrorCode() { Error = "3C00", MappingCan = "07" ,ErrorDescription="7th Cassette pickup error"},
                new ErrorCode() { Error = "4C00", MappingCan = "08" ,ErrorDescription="8th Cassette pickup error"},
                new ErrorCode() { Error = "1000", MappingCan = "01" ,ErrorDescription="No 1st Cassette"} ,
                new ErrorCode() { Error = "2000", MappingCan = "02" ,ErrorDescription="No 2nd Cassette"},
                new ErrorCode() { Error = "3000", MappingCan = "03" ,ErrorDescription="No 3rd Cassette"},
                new ErrorCode() { Error = "4000", MappingCan = "04" ,ErrorDescription="No 4th Cassette"},
                new ErrorCode() { Error = "1400", MappingCan = "05" ,ErrorDescription="No 5th Cassette"},
                new ErrorCode() { Error = "2400", MappingCan = "06" ,ErrorDescription="No 6th Cassette"},
                new ErrorCode() { Error = "3400", MappingCan = "07" ,ErrorDescription="No 7th Cassette"},
                new ErrorCode() { Error = "4400", MappingCan = "08" ,ErrorDescription="No 8th Cassette" },
                new ErrorCode() { Error = "A100", MappingCan = "01" ,ErrorDescription="Shutter open error SCS" },
                new ErrorCode() { Error = "A101", MappingCan = "02" ,ErrorDescription="Shutter open error SOS"},
                new ErrorCode() { Error = "A102", MappingCan = "03" ,ErrorDescription="Shutter open error sensor"},
                new ErrorCode() { Error = "A200", MappingCan = "11" ,ErrorDescription="Shutter close error SCS" },
                new ErrorCode() { Error = "A201", MappingCan = "12" ,ErrorDescription="Shutter close error SCS" },
                new ErrorCode() { Error = "A202", MappingCan = "13" ,ErrorDescription="Shutter close error sensor" }


            };

            return errorList;

        }
    }
}
