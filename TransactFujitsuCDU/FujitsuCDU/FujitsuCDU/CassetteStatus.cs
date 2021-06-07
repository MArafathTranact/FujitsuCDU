using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU
{
    public class CassetteStatus
    {
        public static List<Info> cassetteStatusInfo = new List<Info> {

            new Info { Type="0",Value="0" },
            new Info { Type="1",Value="0" },
            new Info { Type="2",Value="4" },
            new Info { Type="3",Value="1" },
            new Info { Type="4",Value="2" }
        };
    }

    public class Info
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
