namespace Celbridge.Models.CelSignatures;

public record Project
{
    public record MakeASpeech : ICelSignature
    {
        public StringExpression topic { get; set; } = new ();
        public string ReturnType => "String";
        public string GetSummary(PropertyContext context) => $"({topic.GetSummary(context)}) : String";
    }

    public record MakeAPicture : ICelSignature
    {
        public StringExpression prompt { get; set; } = new ();
        public string ReturnType => "";
        public string GetSummary(PropertyContext context) => $"({prompt.GetSummary(context)})";
    }
}