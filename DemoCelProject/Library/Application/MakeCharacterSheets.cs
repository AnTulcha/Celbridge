using System.Threading.Tasks;
using _env = CelRuntime.Environment;

namespace CelApplication;

public static class MakeCharacterSheets
{
    public static async Task CreateCharacterImage(string name, string description)
    {
        await PythonUtils.GenerateImage(prompt: description, imageResource: $"@Characters/{name}.png");
    }
    
    public static async Task CreateCharacterSlideshow(string sheetName)
    {
        _env.Sheet.ReadSheet(sheetName);
        double numRows = _env.Sheet.GetNumRows();
        double rowIndex = 1;
        while (rowIndex < numRows)
        {
            await AddCharacterSlide(rowIndex: rowIndex);
            rowIndex = rowIndex + 1;
        }
        await MakeSlideshow();
    }
    
    public static async Task<string> MakeCharacterStory(string prompt)
    {
        _env.Chat.StartChat("You are a whimsical dungeon master adept at creating detailed back stories for characters. Limit your response to 30 carefully chosen words. Provide a back story based on my short description.");
        string story = await _env.Chat.Ask(prompt);
        return story;
    }
    
    public static async Task AddCharacterSlide(double rowIndex)
    {
        string name = _env.Sheet.GetString(rowIndex, 0);
        string description = _env.Sheet.GetString(rowIndex, 1);
        string race = _env.Sheet.GetString(rowIndex, 2);
        string klass = _env.Sheet.GetString(rowIndex, 3);
        double con = _env.Sheet.GetNumber(rowIndex, 4);
        if (!(con >=1 && con <=20)) throw new System.ArgumentOutOfRangeException($"{name}'s CON must be between 1 and 20");
        double dex = _env.Sheet.GetNumber(rowIndex, 5);
        if (!(dex >=1 && dex <=20)) throw new System.ArgumentOutOfRangeException($"{name}'s DEX must be between 1 and 20");
        double wis = _env.Sheet.GetNumber(rowIndex, 6);
        if (!(wis >=1 && wis <=20)) throw new System.ArgumentOutOfRangeException($"{name}'s WIS must be between 1 and 20");
        // Something wicked this way comes!
        string backStory = await MakeCharacterStory(prompt: $"My name is {name}. I am a {race} {klass}. {description}");
        string nameTrimmed = name;
        nameTrimmed = name.Replace(" ", "").Trim();
        await _env.Chat.TextToSpeech($"Making slide for {name}");
        await MakeCharacterSheets.CreateCharacterImage(name: nameTrimmed, description: description);
        await AddSlide(name: name, backStory: backStory, imageName: nameTrimmed, con: con, dex: dex, wis: wis);
    }
    
    public static async Task Start()
    {
        await CreateCharacterSlideshow(sheetName: "CharacterData");
        _env.Print("ok:Done!");
    }
    
    public static async Task MakeSlideshow()
    {
        string markdownText = _env.Markdown.GetMarkdown();
        string inputFile = "@Characters/Characters.md";
        string outputFile = "@Characters/Characters.html";
        _env.TextFile.WriteText(inputFile, markdownText);
        await RunMarp(inputFile: inputFile, outputFile: outputFile);
        _env.Print("Created slideshow");
    }
    
    public static async Task RunMarp(string inputFile, string outputFile)
    {
        await _env.Process.StartProcess("@Library/Marp/marp.exe",$"\"{inputFile}\" -o \"{outputFile}\" --allow-local-files");
    }
    
    public static async Task AddSlide(string name, string backStory, string imageName, double con, double dex, double wis)
    {
        _env.Markdown.AddLine($"![bg right:40% 80%]({imageName}.png)");
        _env.Markdown.StartSection(name);
        _env.Markdown.AddLine(backStory);
        _env.Markdown.AddLine("| CON | DEX | WIS |");
        _env.Markdown.AddLine("|----------|----------|----------|");
        _env.Markdown.AddLine($"| {con} | {dex} | {wis}");
        _env.Markdown.EndSection();
        _env.Markdown.AddSeparator();
        await Task.CompletedTask;
    }
}
