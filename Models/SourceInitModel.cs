namespace TimeMaker.Models
{
    public abstract class SourceInitModel
    {
        public abstract string Name { get; set; }
        public abstract string Source { get; set; }
        public abstract ApiTimingPoint FirstTarget { get; set; }
    }
}
