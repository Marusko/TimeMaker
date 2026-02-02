using System.Collections.Concurrent;
using TimeMaker.Models;
using TimeMaker.ViewModels;

namespace TimeMaker.Services
{
    public class SerialPortService : SourceService
    {
        public override string Id { get; protected set; } = Guid.NewGuid().ToString();
        public override Type InternalType { get; protected set; } = typeof(SerialPortService);
        public override string Name { get; protected set; } = string.Empty;
        public override string Type { get; protected set; } = string.Empty;
        public override string Source { get; protected set; } = string.Empty;
        public override string Target { get; protected set; } = string.Empty;
        public override int SentOk { get; protected set; }
        public override int SentError { get; protected set; }
        public override ConcurrentQueue<DataModel> DataQueue { get; protected set; } = new();
        public override SourceItemViewModel SourceItemViewModel { get; protected set; } = new();

        public override void Init(SourceInitModel initModel)
        {
            throw new NotImplementedException();
        }

        public override void Start()
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
