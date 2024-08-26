using Celbridge.Resources;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json.Linq;

namespace Celbridge.Projects.Services;

public class ProjectService : IProjectService
{
    private const string ProjectDataFileKey = "projectDataFile";
    private const string DefaultProjectDataFile = "Library/ProjectData/ProjectData.db";
    private const string DefaultLogFolder = "Library/Logs";

    public IProject? LoadedProject { get; private set; }

    public Result ValidateNewProjectConfig(NewProjectConfig config)
    {
        if (config is null)
        {
            return Result.Fail("New project config is null.");
        }

        if (string.IsNullOrWhiteSpace(config.Folder))
        {
            return Result.Fail("Project folder is empty.");
        }

        if (!ResourceKey.IsValidSegment(config.ProjectName))
        {
            return Result.Fail($"Project name is not valid: {config.ProjectName}");
        }

        return Result.Ok();
    }

    public async Task<Result> CreateProjectAsync(NewProjectConfig config)
    {
        try
        {
            var fileProvider = new PhysicalFileProvider(config.Folder);
            var projectFolder = config.ProjectFolder;

            // Check if the project folder already exists using the file provider
            if (fileProvider.GetDirectoryContents(config.ProjectName).Exists)
            {
                return Result<string>.Fail($"Project folder already exists: {projectFolder}");
            }

            // Create the project directory
            Directory.CreateDirectory(projectFolder);

            // Write the .celbridge Json file in the project folder
            var projectJson = $$"""
                {
                    "{{ProjectDataFileKey}}": "{{DefaultProjectDataFile}}"
                }
                """;
            await File.WriteAllTextAsync(config.ProjectFilePath, projectJson);

            // Create a database file inside a folder named after the project
            var databasePath = Path.Combine(projectFolder, DefaultProjectDataFile);
            string dataFolderPath = Path.GetDirectoryName(databasePath)!;
            Directory.CreateDirectory(dataFolderPath);

            var logFolderPath = Path.Combine(projectFolder, DefaultLogFolder);
            Directory.CreateDirectory(logFolderPath);

            var createResult = await Project.CreateProjectAsync(config.ProjectFilePath, databasePath, logFolderPath);
            if (createResult.IsFailure)
            {
                return Result.Fail($"Failed to create project database: {config.ProjectName}");
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to create project: {config.ProjectName}. {ex.Message}");
        }
    }

    public Result LoadProject(string projectPath)
    {
        try
        {
            var projectFolder = Path.GetDirectoryName(projectPath)!;
            var fileProvider = new PhysicalFileProvider(projectFolder);

            var projectJsonFileInfo = fileProvider.GetFileInfo(Path.GetFileName(projectPath));
            if (!projectJsonFileInfo.Exists)
            {
                return Result.Fail($"Project file does not exist: {projectPath}");
            }

            using var stream = projectJsonFileInfo.CreateReadStream();
            using var reader = new StreamReader(stream);
            var projectJsonData = reader.ReadToEnd();

            var jsonObject = JObject.Parse(projectJsonData);
            Guard.IsNotNull(jsonObject);

            string projectDataPathRelative = jsonObject[ProjectDataFileKey]!.ToString();
            string projectDataPath = Path.GetFullPath(Path.Combine(projectFolder, projectDataPathRelative));
            string logFolderPath = Path.GetFullPath(Path.Combine(projectFolder, DefaultLogFolder));

            var loadResult = Project.LoadProject(projectPath, projectDataPath, logFolderPath);
            if (loadResult.IsFailure)
            {
                return Result.Fail($"Failed to load project database: {projectDataPath}");
            }

            LoadedProject = loadResult.Value;

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to load project database. {ex.Message}");
        }
    }

    public Result UnloadProject()
    {
        if (LoadedProject is null)
        {
            return Result.Ok();
        }

        var disposableProject = LoadedProject as IDisposable;
        Guard.IsNotNull(disposableProject);
        disposableProject.Dispose();
        LoadedProject = null;

        return Result.Ok();
    }
}
