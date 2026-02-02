using TimeMaker.Services;

namespace TimeMaker.Models
{
    public class RaceResultApiLoadedEventArgs : EventArgs
    {
        public string PointsApiStatus { get; set; } = "Nenájdené";
        public string ManualApiStatus { get; set; } = "Nenájdené";
        public string BibsApiStatus { get; set; } = "Nenájdené";
    }

    public class RaceResultBibsLoadedEventArgs : EventArgs
    {
        public List<string> Bibs { get; set; } = new();
    }

    public class RaceResultTimeSentEventArgs : EventArgs
    {
        public string Id { get; set; } = string.Empty;
        public UploadStatus Status { get; set; } = UploadStatus.Pending;
    }

    public class SourceEventArgs : EventArgs
    {
        public string SourceId { get; set; } = string.Empty;
        public SourceService? SourceService { get; set; }
        public Type Type { get; set; } = typeof(SourceService);
    }
}
