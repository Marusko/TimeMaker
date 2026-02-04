using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using TimeMaker.Models;
using TimeMaker.Services;
using TimeMaker.ViewModels;

namespace TimeMaker.Windows
{
    /// <summary>
    /// Interaction logic for StatusWindow.xaml
    /// </summary>
    public partial class StatusWindow
    {
        private SourceService _sourceService;
        public StatusWindow(SourceService sourceService)
        {
            InitializeComponent();
            _sourceService = sourceService;
            _sourceService.LogData.CollectionChanged += Items_CollectionChanged;
            ListViewData.ItemsSource = _sourceService.LogData;
            Title = _sourceService.Source;
        }

        private void Upload(object sender, RoutedEventArgs e)
        {
            var link = sender as Hyperlink;
            if (link?.DataContext is DataLogViewModel dataVm)
            {
                var data = new DataModel()
                {
                    Id = dataVm.Id,
                    SourceId = _sourceService.Id,
                    Bib = dataVm.Bib,
                    Time = dataVm.Time,
                    TimingPoint = dataVm.TimingPoint,
                    RawData = dataVm.Raw,
                    IsClear = dataVm.IsClear
                };
                dataVm.Status = UploadStatus.Pending;
                App.RaceResult.AddManual(data);
            }
        }

        private void ShowError(object sender, RoutedEventArgs e)
        {
            var link = sender as Hyperlink;
            if (link?.DataContext is DataLogViewModel dataVm)
            {
                MessageBox.Show($"Chyba nahrávania času. Chyba: [{dataVm.StatusCode}]", "Chyba", MessageBoxButton.OK, MessageBoxImage.None);
            }
        }

        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                ListViewData.ScrollIntoView(ListViewData.Items[0] ?? throw new InvalidOperationException());
            }
        }

        private void StatusWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            _sourceService.LogData.CollectionChanged -= Items_CollectionChanged;
            App.SourceManager.RemoveWindow(_sourceService.Id, this);
        }
    }
}
