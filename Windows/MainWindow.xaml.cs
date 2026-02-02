using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using TimeMaker.Models;
using TimeMaker.ViewModels;

namespace TimeMaker.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private ObservableCollection<SourceItemViewModel> _sources = new();
        public MainWindow()
        {
            InitializeComponent();
            ListViewSources.ItemsSource = _sources;
            App.RaceResult.RaceResultApiLoaded += OnApiLoaded;
            App.SourceManager.SourceAdded += OnSourceAdded;
            App.SourceManager.SourceRemoved += OnSourceRemoved;
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
            if (link?.DataContext is SourceItemViewModel sourceVm)
            {
                App.SourceManager.GetSource(sourceVm.Id)?.Start();
            }
        }

        private void StopSource(object sender, RoutedEventArgs e)
        {
            var link = sender as Hyperlink;
            if (link?.DataContext is SourceItemViewModel sourceVm)
            {
                App.SourceManager.GetSource(sourceVm.Id)?.Stop();
            }
        }

        private void ViewRawSource(object sender, RoutedEventArgs e)
        {
            var link = sender as Hyperlink;
            if (link?.DataContext is SourceItemViewModel sourceVm)
            {
                var statusWindow = new StatusWindow();
                //statusWindow.SetSourceService(source);
                statusWindow.Show();
                //App.SourceManager.AddWindow(source.Id, statusWindow);
            }
        }

        private void RemoveSource(object sender, RoutedEventArgs e)
        {
            var link = sender as Hyperlink;
            if (link?.DataContext is SourceItemViewModel sourceVm)
            {
                App.SourceManager.RemoveSource(sourceVm.Id);
            }
        }

        private void OnApiLoaded(object? sender, RaceResultApiLoadedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                BtnAddFile.IsEnabled = true;
                BtnAddTimy.IsEnabled = true;
                BtnRaceSettings.IsEnabled = false;
            });
        }

        private void OnSourceRemoved(object? sender, SourceEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var source = e.SourceService;
                if (source != null)
                {
                    _sources.Remove(source.SourceItemViewModel);
                }
            });
        }

        private void OnSourceAdded(object? sender, SourceEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var source = e.SourceService;
                if (source != null)
                {
                    _sources.Add(source.SourceItemViewModel);
                }
            });
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
            App.SourceManager.SourceAdded -= OnSourceAdded;
            App.SourceManager.SourceRemoved -= OnSourceRemoved;
            _sources.Clear();
        }
    }
}