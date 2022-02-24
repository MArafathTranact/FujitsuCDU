using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FujitsuCDU
{
    public static class Logger
    {
        public static object _locked = new object();
        private static string folder = GetAppettingValue("Trace");
        private static int logsize = int.Parse(GetAppettingValue("LogSize"));
        private static string ExceptionLog = GetAppettingValue("ExceptionLog");
        public static Queue<string> logmessages = new Queue<string>();
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
                    if (logmessages.Count > 0)
                        LogMessages(logmessages.Dequeue());
                    else
                    {
                        Thread.Sleep(500);
                    }


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

}
