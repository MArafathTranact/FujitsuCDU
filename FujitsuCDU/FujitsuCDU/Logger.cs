using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU
{
    public class Logger
    {
        public static object _locked = new object();

        public void Log(string message)
        {
            lock (_locked)
            {
                string folder = GetFileLocation("Trace");
                if (!File.Exists(folder))
                {
                    // Create a file to write to.
                    using (StreamWriter sw = File.CreateText(folder))
                    {
                        sw.WriteLine($"{message}");

                    }
                }
                else
                {
                    // This text is always added, making the file longer over time
                    // if it is not deleted.
                    using (StreamWriter sw = File.AppendText(folder))
                    {
                        //sw.WriteLine($"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}{deviceId} {message}");
                        sw.WriteLine($"{message}");

                    }
                }



            }


        }

        private string GetFileLocation(string keyvalue)
        {
            return ConfigurationManager.AppSettings[keyvalue];
        }
    }
}
