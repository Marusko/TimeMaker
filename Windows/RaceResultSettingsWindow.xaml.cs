using System.Windows;
using System.Windows.Media;

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
                PointsApiLabel.FontWeight = e.PointsApiLoaded ? FontWeights.Normal : FontWeights.Bold;
                PointsApiLabel.Background = e.PointsApiLoaded ? Brushes.LightGreen : Brushes.LightCoral;
                RawDataApiLabel.Content = e.ManualApiStatus;
                RawDataApiLabel.FontWeight = e.ManualApiLoaded ? FontWeights.Normal : FontWeights.Bold;
                RawDataApiLabel.Background = e.ManualApiLoaded ? Brushes.LightGreen : Brushes.LightCoral;
                BibListApiLabel.Content = e.BibsApiStatus;
                BibListApiLabel.FontWeight = e.BibsApiLoaded ? FontWeights.Normal : FontWeights.Bold;
                BibListApiLabel.Background = e.BibsApiLoaded ? Brushes.LightGreen : Brushes.LightCoral;
                InvalidApiLabel.Content = e.InvalidApiStatus;
                InvalidApiLabel.FontWeight = e.InvalidApiLoaded ? FontWeights.Normal : FontWeights.Bold;
                InvalidApiLabel.Background = e.InvalidApiLoaded ? Brushes.LightGreen : Brushes.LightCoral;
                RawSearchApiLabel.Content = e.RawSearchApiStatus;
                RawSearchApiLabel.FontWeight = e.RawSearchApiLoaded ? FontWeights.Normal : FontWeights.Bold;
                RawSearchApiLabel.Background = e.RawSearchApiLoaded ? Brushes.LightGreen : Brushes.LightCoral;
                LoadButton.IsEnabled = true;
                SaveButton.IsEnabled = e is { PointsApiLoaded: true, ManualApiLoaded: true };
            });
        }

        private async void Load(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadButton.IsEnabled = false;
                if (ApiLinkText.Text.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    ThemedDialog.Show("Nezabezpečené pripojenie",
                        "API link používa HTTP bez šifrovania, prístupový kľúč môže byť odchytený. Odporúčame použiť HTTPS.",
                        ThemedDialogIcon.Warning);
                }
                await App.RaceResult.LoadApi(ApiLinkText.Text);
            }
            catch (Exception ex)
            {
                ThemedDialog.Show("Chyba", $"Nastala chyba pri načítavaní API: {ex.Message}", ThemedDialogIcon.Error);
                // Let the user correct the link and retry.
                LoadButton.IsEnabled = true;
            }
        }

        private async void Save(object sender, RoutedEventArgs e)
        {
            try
            {
                await App.RaceResult.Start();
                App.RaceResult.RaceResultApiLoaded -= OnRaceResultApiLoaded;
                Close();
            }
            catch (Exception ex)
            {
                ThemedDialog.Show("Chyba", $"Nastala chyba pri spustení API služby: {ex.Message}", ThemedDialogIcon.Error);
            }
        }
    }
}
