using Microsoft.Win32;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var lvi = contextMenu?.PlacementTarget as ListViewItem;
            if (lvi?.Content is DataLogViewModel item)
            {
                var data = new DataModel()
                {
                    Id = item.Id,
                    SourceId = _sourceService.Id,
                    Bib = item.Bib,
                    Time = item.Time,
                    TimingPoint = item.TimingPoint,
                    RawData = item.Raw,
                    IsClear = item.IsClear
                };
                item.Status = UploadStatus.Pending;
                App.RaceResult.AddManual(data);
            }
        }

        private void ShowError(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var lvi = contextMenu?.PlacementTarget as ListViewItem;
            if (lvi?.Content is DataLogViewModel item)
            {
                MessageBox.Show($"Chyba nahrávania času. Chyba: [{item.StatusCode}]", "Chyba", MessageBoxButton.OK, MessageBoxImage.None);
            }
        }

        private void CopyNumber(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var lvi = contextMenu?.PlacementTarget as ListViewItem;
            if (lvi?.Content is DataLogViewModel item)
            {
                Clipboard.SetText(item.Bib);
            }
        }

        private void CopyTime(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var lvi = contextMenu?.PlacementTarget as ListViewItem;
            if (lvi?.Content is DataLogViewModel item)
            {
                Clipboard.SetText(item.Time.ToString("HH:mm:ss.ffff"));
            }
        }

        private void ShowChanges(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var lvi = contextMenu?.PlacementTarget as ListViewItem;
            if (lvi?.Content is DataLogViewModel item)
            {
                var changes = string.Join("\n", item.BibChanges);
                MessageBox.Show($"Zmeny čísla: \n{changes}", "Zmeny čísla", MessageBoxButton.OK, MessageBoxImage.None);
            }
        }

        private void EditBib(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem { DataContext: DataLogViewModel { RetryVisibility: Visibility.Visible } itemVm })
            {
                var window = new EditBibWindow(itemVm.Bib);
                window.Owner = this;
                window.ShowDialog();
                itemVm.BibChanges.Add($"{itemVm.Bib} -> {window.GetBib()}");
                itemVm.Bib = window.GetBib();

                var data = new DataModel()
                {
                    Id = itemVm.Id,
                    SourceId = _sourceService.Id,
                    Bib = itemVm.Bib,
                    Time = itemVm.Time,
                    TimingPoint = itemVm.TimingPoint,
                    RawData = itemVm.Raw,
                    IsClear = itemVm.IsClear
                };
                itemVm.Status = UploadStatus.Pending;
                App.RaceResult.AddManual(data);
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

        private async void ExportAll(object sender, RoutedEventArgs e)
        {
            var window = new ExportSeparatorWindow();
            window.Owner = this;
            var progressWindow = new ExportProgressWindow();
            progressWindow.Owner = this;
            try
            {
                window.ShowDialog();
                var op = new SaveFileDialog();
                op.Title = "Vyberte miesto na uloženie";
                op.Filter = "CSV súbor|*.csv";
                op.FileName = $"{_sourceService.Name}_export.csv";
                var res = op.ShowDialog();
                if (res == null || string.IsNullOrEmpty(op.FileName))
                {
                    MessageBox.Show("Je potrebné vybrať miesto pre uloženie", "CSV súbor", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var progress = new Progress<int>(value =>
                {
                    progressWindow.Report(value);
                });
                progressWindow.Show();
                await ExportService.ExportAllAsync(op.FileName, _sourceService.Id, window.GetDelimiter(), progress);
                progressWindow.Close();
                NotificationService.ShowInfoNotification("Export dokončený", $"Data exportované do súboru {op.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nastala chyba pri ukladaní súboru: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                App.Logger.LogError("[SW] Error saving CSV file", ex);
                progressWindow.Close();
            }
        }

        private async void ExportImpulses(object sender, RoutedEventArgs e)
        {
            var progressWindow = new ExportProgressWindow();
            progressWindow.Owner = this;
            try
            {
                var op = new SaveFileDialog();
                op.Title = "Vyberte miesto na uloženie";
                op.Filter = "Textový súbor|*.txt";
                op.FileName = $"{_sourceService.Name}_RAW_export.txt";
                var res = op.ShowDialog();
                if (res == null || string.IsNullOrEmpty(op.FileName))
                {
                    MessageBox.Show("Je potrebné vybrať miesto pre uloženie", "Textový súbor", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var progress = new Progress<int>(value =>
                {
                    progressWindow.Report(value);
                });
                progressWindow.Show();
                await ExportService.ExportImpulsesOnlyAsync(op.FileName, _sourceService.Id, progress);
                progressWindow.Close();
                NotificationService.ShowInfoNotification("Export dokončený", $"Data exportované do súboru {op.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nastala chyba pri ukladaní súboru: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                App.Logger.LogError("[SW] Error saving TXT file", ex);
                progressWindow.Close();
            }
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
