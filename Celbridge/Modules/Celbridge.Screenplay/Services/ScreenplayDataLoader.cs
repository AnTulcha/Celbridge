using Celbridge.Commands;
using Celbridge.Explorer;
using Celbridge.Logging;

namespace Celbridge.Screenplay.Services;

public class ScreenplayDataLoader
{
    private ILogger<ScreenplayDataLoader> _logger;
    private ICommandService _commandService;
    private IExplorerService _explorerService;

    public ScreenplayDataLoader(
        ILogger<ScreenplayDataLoader> logger,
        ICommandService commandService,
        IExplorerService explorerService)
    {
        _logger = logger;
        _commandService = commandService;
        _explorerService = explorerService;
    }

    public async Task<Result> ImportData(ResourceKey excelResource)
    {
        var extension = Path.GetExtension(excelResource);
        if (extension != ".xlsx")
        {
            return Result.Fail($"Unsupported file type: {extension}");
        }

        var resourceRegistry = _explorerService.ResourceRegistry;
        var excelFilePath = resourceRegistry.GetResourcePath(excelResource);
        var screenplayFolderPath = Path.GetFileNameWithoutExtension(excelFilePath);

        // Delete the screenplay folder if it already exists
        if (Directory.Exists(screenplayFolderPath))
        {
            Directory.Delete(screenplayFolderPath, true);
        }

        // Create a new screenplay folder for the screenplay
        Directory.CreateDirectory(screenplayFolderPath);

        // Update the resource registry to delete any associated entity data files before
        // we start adding .scene files.
        var updateResult = resourceRegistry.UpdateResourceRegistry();
        if (updateResult.IsFailure)
        {
            return Result.Fail($"Failed to update resource registry")
                .WithErrors(updateResult);
        }

        // Load the Excel file and generate .scene files
        var loadResult = await LoadScreenplayDataAsync(excelFilePath, screenplayFolderPath);
        if (loadResult.IsFailure)
        {
            return Result.Fail($"Failed to load screenplay data")
                .WithErrors(loadResult);
        }

        return Result.Ok();
    }

    private async Task<Result> LoadScreenplayDataAsync(string excelFilePath, string screenplayFolderPath)
    {
        await Task.CompletedTask;

        return Result.Ok();
    }
}
