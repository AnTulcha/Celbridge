namespace Celbridge.Models.CelSignatures;

public record MakeCharacterSheets
{
    public record CreateCharacterImage : ICelSignature
    {
        public StringExpression name { get; set; } = new ();
        public StringExpression description { get; set; } = new ();
        public string ReturnType => "";
        public string GetSummary(PropertyContext context) => $"({name.GetSummary(context)}, {description.GetSummary(context)})";
    }

    public record CreateCharacterSlideshow : ICelSignature
    {
        public StringExpression sheetName { get; set; } = new ();
        public string ReturnType => "";
        public string GetSummary(PropertyContext context) => $"({sheetName.GetSummary(context)})";
    }

    public record MakeCharacterStory : ICelSignature
    {
        public StringExpression prompt { get; set; } = new ();
        public string ReturnType => "String";
        public string GetSummary(PropertyContext context) => $"({prompt.GetSummary(context)}) : String";
    }

    public record AddCharacterSlide : ICelSignature
    {
        public NumberExpression rowIndex { get; set; } = new ();
        public string ReturnType => "";
        public string GetSummary(PropertyContext context) => $"({rowIndex.GetSummary(context)})";
    }

    public record Start : ICelSignature
    {
        public string ReturnType => "";
        public string GetSummary(PropertyContext context) => $"()";
    }

    public record MakeSlideshow : ICelSignature
    {
        public string ReturnType => "";
        public string GetSummary(PropertyContext context) => $"()";
    }

    public record RunMarp : ICelSignature
    {
        public StringExpression inputFile { get; set; } = new ();
        public StringExpression outputFile { get; set; } = new ();
        public string ReturnType => "";
        public string GetSummary(PropertyContext context) => $"({inputFile.GetSummary(context)}, {outputFile.GetSummary(context)})";
    }

    public record AddSlide : ICelSignature
    {
        public StringExpression name { get; set; } = new ();
        public StringExpression backStory { get; set; } = new ();
        public StringExpression imageName { get; set; } = new ();
        public NumberExpression con { get; set; } = new ();
        public NumberExpression dex { get; set; } = new ();
        public NumberExpression wis { get; set; } = new ();
        public string ReturnType => "";
        public string GetSummary(PropertyContext context) => $"({name.GetSummary(context)}, {backStory.GetSummary(context)}, {imageName.GetSummary(context)}, {con.GetSummary(context)}, {dex.GetSummary(context)}, {wis.GetSummary(context)})";
    }
}