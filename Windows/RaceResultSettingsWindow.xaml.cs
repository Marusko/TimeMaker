using System.Windows;

namespace TimeMaker.Windows
{
    /// <summary>
    /// Interaction logic for RaceResultSettingsWindow.xaml
    /// </summary>
    public partial class RaceResultSettingsWindow
    {
        public RaceResultSettingsWindow()
        {
            InitializeComponent();
            App.RaceResult.RaceResultApiLoaded += OnRaceResultApiLoaded;
        }

        private void OnRaceResultApiLoaded(object? sender, Models.RaceResultApiLoadedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                PointsApiLabel.Content = e.PointsApiStatus;
                RawDataApiLabel.Content = e.ManualApiStatus;
                BibListApiLabel.Content = e.BibsApiStatus;
            });
        }

        private async void Load(object sender, RoutedEventArgs e)
        {
            try
            {
                await App.RaceResult.LoadApi(ApiLinkText.Text);
                SaveButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;
                MessageBox.Show($"Nastala chyba pri načítavaní API: {ex.Message}", "Chyba", button, icon, MessageBoxResult.OK);
            }
        }

        private async void Save(object sender, RoutedEventArgs e)
        {
            await App.RaceResult.Start();
            App.RaceResult.RaceResultApiLoaded -= OnRaceResultApiLoaded;
            Close();
        }
    }
}
