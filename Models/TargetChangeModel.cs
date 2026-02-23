namespace TimeMaker.Models
{
    public abstract class TargetChangeModel
    {
        public abstract string Target { get; set; }
    }

    public class FileTargetChangeModel : TargetChangeModel
    {
        public override string Target { get; set; } = string.Empty;
    }

    public class SerialTargetChangeModel : TargetChangeModel
    {
        public override string Target { get; set; } = string.Empty;
        public string SecondTarget { get; set; } = string.Empty;
        public string ThirdTarget { get; set; } = string.Empty;
    }
}
