namespace TimeMaker.Models
{
    public class RaceResultApiLoadedEventArgs : EventArgs
    {
        public string PointsApiStatus { get; set; } = "Nenájdené";
        public string ManualApiStatus { get; set; } = "Nenájdené";
        public string BibsApiStatus { get; set; } = "Nenájdené";
    }
}
