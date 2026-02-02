using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using TimeMaker.Models;
using TimeMaker.Services;

namespace TimeMaker.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            App.RaceResult.RaceResultApiLoaded += OnApiLoaded;
        }

        private void BtnAddFile_Click(object sender, RoutedEventArgs e)
        {
            var window = new CreateFileSourceWindow();
            window.ShowDialog();
        }

        private void BtnAddTimy_Click(object sender, RoutedEventArgs e)
        {
            var window = new CreateSerialSourceWindow();
            window.ShowDialog();
        }

        private void BtnRaceSettings_Click(object sender, RoutedEventArgs e)
        {
            var window = new RaceResultSettingsWindow();
            window.ShowDialog();
        }

        private void StartSource(object sender, RoutedEventArgs e)
        {
            var link = sender as Hyperlink;
            var source = link?.DataContext as SourceService;
            source?.Start();
        }

        private void StopSource(object sender, RoutedEventArgs e)
        {
            var link = sender as Hyperlink;
            var source = link?.DataContext as SourceService;
            source?.Stop();
        }

        private void ViewRawSource(object sender, RoutedEventArgs e)
        {
            var link = sender as Hyperlink;
            if (link?.DataContext is SourceService source)
            {
                var statusWindow = new StatusWindow();
                //statusWindow.SetSourceService(source);
                statusWindow.Show();
                App.SourceManager.AddWindow(source.Id, statusWindow);
            }
        }

        private void RemoveSource(object sender, RoutedEventArgs e)
        {
            var link = sender as Hyperlink;
            if (link?.DataContext is SourceService source)
            {
                App.SourceManager.RemoveSource(source.Id);
            }
        }

        private void OnApiLoaded(object? sender, RaceResultApiLoadedEventArgs e)
        {
            BtnAddFile.IsEnabled = true;
            BtnAddTimy.IsEnabled = true;
            BtnRaceSettings.IsEnabled = false;
        }

        private void OpenJson(object sender, RoutedEventArgs e)
        {
            const string url = "https://www.newtonsoft.com/json";
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }

        private void OpenIo(object sender, RoutedEventArgs e)
        {
            const string url = "https://www.nuget.org/packages/system.io.ports/";
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }

        private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            App.RaceResult.RaceResultApiLoaded -= OnApiLoaded;
            App.SourceManager.StopAllSources();
            App.SourceManager.CloseAllStatusWindows();
        }
    }
}