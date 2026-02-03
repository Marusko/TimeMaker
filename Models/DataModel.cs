namespace TimeMaker.Models
{
    public class DataModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SourceId { get; set; } = string.Empty;
        public string Bib { get; set; } = string.Empty;
        public TimeOnly Time { get; set; }
        public ApiTimingPoint TimingPoint { get; set; } = new();
        public string RawData { get; set; } = string.Empty;
        public bool IsClear { get; set; }
        public bool CompareBib(DataModel d)
        {
            return (!string.IsNullOrEmpty(Bib) && !string.IsNullOrEmpty(d.Bib) && Bib.Equals(d.Bib)) &&
                   (Time.ToString("hhmmsstt").Equals(d.Time.ToString("hhmmsstt")));
        }
        public bool Compare(DataModel d)
        {
            return !string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(d.Id) && Id.Equals(d.Id);
        }
    }
}
