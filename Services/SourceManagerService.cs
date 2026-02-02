using System.Collections.Concurrent;
using TimeMaker.Models;

namespace TimeMaker.Services
{
    public class SourceManagerService : IDisposable
    {
        public ConcurrentDictionary<string, SourceService> Sources { get; private set; } = new();

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
            return Sources.TryRemove(id, out _);
        }

        public void Dispose()
        {
            foreach (var ss in Sources)
            {
                ss.Value.Stop();
            }
        }
    }
}
