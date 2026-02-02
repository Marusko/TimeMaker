using System.Collections.Concurrent;
using TimeMaker.Models;
using TimeMaker.ViewModels;

namespace TimeMaker.Services
{
    public class FileService : SourceService
    {
        public override string Id { get; protected set; } = Guid.NewGuid().ToString();
        public override Type InternalType { get; protected set; } = typeof(FileService);
        public override string Name { get; protected set; } = string.Empty;
        public override string Type { get; protected set; } = string.Empty;
        public override string Source { get; protected set; } = string.Empty;
        public override string Target { get; protected set; } = string.Empty;
        public override int SentOk { get; protected set; }
        public override int SentError { get; protected set; }
        public override bool Running { get; protected set; }
        public override ConcurrentQueue<DataModel> DataQueue { get; protected set; } = new();
        public override ConcurrentQueue<DataModel> SentData { get; protected set; } = new();
        public override SourceItemViewModel SourceItemViewModel { get; protected set; } = new();

        public override void Init(SourceInitModel initModel)
        {
            if (initModel is FileSourceInitModel model)
            {
                SourceItemViewModel = new SourceItemViewModel()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = model.Name,
                    Type = "CSV",
                    Source = model.Source,
                    Target = model.FirstTarget.Name,
                    Status = "Pripravené",
                    IsRunning = false
                };
                Id = SourceItemViewModel.Id;
                Name = model.Name;
                Type = "CSV";
                Source = model.Source;
                Target = model.FirstTarget.Name;
            }
        }

        public override void Start()
        {
            throw new NotImplementedException();
        }

        public override List<DataModel> GetAllData()
        {
            throw new NotImplementedException();
        }

        public override List<DataModel> GetUnsentData()
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
