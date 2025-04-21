using Celbridge.Entities;
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

    private record SceneData(string Category, string Namespace, IComponentProxy SceneComponent, List<IComponentProxy> DialogueComponents);

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

            var screenplayFolderPath = Path.GetDirectoryName(workbookFilePath);
            if (string.IsNullOrEmpty(screenplayFolderPath))
            {
                return Result.Fail($"Failed to get screenplay folder path for resource '{screenplayResource}'");
            }

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

            var dialogueSheet = workbook.Worksheet("Dialogue");

            // Find all .scene files in the screenplay folder
            var sceneFiles = Directory.GetFiles(screenplayFolderPath, "*.scene", SearchOption.AllDirectories).ToList();
            if (sceneFiles.Count == 0)
            {
                return Result.Fail($"No scene files found in folder '{screenplayFolderPath}'");
            }

            // Collect all dialogue lines for all scene files found
            var collectResult = CollectSceneData(entityService, sceneFiles);
            if (collectResult.IsFailure)
            {
                return Result.Fail($"Failed to collect scene data from workbook file resource '{screenplayResource}'")
                    .WithErrors(collectResult);
            }
            var sceneDataList = collectResult.Value;

            // Duplicate the Dialogue worksheet
            if (workbook.Worksheets.Contains("EditedDialogue"))
            {
                workbook.Worksheet("EditedDialogue").Delete();
                workbook.Save();
            }
            var editedSheet = dialogueSheet.CopyTo("EditedDialogue");

            // Delete all rows except the header
            var lastRow = editedSheet.LastRowUsed()?.RowNumber() ?? 1;
            var range = editedSheet.Range(2, 1, lastRow, 14);
            range.Clear();

            // Output all dialogue lines to the "Dialogue" spreadsheet.
            // Todo: Copy bark lines from the Dialogue worksheet at the end.

            int rowIndex = 2;
            foreach (var sceneData in sceneDataList)
            {
                var sceneCategory = sceneData.Category;
                var sceneNamespace = sceneData.Namespace;

                int sceneNoteIndex = 1;
                foreach (var dialogueComponent in sceneData.DialogueComponents)
                {
                    editedSheet.Cell(rowIndex, 1).Value = sceneCategory;
                    editedSheet.Cell(rowIndex, 2).Value = sceneNamespace;

                    if (dialogueComponent.Schema.ComponentType == LineEditor.ComponentType)
                    {
                        editedSheet.Cell(rowIndex, 3).Value = dialogueComponent.GetString(LineEditor.DialogueKey);
                        editedSheet.Cell(rowIndex, 4).Value = dialogueComponent.GetString(LineEditor.CharacterId);
                        editedSheet.Cell(rowIndex, 5).Value = dialogueComponent.GetString(LineEditor.SpeakingTo);
                        editedSheet.Cell(rowIndex, 6).Value = dialogueComponent.GetString(LineEditor.SourceText);
                        editedSheet.Cell(rowIndex, 7).Value = dialogueComponent.GetString(LineEditor.ContextNotes);
                        editedSheet.Cell(rowIndex, 8).Value = dialogueComponent.GetString(LineEditor.Direction);
                        editedSheet.Cell(rowIndex, 9).Value = dialogueComponent.GetString(LineEditor.GameArea);
                        editedSheet.Cell(rowIndex, 10).Value = dialogueComponent.GetString(LineEditor.TimeConstraint);
                        editedSheet.Cell(rowIndex, 11).Value = dialogueComponent.GetString(LineEditor.SoundProcessing);
                        editedSheet.Cell(rowIndex, 12).Value = dialogueComponent.GetString(LineEditor.Platform);
                        editedSheet.Cell(rowIndex, 13).Value = dialogueComponent.GetString(LineEditor.LinePriority);
                        editedSheet.Cell(rowIndex, 14).Value = dialogueComponent.GetString(LineEditor.ProductionStatus);
                    }
                    else if (dialogueComponent.Schema.ComponentType == EntityConstants.EmptyComponentType)
                    {
                        var commentText = dialogueComponent.GetString("/comment");

                        var dialogueKey = $"SceneNote.{sceneNamespace}.Note{sceneNoteIndex}";
                        editedSheet.Cell(rowIndex, 3).Value = dialogueKey;
                        editedSheet.Cell(rowIndex, 4).Value = "SceneNote";
                        editedSheet.Cell(rowIndex, 6).Value = commentText;
                        sceneNoteIndex++;
                    }
                    rowIndex++;
                }
            }

            // Save the workbook
            workbook.Save();

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to import screenplay data from workbook")
                .WithException(ex);
        }
    }

    private Result<List<SceneData>> CollectSceneData(IEntityService entityService, IReadOnlyList<string> sceneFiles)
    {
        var processedNamespaces = new HashSet<string>();

        // Build a dictionary mapping each scene namespace to it's list of dialogue components
        var sceneDataList = new List<SceneData>();
        foreach (var sceneFile in sceneFiles)
        {
            // Get the resource key for the scene file
            var getResourceResult = _explorerService.ResourceRegistry.GetResourceKey(sceneFile);
            if (getResourceResult.IsFailure)
            {
                return Result<List<SceneData>>.Fail($"Failed to get resource key for scene file '{sceneFile}'")
                    .WithErrors(getResourceResult);
            }
            var sceneResource = getResourceResult.Value;

            // Get all components in the scene entity
            var getComponentsResult = entityService.GetComponents(sceneResource);
            if (getComponentsResult.IsFailure)
            {
                Result<List<SceneData>>.Fail($"Failed to get components for scene file '{sceneFile}'")
                    .WithErrors(getComponentsResult);
            }
            var components = getComponentsResult.Value;

            // Get the Scene root component for the scene file
            if (components.Count == 0)
            {
                Result<List<SceneData>>.Fail($"No components found for scene file '{sceneFile}'");
            }
            var sceneComponent = components[0];
            if (sceneComponent.Schema.ComponentType != SceneEditor.ComponentType)
            {
                Result<List<SceneData>>.Fail($"Failed to get Scene root Component for scene file '{sceneFile}'");
            }

            var sceneCategory = sceneComponent.GetString(SceneEditor.Category);
            if (sceneCategory != "Cinematic" &&
                sceneCategory != "Conversation" &&
                sceneCategory != "Bark")
            {
                Result<List<SceneData>>.Fail($"Invalid category '{sceneCategory}'");
            }

            // Get the scene namespace
            var sceneNamespace = sceneComponent.GetString(SceneEditor.Namespace);
            if (processedNamespaces.Contains(sceneNamespace))
            {
                return Result<List<SceneData>>.Fail($"Duplicate declaration of namespace '{sceneNamespace}' in scene file '{sceneFile}'.");
            }
            processedNamespaces.Add(sceneNamespace);

            // Build list of all Line and Empty components in the scene entity
            var dialogueComponents = new List<IComponentProxy>();
            foreach (var component in components)
            {
                if (component.Schema.ComponentType == LineEditor.ComponentType ||
                    component.Schema.ComponentType == EntityConstants.EmptyComponentType)
                {
                    // Add the component to the list of dialogue components
                    dialogueComponents.Add(component);
                }
            }

            // Add the scene data to the list
            var sceneData = new SceneData(sceneCategory, sceneNamespace, sceneComponent, dialogueComponents);
            sceneDataList.Add(sceneData);
        }

        // Sort the scene data list
        sceneDataList = sceneDataList.OrderBy(sceneData => sceneData.Category)
            .ThenBy(sceneData => sceneData.Namespace)
            .ToList();

        return Result<List<SceneData>>.Ok(sceneDataList);
    }
}
