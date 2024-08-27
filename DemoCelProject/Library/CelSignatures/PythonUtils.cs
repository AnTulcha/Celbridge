namespace Celbridge.Models.CelSignatures;

public record PythonUtils
{
    public record GenerateImage : ICelSignature
    {
        public StringExpression prompt { get; set; } = new ();
        public StringExpression imageResource { get; set; } = new ();
        public string ReturnType => "";
        public string GetSummary(PropertyContext context) => $"({prompt.GetSummary(context)}, {imageResource.GetSummary(context)})";
    }

    public record TestGenerateImage : ICelSignature
    {
        public string ReturnType => "";
        public string GetSummary(PropertyContext context) => $"()";
    }
}