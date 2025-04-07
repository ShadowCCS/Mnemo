using System;
using System.IO;
using System.Threading.Tasks;

namespace MnemoProject.Services
{
    public static class LogService
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt");
        private static readonly object LockObject = new object();

        public static class Log
        {
            public static void Error(string message)
            {
                WriteLog("ERROR", message);
            }

            public static void Warning(string message)
            {
                WriteLog("WARNING", message);
            }

            public static void Info(string message)
            {
                WriteLog("INFO", message);
            }

            public static void Debug(string message)
            {
                WriteLog("DEBUG", message);
            }

            public static void Write(string message)
            {
                WriteLog("LOG", message);
            }

            private static void WriteLog(string level, string message)
            {
                EnsureLogFileExists();
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                
                lock (LockObject)
                {
                    File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
                }
            }

            private static void EnsureLogFileExists()
            {
                if (!File.Exists(LogFilePath))
                {
                    lock (LockObject)
                    {
                        if (!File.Exists(LogFilePath))
                        {
                            File.Create(LogFilePath).Close();
                        }
                    }
                }
            }
        }
    }
} 