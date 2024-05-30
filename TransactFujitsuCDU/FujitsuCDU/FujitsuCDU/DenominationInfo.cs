using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FujitsuCDU.Common;

namespace FujitsuCDU
{
    public class DenominationInfo
    {
        //private readonly Logger logger = new Logger();
        private readonly API api = new API();


        [JsonProperty(PropertyName = "denomination")]
        public string denomination { get; set; }

        [JsonProperty(PropertyName = "dev_id")]
        public string dev_id { get; set; }

        [JsonProperty(PropertyName = "cassette_nbr")]
        public string cassette_nbr { get; set; }

        [JsonProperty(PropertyName = "cassette_id")]
        public string cassette_id { get; set; }

        [JsonProperty(PropertyName = "currency_type")]
        public string currency_type { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string status { get; set; }

        [JsonProperty(PropertyName = "host_start_count")]
        public int? host_start_count { get; set; }

        [JsonProperty(PropertyName = "host_cycle_count")]
        public int? host_cycle_count { get; set; }

        [JsonProperty(PropertyName = "dev_start_count")]
        public int? dev_start_count { get; set; }

        [JsonProperty(PropertyName = "dev_cycle_count")]
        public int? dev_cycle_count { get; set; }

        [JsonProperty(PropertyName = "dev_divert_count")]
        public int? dev_divert_count { get; set; }

        [JsonProperty(PropertyName = "added_count")]
        public string added_count { get; set; }

        [JsonProperty(PropertyName = "old_added")]
        public string old_added { get; set; }

        public List<DenominationInfo> GetDenominationInfos(string deviceId)
        {
            List<DenominationInfo> denoms = new List<DenominationInfo>();
            try
            {
                denoms = api.GetRequest<List<DenominationInfo>>($"bill_counts?dev_id={deviceId}", deviceId, out bool databaseError);
                return denoms.OrderBy(x => x.cassette_id).ToList();
            }
            catch (Exception ex)
            {
                Logger.LogWithNoLock($"{DateTime.Now:MM-dd-yyyy HH:mm:ss.fff}: Dev {deviceId} : {ex.Message}");
                return denoms;
            }
        }
    }
}
