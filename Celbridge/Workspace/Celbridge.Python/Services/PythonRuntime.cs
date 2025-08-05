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

    public static async Task<Result<string>> EnsurePythonInstalledAsync()
    {
        try
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

            return Result<string>.Ok(pythonFolder.Path);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail("Failed to install Python")
                .WithException(ex);
        }
    }

    /// <summary>
    /// Runs a Python script using the embedded runtime.
    /// </summary>
    public static async Task<Result<string>> RunScriptAsync(string scriptFile, string workingDir)
    {
        try
        {
            var ensureResult = await EnsurePythonInstalledAsync();
            if (ensureResult.IsFailure)
            {
                return Result<string>.Fail($"Failed to ensure Python installation")
                    .WithErrors(ensureResult);
            }

            var pythonFolder = ensureResult.Value;

            var pythonPath = Path.Combine(pythonFolder, PythonExeName);
            var arguments = string.IsNullOrEmpty(scriptFile) ? string.Empty : $"\"{scriptFile}\"";

            var psi = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = arguments,
                WorkingDirectory = workingDir,
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

            var outputText = !string.IsNullOrWhiteSpace(errors) ? $"ERROR:\n{errors}" : output;

            return Result<string>.Ok(outputText);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Failed to run Python script: '{scriptFile}'")
                .WithException(ex);
        }
    }

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
}
