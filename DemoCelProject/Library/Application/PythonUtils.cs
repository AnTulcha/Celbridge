using System.Threading.Tasks;
using _env = CelRuntime.Environment;

namespace CelApplication;

public static class PythonUtils
{
    public static async Task GenerateImage(string prompt, string imageResource)
    {
        string result = await _env.Process.StartProcess("Library/Python/RunPython.ps1",$"Library/Python/generate_image.py \"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\" \"{prompt}\" \"{imageResource}\"");
        _env.Print($"ok:{result}");
    }
    
    public static async Task TestGenerateImage()
    {
        await PythonUtils.GenerateImage(prompt: "A duck smoking a cigar", imageResource: "@Test/Test.png");
    }
}
