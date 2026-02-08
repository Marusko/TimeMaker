using System.Windows;
using System.Windows.Controls;
using TimeMaker.Models;
using TimeMaker.Services;

namespace TimeMaker.Windows
{
    /// <summary>
    /// Interaction logic for CreateSerialSourceWindow.xaml
    /// </summary>
    public partial class CreateSerialSourceWindow
    {
        private bool _stopwatch = true;
        public CreateSerialSourceWindow()
        {
            InitializeComponent();
            RefreshLogic();
        }

        private void ModeChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton button)
            {
                _stopwatch = button.Content.ToString()?.Contains("Stopwatch") ?? false;
            }
        }

        private void Test(object sender, RoutedEventArgs e)
        {
            var sps = new SerialPortService();
            sps.TestConnection(ComPortBox.Text);
        }

        private void Refresh(object sender, RoutedEventArgs e)
        {
            RefreshLogic();
        }

        private void RefreshLogic()
        {
            var ports = SerialPortService.GetAvailablePorts();
            ComPortBox.Items.Clear();
            foreach (var p in ports)
            {
                ComPortBox.Items.Add(p);
            }
            StartPoint.Items.Clear();
            foreach (var p in App.RaceResult.Points)
            {
                StartPoint.Items.Add(p.Name);
            }
            FinishPoint.Items.Clear();
            foreach (var p in App.RaceResult.Points)
            {
                FinishPoint.Items.Add(p.Name);
            }
            RunTimePoint.Items.Clear();
            foreach (var p in App.RaceResult.Points)
            {
                RunTimePoint.Items.Add(p.Name);
            }
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ComPortBox.Text) || string.IsNullOrEmpty(StartPoint.Text) || string.IsNullOrEmpty(FinishPoint.Text) || string.IsNullOrEmpty(NameText.Text))
            {
                MessageBox.Show("Prosím zadajte všetky potrebné dáta", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                var window = new SerialFlagsSettingsWindow(!_stopwatch);
                window.ShowDialog();
                var flags = window.GetFlags();
                var init = new SerialSourceInitModel()
                {
                    Name = NameText.Text,
                    Source = ComPortBox.Text,
                    FirstTarget = new ApiTimingPoint() { Name = StartPoint.Text },
                    SecondTarget = new ApiTimingPoint() { Name = FinishPoint.Text },
                    ThirdTarget = new ApiTimingPoint() { Name = RunTimePoint.Text },
                    Mode = _stopwatch ? TimyMode.Stopwatch : TimyMode.Backup,
                    Flags = flags
                };
                App.SourceManager.AddSource(typeof(SerialPortService), init);
                Close();
            }
        }
    }
}
