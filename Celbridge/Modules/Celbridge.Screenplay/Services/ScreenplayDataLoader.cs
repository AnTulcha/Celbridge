using Celbridge.Logging;

namespace Celbridge.Screenplay.Services;

public class ScreenplayDataLoader
{
    private ILogger<ScreenplayDataLoader> _logger;

    public ScreenplayDataLoader(ILogger<ScreenplayDataLoader> logger)
    {
        _logger = logger;
    }

    public Result LoadData(ResourceKey resource)
    {
        _logger.LogInformation($"Loading data from: {resource}");

        // Use commands for all operations to support undo / redo
        // May need to refactor the group id stuff to support this?

        // Todo: Delete any existing scene folder
        // Todo: Load the Excel file
        // Todo: Create folders for each category
        // Todo: Create a .scene file for each namespace
        // Todo: Add Line components for each line in the spreadsheet

        return Result.Ok();
    }
}
