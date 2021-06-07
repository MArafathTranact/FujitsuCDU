using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU.Common
{
   public class ServiceConfiguration
    {

        public string GetFileLocation(string name)
        {
            return ConfigurationManager.AppSettings[name];
        }
    }
}
