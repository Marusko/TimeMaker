using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using TimeMaker.Models;
using TimeMaker.ViewModels;

namespace TimeMaker.Services
{
    public abstract class SourceService
    {
        private static readonly string[] TimeFormats = { "HH:mm:ss.FFFF", "H:mm:ss.FFFF" };

        protected static bool TryParseTime(string s, out TimeOnly time)
        {
            return TimeOnly.TryParseExact(s, TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out time)
                   || TimeOnly.TryParse(s, out time);
        }

        public abstract string Id { get; protected set; }
        public abstract Type InternalType { get; protected set; }
        public abstract string Name { get; protected set; }
        public abstract string Type { get; protected set; }
        public abstract string Source { get; protected set; }
        public abstract string Target { get; protected set; }
        public abstract int SentOk { get; protected set; }
        public abstract int SentError { get; protected set; }
        public abstract bool Running { get; protected set; }
        public abstract ConcurrentDictionary<string, DataModel> DataDictionary { get; protected set; }
        public abstract ObservableCollection<DataLogViewModel> LogData { get; protected set; }
        public abstract ConcurrentDictionary<string, DataLogViewModel> LogDataLookup { get; protected set; }
        public abstract SourceItemViewModel SourceItemViewModel { get; protected set; }

        public abstract void Init(SourceInitModel initModel);

        public abstract void Start();

        public abstract List<DataModel> GetUnsentData();

        public abstract void ChangeTarget(TargetChangeModel target);

        public abstract void Stop();
    }
}
