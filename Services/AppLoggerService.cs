using System.IO;

namespace TimeMaker.Services
{
    public class AppLoggerService
    {
        private readonly string _logFolder;
        private readonly object _writeLock = new();

        public AppLoggerService(string logFolder)
        {
            _logFolder = logFolder;
            Directory.CreateDirectory(logFolder);
        }

        public void Log(string message)
        {
            WriteToLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}");
        }

        public void LogError(string message, Exception? ex = null)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}";
            if (ex != null)
            {
                logEntry += $"\n{ex.GetType().Name}: {ex.Message}";
                logEntry += $"\nStack Trace: {ex.StackTrace}";
            }
            WriteToLog(logEntry);
        }

        public void LogWarning(string message)
        {
            WriteToLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WARNING: {message}");
        }

        private void WriteToLog(string entry)
        {
            try
            {
                // Log calls come from the UI thread, timers and the serial port
                // thread at once - serialize the writes. The file name is
                // resolved per write so a run that crosses midnight rolls over.
                lock (_writeLock)
                {
                    string path = Path.Combine(_logFolder, $"Log_{DateTime.Now:yyyyMMdd}.txt");
                    if (!File.Exists(path))
                    {
                        File.WriteAllText(path, "=== Time Maker App Log ===\n\n");
                    }
                    File.AppendAllText(path, entry + Environment.NewLine);
                }
            }
            catch
            {
                // Logging must never block or interrupt the app - if the file
                // is unwritable there is nothing useful we can do about it here.
            }
        }
    }
}
