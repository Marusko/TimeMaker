namespace TimeMaker.Models
{
    public class SerialSourceInitModel : SourceInitModel
    {
        public override string Name { get; set; } = string.Empty;
        public override string Source { get; set; } = string.Empty;
        public override string FirstTarget { get; set; } = string.Empty;
        public string SecondTarget { get; set; } = string.Empty;
        public string ThirdTarget { get; set; } = string.Empty;
        public TimyMode Mode { get; set; } = TimyMode.Stopwatch;
    }
}
