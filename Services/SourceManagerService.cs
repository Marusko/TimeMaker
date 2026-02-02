using System.Collections.Concurrent;
using TimeMaker.Models;
using TimeMaker.Windows;

namespace TimeMaker.Services
{
    public class SourceManagerService : IDisposable
    {
        public ConcurrentDictionary<string, SourceService> Sources { get; private set; } = new();
        private ConcurrentDictionary<string, List<StatusWindow>> _statusWindows = new();

        public SourceService? GetSource(string key)
        {
            return Sources.GetValueOrDefault(key);
        }

        public bool AddSource(Type sourceType, SourceInitModel initModel)
        {
            if (sourceType == typeof(FileService))
            {
                var fileService = new FileService();
                fileService.Init(initModel);
                return Sources.TryAdd(fileService.Id, fileService);
            }
            else if (sourceType == typeof(SerialPortService))
            {
                var serialPortService = new SerialPortService();
                serialPortService.Init(initModel);
                return Sources.TryAdd(serialPortService.Id, serialPortService);
            }

            return false;
        }

        public bool RemoveSource(string id)
        {
            var res = Sources.TryRemove(id, out var source);
            if (res)
            {
                source?.Stop();
            }

            return res;
        }

        private void StopAllSources()
        {
            foreach (var source in Sources)
            {
                source.Value.Stop();
            }
            Sources.Clear();
        }

        public bool AddWindow(string id, StatusWindow window)
        {
            if (!_statusWindows.TryGetValue(id, out List<StatusWindow>? value))
            {
                value = new List<StatusWindow>();
                _statusWindows[id] = value;
            }

            value.Add(window);
            return true;
        }

        public bool RemoveWindow(string id, StatusWindow window)
        {
            if (_statusWindows.TryGetValue(id, out List<StatusWindow>? value))
            {
                value.Remove(window);
                return true;
            }
            return false;
        }

        private void CloseAllStatusWindows()
        {
            foreach (var sw in _statusWindows)
            {
                foreach (var window in sw.Value)
                {
                    window.Close();
                }
                sw.Value.Clear();
            }
            _statusWindows.Clear();
        }

        public void Dispose()
        {
            StopAllSources();
            CloseAllStatusWindows();
        }
    }
}
