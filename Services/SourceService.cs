using System.Collections.Concurrent;
using TimeMaker.Models;

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
    }
}
