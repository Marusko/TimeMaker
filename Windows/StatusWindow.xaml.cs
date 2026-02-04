using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
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
        private ScrollViewer? _scrollViewer;
        private ICollectionView? _itemsView;
        public StatusWindow(SourceService sourceService)
        {
            InitializeComponent();
            _sourceService = sourceService;
            _sourceService.LogData.CollectionChanged += Items_CollectionChanged;
            foreach (var item in _sourceService.LogData)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }

            _itemsView = CollectionViewSource.GetDefaultView(_sourceService.LogData);
            _itemsView.Filter = FilterItems;
            ListViewData.ItemsSource = _itemsView;
            ListViewData.Loaded += ListLoaded;
            ComboBox.SelectionChanged += SelectedFilterChanged;
            ComboBox.SelectedIndex = 0;
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
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (IsNearBottom())
                    {
                        ListViewData.ScrollIntoView(_sourceService.LogData.Last());
                    }
                }), DispatcherPriority.Background);
            }

            if (e.NewItems != null)
            {
                foreach (DataLogViewModel item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (DataLogViewModel item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DataLogViewModel.Status))
            {
                _itemsView?.Refresh();
            }
        }

        private void ListLoaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = FindVisualChild<ScrollViewer>(ListViewData);
        }

        private void SelectedFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            _itemsView?.Refresh();
        }

        private bool FilterItems(object obj)
        {
            var opt = ComboBox.SelectedIndex;
            if (opt == 0)
            {
                return true;
            }

            if (obj is DataLogViewModel dataVm)
            {
                return ComboBox.SelectedIndex switch
                {
                    1 => dataVm.Status == UploadStatus.Pending,
                    2 => dataVm.Status == UploadStatus.Completed,
                    3 => dataVm.Status == UploadStatus.Ignored,
                    4 => dataVm.IsClear,
                    5 => dataVm.Status == UploadStatus.Failed,
                    _ => true,
                };
            }
            return false;
        }

        private bool IsNearBottom()
        {
            if (_scrollViewer == null)
                return true;

            // Total scrollable content height
            double extentHeight = _scrollViewer.ExtentHeight;

            // Bottom of currently visible area
            double visibleBottom = _scrollViewer.VerticalOffset + _scrollViewer.ViewportHeight;

            // Distance from bottom
            double distanceFromBottom = extentHeight - visibleBottom;

            return distanceFromBottom < 150;
        }

        private T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T typedChild)
                    return typedChild;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        private void StatusWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            _sourceService.LogData.CollectionChanged -= Items_CollectionChanged;
            foreach (var item in _sourceService.LogData)
            {
                item.PropertyChanged -= Item_PropertyChanged;
            }
            ListViewData.Loaded -= ListLoaded;
            ComboBox.SelectionChanged -= SelectedFilterChanged;
            App.SourceManager.RemoveWindow(_sourceService.Id, this);
            _scrollViewer = null;
            _itemsView = null;
        }
    }
}
