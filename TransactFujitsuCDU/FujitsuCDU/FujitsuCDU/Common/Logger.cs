using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU
{
    public static class Logger
    {
        public static object _locked = new object();
        private static string folder = GetAppettingValue("Trace");
        private static int logsize = int.Parse(GetAppettingValue("LogSize"));
        private static string ExceptionLog = GetAppettingValue("ExceptionLog");
        public static ConcurrentQueue<string> logmessages = new ConcurrentQueue<string>();
        private static bool IsloggingStopped = false;

        static Logger()
        {
            Task.Factory.StartNew(() =>
            {
                LogEnqueuedMessage();
            });
        }

        private static void LogEnqueuedMessage()
        {
            while (true)
            {
                try
                {
                    var input = string.Empty;
                    while (logmessages.TryDequeue(out input))
                        LogMessages(input);

                }
                catch (Exception ex)
                {
                    IsloggingStopped = true;
                    LogMessages($"Dequeue Error : {ex.Message}");
                }
            }

        }

        public static void LogWithNoLock(string message)
        {
            logmessages.Enqueue(message);
            if (IsloggingStopped)
            {
                Task.Factory.StartNew(() =>
                {
                    LogEnqueuedMessage();
                });
            }
        }


        public static void LogMessages(string message)
        {
            try
            {
                if (!string.IsNullOrEmpty(message))
                {
                    CreateLogFile(folder);

                    FileInfo fi = new FileInfo(folder);
                    var size = fi.Length >> 20;
                    var fileMode = size >= logsize ? FileMode.Truncate : FileMode.Append;

                    using (var fs = new FileStream(folder, fileMode, FileAccess.Write, FileShare.Write))
                    using (var sw = new StreamWriter(fs))
                    {
                        sw.WriteLine(message);
                    }
                }

            }
            catch (Exception ex)
            {
                LogExceptionMessages($"Message : {message} ; Exception : {ex.Message}");
            }

        }

        public static void LogExceptionMessages(string message)
        {
            try
            {
                CreateLogFile(ExceptionLog);

                FileInfo fi = new FileInfo(ExceptionLog);
                var size = fi.Length >> 20;
                var fileMode = size >= logsize ? FileMode.Truncate : FileMode.Append;

                using (var fs = new FileStream(ExceptionLog, fileMode, FileAccess.Write, FileShare.Write))
                using (var sw = new StreamWriter(fs))
                {
                    sw.WriteLine(message);
                }

            }
            catch (Exception ex)
            {
                //LogWithNoLock($"{message}");
            }

        }

        private static void CreateLogFile(string folder)
        {
            if (!File.Exists(folder))
            {
                using (File.Create(folder)) { }
            }
        }
        public static string GetAppettingValue(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public static int GetSystemValue(string key)
        {
            return int.Parse(ConfigurationManager.AppSettings[key]);
        }

    }


    //public class Logger
    //{
    //    public static object _locked = new object();

    //    public static void LogWithNoLock(string message)
    //    {
    //        string folder = GetFileLocation("Trace");
    //        if (!File.Exists(folder))
    //        {
    //            using (FileStream fs = File.Create(folder))
    //            {
    //                byte[] info = new UTF8Encoding(true).GetBytes(message);
    //                fs.Write(info, 0, info.Length);

    //            }
    //            //// Create a file to write to.
    //            //using (StreamWriter sw = File.CreateText(folder))
    //            //{
    //            //    sw.WriteLine($"{message}");

    //            //}

    //        }
    //        else
    //        {

    //            //using (StreamWriter sw = File.AppendText(folder))
    //            //{
    //            //    //sw.WriteLine($"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}{deviceId} {message}");
    //            //    sw.WriteLine($"{message}");

    //            //}               
    //            using (var fs = new FileStream(folder, FileMode.Append, FileAccess.Write, FileShare.Write))
    //            using (var sw = new StreamWriter(fs))
    //            {
    //                sw.WriteLine(message);
    //            }

    //        }

    //    }

    //    private static string GetFileLocation(string keyvalue)
    //    {
    //        return ConfigurationManager.AppSettings[keyvalue];
    //    }
    //}
}
