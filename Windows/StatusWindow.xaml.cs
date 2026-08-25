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
            Title = _sourceService.Name;
        }

        private static DataLogViewModel? GetContextMenuItem(object sender)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var lvi = contextMenu?.PlacementTarget as ListViewItem;
            return lvi?.Content as DataLogViewModel;
        }

        private void Upload(object sender, RoutedEventArgs e)
        {
            if (GetContextMenuItem(sender) is { } item)
            {
                item.Status = UploadStatus.Pending;
                App.RaceResult.AddManual(item.ToDataModel(_sourceService.Id));
            }
        }

        private void ShowError(object sender, RoutedEventArgs e)
        {
            if (GetContextMenuItem(sender) is { } item)
            {
                ThemedDialog.Show("Chyba", $"Chyba nahrávania času. Chyba: [{item.StatusCode}]", ThemedDialogIcon.Error);
            }
        }

        private void CopyNumber(object sender, RoutedEventArgs e)
        {
            if (GetContextMenuItem(sender) is { } item)
            {
                CopyToClipboard(item.GetBibToCopy(), "číslo");
            }
        }

        private void CopyTime(object sender, RoutedEventArgs e)
        {
            if (GetContextMenuItem(sender) is { } item)
            {
                CopyToClipboard(item.GetTimeToCopy(), "čas");
            }
        }

        private static void CopyToClipboard(string value, string what)
        {
            if (string.IsNullOrEmpty(value))
            {
                // Nothing parsed and nothing usable in the raw impulse.
                ThemedDialog.Show("Kopírovanie", $"V impulze sa nepodarilo nájsť {what}.", ThemedDialogIcon.Warning);
                return;
            }

            try
            {
                Clipboard.SetText(value);
            }
            catch (Exception ex)
            {
                App.Logger.LogError("[SW] Error copying to clipboard", ex);
                ThemedDialog.Show("Chyba", $"Nepodarilo sa skopírovať do schránky: {ex.Message}", ThemedDialogIcon.Error);
            }
        }

        private void ShowChanges(object sender, RoutedEventArgs e)
        {
            if (GetContextMenuItem(sender) is { } item)
            {
                var changes = string.Join("\n", item.BibChanges);
                ThemedDialog.Show("Zmeny čísla", changes, ThemedDialogIcon.Info);
            }
        }

        private void EditBib(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem { DataContext: DataLogViewModel { RetryVisibility: Visibility.Visible } itemVm })
            {
                var window = new EditBibWindow(itemVm.Bib);
                window.Owner = this;
                window.ShowDialog();

                var newBib = window.GetBib();
                if (string.IsNullOrEmpty(newBib) || newBib == itemVm.Bib)
                {
                    // Closed without an actual change - do not re-upload.
                    return;
                }

                itemVm.BibChanges.Add($"{itemVm.Bib} -> {newBib}");
                itemVm.Bib = newBib;
                itemVm.Status = UploadStatus.Pending;
                App.RaceResult.AddManual(itemVm.ToDataModel(_sourceService.Id));
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
                    ThemedDialog.Show("CSV súbor", "Je potrebné vybrať miesto pre uloženie", ThemedDialogIcon.Warning);
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
                ThemedDialog.Show("Chyba", $"Nastala chyba pri ukladaní súboru: {ex.Message}", ThemedDialogIcon.Error);
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
                    ThemedDialog.Show("Textový súbor", "Je potrebné vybrať miesto pre uloženie", ThemedDialogIcon.Warning);
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
                ThemedDialog.Show("Chyba", $"Nastala chyba pri ukladaní súboru: {ex.Message}", ThemedDialogIcon.Error);
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
