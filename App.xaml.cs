using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using TimeMaker.Services;

namespace TimeMaker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static string AppDataFolder { get; set; } = string.Empty;
        private static string LogFolder { get; set; } = string.Empty;
        public static AppLoggerService Logger { get; private set; }
        public static RaceResultService RaceResult { get; private set; }
        public static SourceManagerService SourceManager { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize application folders
            AppDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TimeMaker");

            LogFolder = Path.Combine(AppDataFolder, "Logs");

            // Create directories if they don't exist
            Directory.CreateDirectory(AppDataFolder);
            Directory.CreateDirectory(LogFolder);

            // Initialize logger
            Logger = new AppLoggerService(LogFolder);
            Logger.Log("Application starting up");

            // Initialize race result service
            RaceResult = new RaceResultService();

            // Initialize source manager service
            SourceManager = new SourceManagerService();

            // Handle unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                Logger.LogError("Unhandled exception", ex);
                MessageBox.Show($"An unhandled exception occurred: {ex?.Message}\nThe application will now close.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Log("Application shutting down");
            RaceResult.Dispose();
            SourceManager.Dispose();
            base.OnExit(e);
        }
    }

}
