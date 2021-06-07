using FujitsuCDU.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FujitsuCDU.FujitsuCDUProcessor;

namespace FujitsuCDU
{
    public class CannisterDispenserAttempt
    {
        private static API api = new API();
        public List<Cannister> cannistersList = GetCannisters();//{ get; set; }

        private static List<Cannister> GetCannisters()
        {
            try
            {
                var cannisters = api.GetRequest<List<Cannister>>(string.Empty, string.Empty, out bool databaseError);
                return cannisters;

            }
            catch (Exception)
            {

                throw;
            }
        }
    }
    public class Cannister
    {
        public int CanId { get; set; }
        public int Denoms { get; set; }
        public int host_cycle_count { get; set; }
        public int host_Start_count { get; set; }
        public int dev_id { get; set; }
        public int dispensed { get; set; }
        public TCanState State { get; set; }
    }
}
