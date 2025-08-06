using System.IO.Compression;

using Path = System.IO.Path;

namespace Celbridge.Python.Services;

public static class PythonInstaller
{
    private const string PythonFolderName = "Python";
    private const string PythonZipAssetPath = "ms-appx:///Assets/EmbeddedPython/python-3.14.0rc1-embed-amd64.zip";

    public static async Task<Result<string>> InstallPythonAsync()
    {
        try
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var pythonFolderPath = Path.Combine(localFolder.Path, PythonFolderName);

            // Uncomment to force install embedded Python
            // Directory.Delete(pythonFolderPath, true);

            if (!Directory.Exists(pythonFolderPath))
            {
                var pythonFolder = await localFolder.CreateFolderAsync(PythonFolderName, CreationCollisionOption.OpenIfExists);

                var zipFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(PythonZipAssetPath));
                var tempFile = await zipFile.CopyAsync(ApplicationData.Current.TemporaryFolder, "python-embed.zip", NameCollisionOption.ReplaceExisting);

                ZipFile.ExtractToDirectory(tempFile.Path, pythonFolder.Path, overwriteFiles: true);

                StorageFolder installedLocation = Package.Current.InstalledLocation;
                StorageFolder extrasFolder = await installedLocation.GetFolderAsync("Assets\\PythonExtras");

                await CopyStorageFolderAsync(extrasFolder, pythonFolder.Path);
            }

            return Result<string>.Ok(pythonFolderPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Failed to install embedded Python to: pythonFolderPath")
                .WithException(ex);
        }
    }

    private static async Task CopyStorageFolderAsync(StorageFolder sourceFolder, string destinationPath)
    {
        if (sourceFolder == null)
        { 
            throw new ArgumentNullException(nameof(sourceFolder));
        }

        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException("Destination path must not be empty", nameof(destinationPath));
        }

        Directory.CreateDirectory(destinationPath);

        var files = await sourceFolder.GetFilesAsync();
        foreach (var file in files)
        {
            var targetFilePath = Path.Combine(destinationPath, file.Name);
            using (var sourceStream = await file.OpenStreamForReadAsync())
            using (var destinationStream = File.Create(targetFilePath))
            {
                await sourceStream.CopyToAsync(destinationStream);
            }
        }

        var subfolders = await sourceFolder.GetFoldersAsync();
        foreach (var subfolder in subfolders)
        {
            var subfolderPath = Path.Combine(destinationPath, subfolder.Name);
            await CopyStorageFolderAsync(subfolder, subfolderPath);
        }
    }
}
