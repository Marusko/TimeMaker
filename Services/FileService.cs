using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
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
        public bool Template { get; protected set; }
        public override ConcurrentDictionary<string, DataModel> DataDictionary { get; protected set; } = new();
        public override ObservableCollection<DataLogViewModel> LogData { get; protected set; } = new();
        protected override ConcurrentDictionary<string, DataLogViewModel> LogDataLookup { get; set; } = new();
        public override SourceItemViewModel SourceItemViewModel { get; protected set; } = new();
        private char _separator = ';';
        private HashSet<string> _defined = new();
        private HashSet<string> _undefined = new();
        private int _count;

        public override void Init(SourceInitModel initModel)
        {
            if (initModel is FileSourceInitModel model)
            {
                SourceItemViewModel = new SourceItemViewModel()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = model.Name,
                    Type = "CSV" + (model.Template ? " šablóna" : ""),
                    Source = model.Source,
                    Target = model.FirstTarget.Name,
                    Status = "Pripravené",
                    IsRunning = false
                };
                Id = SourceItemViewModel.Id;
                Name = model.Name;
                Type = "CSV" + (model.Template ? " šablóna" : "");
                Source = model.Source;
                Target = model.FirstTarget.Name;
                Template = model.Template;
                _separator = model.Separator;
                App.Logger.Log($"[FS] Initialized FileSource: {Id}");
            }
        }

        private void OnRaceResultTimeSent(object? sender, RaceResultTimeSentEventArgs e)
        {
            if (e.SourceId.Equals(Id))
            {
                if (LogDataLookup.TryGetValue(e.Id, out var item))
                {
                    item.ChangeStatus(e.Status);
                }
                if (e.Status == UploadStatus.Completed)
                {
                    SentOk++;
                }
                else if (e.Status == UploadStatus.Failed)
                {
                    SentError++;
                }
                SourceItemViewModel.UpdateStatus($"OK: {SentOk}, Chyba: {SentError}, Všetky: {_count}");
                if (SentError + SentOk == _count)
                {
                    App.Logger.Log($"[FS] All data sent for FileSource: {Id}");
                    Stop();
                }
            }
        }

        private void OnRaceResultBibsLoaded(object? sender, RaceResultBibsLoadedEventArgs e)
        {
            foreach (var b in e.Bibs)
            {
                if (!_defined.Contains(b) && _undefined.Remove(b))
                {
                    _defined.Add(b);
                }
            }
            App.Logger.Log($"[FS] Bibs loaded for FileSource: {Id}");
        }

        public override void Start()
        {
            if (!Running)
            {
                App.Logger.Log($"[FS] Starting FileSource: {Id}");
                Running = true;
                SourceItemViewModel.IsRunning = true;
                SourceItemViewModel.UpdateStatus("Aktívne");
                SentError = 0;
                SentOk = 0;
                LogData.Clear();
                LogDataLookup.Clear();
                App.RaceResult.RaceResultBibsLoaded += OnRaceResultBibsLoaded;
                App.RaceResult.RaceResultTimeSent += OnRaceResultTimeSent;
                LoadData();
                App.Logger.Log($"[FS] Started FileSource: {Id} with {_count} entries");
            }
        }

        public override List<DataModel> GetUnsentData()
        {
            var unsent = new List<DataModel>(_defined.Count);

            if (Running)
            {
                if (Template)
                {
                    foreach (var key in _defined)
                    {
                        if (DataDictionary.TryRemove(key, out var value))
                        {
                            unsent.Add(value);
                        }
                    }

                    _defined.Clear();
                }
                else
                {
                    unsent.AddRange(DataDictionary.Values);
                    DataDictionary.Clear();
                }
            }
            App.Logger.Log($"[FS] Unsent data for FileSource: {Id}, {unsent.Count} items");
            return unsent;
        }

        public override void Stop()
        {
            if (Running)
            {
                App.Logger.Log($"[FS] Stopping FileSource: {Id}");
                Running = false;
                SourceItemViewModel.IsRunning = false;
                SourceItemViewModel.UpdateStatus($"Zastavené - OK: {SentOk}, Chyba: {SentError}");
                _defined.Clear();
                _undefined.Clear();
                DataDictionary.Clear();
                App.RaceResult.RaceResultBibsLoaded -= OnRaceResultBibsLoaded;
                App.RaceResult.RaceResultTimeSent -= OnRaceResultTimeSent;
                App.Logger.Log($"[FS] Stopped FileSource: {Id}");
            }
        }

        private void LoadData()
        {
            _count = 0;
            try
            {
                App.Logger.Log($"[FS] Loading data from: {Source}, {Id}");
                using StreamReader reader = new StreamReader(Source);
                while (reader.ReadLine() is { } line)
                {
                    _count++;
                    var split = line.Split(_separator);
                    var data = new DataModel()
                    {
                        Id = Guid.NewGuid().ToString(),
                        SourceId = Id,
                        Bib = split[0].Trim(),
                        Time = StringToDateTime(split[1].Trim()),
                        TimingPoint = new ApiTimingPoint() { Name = Target },
                        RawData = line.Trim()
                    };
                    var vm = new DataLogViewModel()
                    {
                        Id = data.Id,
                        Raw = data.RawData,
                        Bib = data.Bib,
                        Time = data.Time,
                        TimingPoint = data.TimingPoint,
                        Status = UploadStatus.Pending
                    };
                    if (_undefined.Add(data.Bib))
                    {
                        DataDictionary.AddOrUpdate(data.Bib, data, (_, _) => data);
                        LogData.Add(vm);
                        LogDataLookup.AddOrUpdate(data.Id, vm, (_, _) => vm);
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                App.Logger.LogError($"[FS] Error loading data for FileSource: {Id}", ex);
                throw new Exception(ex.Message);
            }
        }

        private TimeOnly StringToDateTime(string s)
        {
            if (TimeOnly.TryParse(s, out var parsedTime))
            {
                return parsedTime;
            }
            else
            {
                App.Logger.LogError($"[FS] Invalid time format for FileSource: {Id}, {Source}, {s}");
                throw new InvalidDataException("Nesprávny formát času");
            }
        }
    }
}
