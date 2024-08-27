using System.Threading.Tasks;
using _env = CelRuntime.Environment;

namespace CelApplication;

public static class Project
{
    public static async Task<string> MakeASpeech(string topic)
    {
        _env.Chat.StartChat("You are the kings speech composer. Compose a short emotional speech in 20 words. Do not use quotation marks in the response.");
        string speech = await _env.Chat.Ask($"Write a speech about: {topic}");
        _env.Chat.EndChat();
        return speech;
    }
    
    public static async Task MakeAPicture(string prompt)
    {
        await PythonUtils.GenerateImage(prompt: prompt, imageResource: "@Example.png");
    }
    
    public static async Task Start()
    {
        string speech = await Project.MakeASpeech(topic: "Playing Five nights at freddies game");
        _env.Print(speech);
        await _env.Chat.TextToSpeech(speech);
        await Project.MakeAPicture(prompt: speech);
        _env.Print("ok:Done!");
    }
}
