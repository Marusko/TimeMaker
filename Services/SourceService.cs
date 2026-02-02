using System.Collections.Concurrent;
using TimeMaker.Models;
using TimeMaker.ViewModels;

namespace TimeMaker.Services
{
    public abstract class SourceService
    {
        public abstract string Id { get; set; }
        public abstract Type InternalType { get; set; }
        public abstract string Name { get; set; }
        public abstract string Type { get; set; }
        public abstract string Source { get; set; }
        public abstract string Target { get; set; }
        public abstract ConcurrentQueue<DataModel> DataQueue { get; set; }
        public abstract SourceItemViewModel SourceItemViewModel { get; set; }

        public abstract void Init(SourceInitModel initModel);

        public abstract void Start();

        public abstract void Stop();
    }
}
