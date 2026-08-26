namespace TimeMaker.ViewModels
{
    /// <summary>One generated time as shown in the definition preview.</summary>
    public class TimePreviewItemViewModel
    {
        public string Part { get; init; } = string.Empty;
        public string Bib { get; init; } = string.Empty;
        public string Time { get; init; } = string.Empty;
        public bool IsDuplicate { get; init; }
        public string Note => IsDuplicate ? "duplicita" : string.Empty;
    }
}
