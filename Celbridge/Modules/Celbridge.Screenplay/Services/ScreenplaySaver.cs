using Celbridge.Explorer;
using Celbridge.Logging;
using Celbridge.Screenplay.Components;
using Celbridge.Workspace;
using ClosedXML.Excel;

namespace Celbridge.Screenplay.Services;

public class ScreenplaySaver
{
    private ILogger<ScreenplayLoader> _logger;
    private IExplorerService _explorerService;
    private IWorkspaceWrapper _workspaceWrapper;

    public ScreenplaySaver(
        ILogger<ScreenplayLoader> logger,
        IExplorerService explorerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;
        _explorerService = explorerService;
        _workspaceWrapper = workspaceWrapper;
    }

    public async Task<Result> SaveScreenplayAsync(ResourceKey screenplayResource)
    {
        try
        {
            var extension = Path.GetExtension(screenplayResource);
            if (extension != ".xlsx")
            {
                return Result.Fail($"Unsupported file type: {extension}");
            }

            var entityService = _workspaceWrapper.WorkspaceService.EntityService;
            var resourceRegistry = _explorerService.ResourceRegistry;
            var workbookFilePath = resourceRegistry.GetResourcePath(screenplayResource);
            var screenplayFolderPath = Path.GetFileNameWithoutExtension(workbookFilePath);

            // Acquire the ScreenplayData component from the screenplay resource
            var getComponentResult = entityService.GetComponentOfType(screenplayResource, ScreenplayDataEditor.ComponentType);
            if (getComponentResult.IsFailure)
            {
                return Result.Fail($"Failed to get ScreenplayData component from workbook file resource '{screenplayResource}'")
                    .WithErrors(getComponentResult);
            }
            var screenplayData = getComponentResult.Value;

            // Open the workbook file.
            // It's best to do this before we make any other changes, e.g. in case the file is locked.
            using var workbook = new XLWorkbook(workbookFilePath);

            // Todo: Implement the logic for saving the dialogue data back to the "Dialogue" sheet
            // Find all .scene files in the screenplay folder
            // For each .scene file
            //  Get the namespace from the SceneComponent on the .scene file entity
            // Sort the namespaces in the same order that they appear in the "Dialogue" sheet

            // Clear the "DialogueSheet" data rows (leave the header intact)
            // For each namespace
            //  Process all Line and Empty components on the corresponding .scene file entity
            //  Add a dilaogue line row in the "Lines" sheet for each Line
            //  Add a scene note line in the "Lines" sheet for each scene note.

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to import screenplay data from workbook")
                .WithException(ex);
        }
    }
}
