using System.Collections.Concurrent;
using TimeMaker.Models;

namespace TimeMaker.Services
{
    public class SerialPortService : SourceService
    {
        public override string Id { get; set; } = Guid.NewGuid().ToString();
        public override Type InternalType { get; set; } = typeof(SerialPortService);
        public override string Name { get; set; } = string.Empty;
        public override string Type { get; set; } = string.Empty;
        public override string Source { get; set; } = string.Empty;
        public override string Target { get; set; } = string.Empty;
        public override ConcurrentQueue<DataModel> DataQueue { get; set; } = new();
    }
}
