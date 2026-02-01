using System.Diagnostics;
using System.Windows;

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
            throw new NotImplementedException();
        }

        private void StopSource(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ViewRawSource(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void RemoveSource(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
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
    }
}