using System.Diagnostics;
using System.IO.Compression;

using Path = System.IO.Path;

namespace Celbridge.Python.Services;

public static class PythonRuntime
{
    private const string PythonFolderName = "Python";
    private const string PythonZipAssetPath = "ms-appx:///Assets/EmbeddedPython/python-3.14.0rc1-embed-amd64.zip";
    private const string PthFileName = "python314._pth";
    private const string PythonExeName = "python.exe";

    /// <summary>
    /// Ensures the embedded Python runtime is installed and ready to use.
    /// </summary>
    public static async Task<string> EnsurePythonInstalledAsync()
    {
        var localFolder = ApplicationData.Current.LocalFolder;
        var pythonFolder = await localFolder.CreateFolderAsync(PythonFolderName, CreationCollisionOption.OpenIfExists);
        var pythonExePath = Path.Combine(pythonFolder.Path, PythonExeName);

        if (!File.Exists(pythonExePath))
        {
            var zipFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(PythonZipAssetPath));
            var tempFile = await zipFile.CopyAsync(ApplicationData.Current.TemporaryFolder, "python-embed.zip", NameCollisionOption.ReplaceExisting);

            ZipFile.ExtractToDirectory(tempFile.Path, pythonFolder.Path, overwriteFiles: true);

            await EnsurePthFileAsync(pythonFolder.Path);
        }

        return pythonFolder.Path;
    }

    /// <summary>
    /// Ensures the python311._pth file exists and contains valid entries.
    /// </summary>
    private static async Task EnsurePthFileAsync(string pythonPath)
    {
        var pthPath = Path.Combine(pythonPath, PthFileName);
        if (File.Exists(pthPath))
        {
            File.Delete(pthPath);
        }

        var contents = string.Join(Environment.NewLine, new[]
        {
            "python314.zip",
            ".",
            "import site"
        });

        await File.WriteAllTextAsync(pthPath, contents);
    }

    /// <summary>
    /// Runs a Python script (inline or path to script) using the embedded runtime.
    /// </summary>
    public static async Task<string> RunScriptAsync(string scriptText, string? workingDir = null)
    {
        var pythonRoot = await EnsurePythonInstalledAsync();
        var scriptFile = Path.Combine(pythonRoot, "startup.py");
        await File.WriteAllTextAsync(scriptFile, scriptText);

        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(pythonRoot, PythonExeName),
            Arguments = $"\"{scriptFile}\"",
            WorkingDirectory = workingDir ?? pythonRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = psi };
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        var output = outputBuilder.ToString();
        var errors = errorBuilder.ToString();

        return !string.IsNullOrWhiteSpace(errors) ? $"ERROR:\n{errors}" : output;
    }
}
