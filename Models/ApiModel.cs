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
}
