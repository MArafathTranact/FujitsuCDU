using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU
{
    public class TCassetteStats
    {
        public byte LengthLongErrors { get; set; }
        public byte LengthShortErrors { get; set; }
        public byte SkewErrors { get; set; }
        public byte ThicknessErrors { get; set; }
        public byte SpacingErrors { get; set; }
        public byte PickFromWrongCassetteErrors { get; set; }
        public byte PickErrorsBillsNotLow { get; set; }
        public byte CountUnmatchErrors { get; set; }
        public byte PickRetries { get; set; }
        public byte CountRetries { get; set; }
        public byte RetriesAfterAutoReject { get; set; }
        public byte JamErrors { get; set; }
        public byte PrintedLineCounter { get; set; }
        public byte BillDetectAfterCount { get; set; }
        public byte Rsv1 { get; set; }
        public byte Rsv2 { get; set; }

    }
}
