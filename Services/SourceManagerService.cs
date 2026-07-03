using System.Collections.Concurrent;
using TimeMaker.Models;
using TimeMaker.Windows;

namespace TimeMaker.Services
{
    public class SourceManagerService : IDisposable
    {
        public ConcurrentDictionary<string, SourceService> Sources { get; private set; } = new();
        private ConcurrentDictionary<string, List<StatusWindow>> _statusWindows = new();
        public event EventHandler<SourceEventArgs>? SourceAdded;
        public event EventHandler<SourceEventArgs>? SourceRemoved;

        public SourceService? GetSource(string key)
        {
            return Sources.GetValueOrDefault(key);
        }

        public bool AddSource(Type sourceType, SourceInitModel initModel)
        {
            SourceService? source =
                sourceType == typeof(FileService) ? new FileService()
                : sourceType == typeof(SerialPortService) ? new SerialPortService()
                : null;
            if (source == null)
            {
                return false;
            }

            source.Init(initModel);
            if (!Sources.TryAdd(source.Id, source))
            {
                return false;
            }

            // Raise the event only after the source is retrievable via GetSource.
            App.Logger.Log($"[SM] {source.InternalType.Name} added: {source.Id}");
            SourceAdded?.Invoke(this, new SourceEventArgs { SourceService = source });
            return true;
        }

        public bool RemoveSource(string id)
        {
            var res = Sources.TryRemove(id, out var source);
            if (res)
            {
                App.Logger.Log($"[SM] Source removed: {source?.Id}");
                SourceRemoved?.Invoke(this, new SourceEventArgs { SourceService = source });
                RemoveAllSourceWindows(source?.Id ?? "");
                source?.Stop();
            }

            return res;
        }

        private void StopAllSources()
        {
            App.Logger.Log("[SM] Stopping all sources");
            foreach (var source in Sources)
            {
                source.Value.Stop();
            }
            Sources.Clear();
        }

        public bool AddWindow(string id, StatusWindow window)
        {
            App.Logger.Log($"[SM] Adding window for source: {id}");
            if (!_statusWindows.TryGetValue(id, out List<StatusWindow>? value))
            {
                value = new List<StatusWindow>();
                _statusWindows[id] = value;
            }

            value.Add(window);
            return true;
        }

        private void RemoveAllSourceWindows(string id)
        {
            App.Logger.Log($"[SM] Removing all windows for source: {id}");
            if (_statusWindows.TryRemove(id, out List<StatusWindow>? value))
            {
                foreach (var window in value)
                {
                    window.Close();
                }
                value.Clear();
            }
        }

        public bool RemoveWindow(string id, StatusWindow window)
        {
            App.Logger.Log($"[SM] Removing window for source: {id}");
            if (_statusWindows.TryGetValue(id, out List<StatusWindow>? value))
            {
                value.Remove(window);
                return true;
            }
            return false;
        }

        private void CloseAllStatusWindows()
        {
            App.Logger.Log("[SM] Closing all status windows");
            foreach (var key in _statusWindows.Keys)
            {
                RemoveAllSourceWindows(key);
            }
            _statusWindows.Clear();
        }

        public void Dispose()
        {
            App.Logger.Log("[SM] Stopping and disposing resources");
            CloseAllStatusWindows();
            StopAllSources();
        }
    }
}
