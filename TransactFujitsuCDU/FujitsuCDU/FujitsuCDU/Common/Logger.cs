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

        public static void LogWithNoLock(string message)
        {
            string folder = GetFileLocation("Trace");
            if (!File.Exists(folder))
            {
                using (FileStream fs = File.Create(folder))
                {
                    byte[] info = new UTF8Encoding(true).GetBytes(message);
                    fs.Write(info, 0, info.Length);

                }
                //// Create a file to write to.
                //using (StreamWriter sw = File.CreateText(folder))
                //{
                //    sw.WriteLine($"{message}");

                //}

            }
            else
            {

                //using (StreamWriter sw = File.AppendText(folder))
                //{
                //    //sw.WriteLine($"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}{deviceId} {message}");
                //    sw.WriteLine($"{message}");

                //}               
                using (var fs = new FileStream(folder, FileMode.Append, FileAccess.Write, FileShare.Write))
                using (var sw = new StreamWriter(fs))
                {
                    sw.WriteLine(message);
                }

            }

        }

        private static string GetFileLocation(string keyvalue)
        {
            return ConfigurationManager.AppSettings[keyvalue];
        }
    }
}
