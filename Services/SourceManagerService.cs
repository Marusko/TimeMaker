using System.Collections.Concurrent;

namespace TimeMaker.Services
{
    public class SourceManagerService : IDisposable
    {
        public ConcurrentDictionary<string, SourceService> Sources { get; private set; } = new();
        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}
