using Newtonsoft.Json;
namespace TimeMaker.Models
{
    public class ApiModel
    {
        public bool? Disabled { get; set; } = false;
        public string? Key { get; set; } = null;
        public string? Label { get; set; } = null;
    }

    public class ApiTimingPoint
    {
        public string Name { get; set; } = "";
    }

    public class TimingResult
    {
        [JsonProperty("ID")]
        public int Id { get; set; }

        [JsonProperty("TimingPoint")]
        public string TimingPoint { get; set; } = string.Empty;

        [JsonProperty("Time")]
        public string Time { get; set; } = string.Empty;

        [JsonProperty("Bib")] 
        public string Bib { get; set; } = string.Empty;
    }
}
