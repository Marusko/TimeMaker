namespace TimeMaker.Models
{
    public class SerialSourceInitModel : SourceInitModel
    {
        public override string Name { get; set; } = string.Empty;
        public override string Source { get; set; } = string.Empty;
        public override ApiTimingPoint FirstTarget { get; set; } = new();
        public ApiTimingPoint SecondTarget { get; set; } = new();
        public ApiTimingPoint ThirdTarget { get; set; } = new();
        public TimyMode Mode { get; set; } = TimyMode.Stopwatch;
    }
}
