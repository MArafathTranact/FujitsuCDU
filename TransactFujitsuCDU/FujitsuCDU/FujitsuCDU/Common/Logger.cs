using System;
using System.Collections.Generic;
using System.IO;
using NLog;


namespace FujitsuCDU
{
    public static class Logger
    {
        public static object _locked = new object();
        public static Queue<string> logmessages = new Queue<string>();
        private static ILogger logger { get; set; }
        static Logger()
        {
            logger = LogManager.GetCurrentClassLogger();

        }

        private static void LogInfo(string information)
        {
            try
            {
                logger.Info(information);
            }
            catch (Exception)
            {
            }
        }

        private static void LogError(string message, Exception exception)
        {
            try
            {
                logger.Error(exception, message);
            }
            catch (Exception)
            {
            }
        }

        private static void LogWarning(string message)
        {
            try
            {
                logger.Warn(message);
            }
            catch (Exception)
            {
            }
        }

        public static void LogWithNoLock(string message)
        {
            LogInfo(message);
        }

        public static void LogExceptionWithNoLock(string message, Exception exception)
        {
            LogError(message, exception);
        }

        public static void LogWarningWithNoLock(string message)
        {
            LogWarning(message);
        }



        public static void LogMessages(string message)
        {

        }

        public static void LogExceptionMessages(string message)
        {

        }

        private static void CreateLogFile(string folder)
        {
            if (!File.Exists(folder))
            {
                using (File.Create(folder)) { }
            }
        }

    }

}
