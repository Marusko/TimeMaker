namespace TimeMaker.Models
{
    public class FileSourceInitModel : SourceInitModel
    {
        public override string Name { get; set; } = string.Empty;
        public override string Source { get; set; } = string.Empty;
        public override ApiTimingPoint FirstTarget { get; set; } = new();
        public char Separator { get; set; } = ';';
        public bool Template { get; set; } = false;
    }
}
