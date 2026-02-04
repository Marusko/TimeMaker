using TimeMaker.Services;

namespace TimeMaker.Models
{
    public class RaceResultApiLoadedEventArgs : EventArgs
    {
        public string PointsApiStatus { get; set; } = "Nenájdené";
        public string ManualApiStatus { get; set; } = "Nenájdené";
        public string BibsApiStatus { get; set; } = "Nenájdené";
        public string InvalidApiStatus { get; set; } = "Nenájdené";
        public string RawSearchApiStatus { get; set; } = "Nenájdené";
    }

    public class RaceResultBibsLoadedEventArgs : EventArgs
    {
        public List<string> Bibs { get; init; } = new();
    }

    public class RaceResultTimeSentEventArgs : EventArgs
    {
        public string Id { get; init; } = string.Empty;
        public string SourceId { get; init; } = string.Empty;
        public UploadStatus Status { get; init; } = UploadStatus.Pending;
        public string StatusCode { get; init; } = string.Empty;
    }

    public class SourceEventArgs : EventArgs
    {
        public SourceService? SourceService { get; init; }
    }
}
