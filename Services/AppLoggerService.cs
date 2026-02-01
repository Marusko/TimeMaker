using System.IO;
using System.Windows;

namespace TimeMaker.Services
{
    public class AppLoggerService
    {
        private readonly string _logFilePath;

        public AppLoggerService(string logFolder)
        {
            // Create a log file with timestamp in name
            string timestamp = DateTime.Now.ToString("yyyyMMdd");
            _logFilePath = Path.Combine(logFolder, $"Log_{timestamp}.txt");

            // Ensure the directory exists
            Directory.CreateDirectory(logFolder);

            if (!File.Exists(_logFilePath))
            {
                // Create the log file with header
                File.WriteAllText(_logFilePath, "=== Ad Display App Log ===\n\n");
            }
        }

        public void Log(string message)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}";
            WriteToLog(logEntry);
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
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WARNING: {message}";
            WriteToLog(logEntry);
        }

        private void WriteToLog(string entry)
        {
            try
            {
                // Append to the log file
                using (StreamWriter writer = File.AppendText(_logFilePath))
                {
                    writer.WriteLine(entry);
                }
            }
            catch
            {
                // If we can't write to the log file, there's not much we can do
                MessageBox.Show("Failed to write to log file.", "Logging Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
