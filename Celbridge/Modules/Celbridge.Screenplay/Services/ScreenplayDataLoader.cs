using Celbridge.Explorer;
using Celbridge.Logging;
using Celbridge.Screenplay.Models;
using Celbridge.Workspace;
using ClosedXML.Excel;

namespace Celbridge.Screenplay.Services;

public class ScreenplayDataLoader
{
    private ILogger<ScreenplayDataLoader> _logger;
    private IExplorerService _explorerService;
    private IWorkspaceWrapper _workspaceWrapper;

    public ScreenplayDataLoader(
        ILogger<ScreenplayDataLoader> logger,
        IExplorerService explorerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;
        _explorerService = explorerService;
        _workspaceWrapper = workspaceWrapper;
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

        // Update the resource registry to delete any existing entity data files before
        // we start adding new .scene files.
        var updateResult = resourceRegistry.UpdateResourceRegistry();
        if (updateResult.IsFailure)
        {
            return Result.Fail($"Failed to update resource registry")
                .WithErrors(updateResult);
        }

        // Load the dialogue lines from the Excel file
        var loadResult = LoadDialogueLines(excelFilePath, "Lines");
        if (loadResult.IsFailure)
        {
            return Result.Fail($"Failed to load dialogue lines from Excel")
                .WithErrors(loadResult);
        }
        var lines = loadResult.Value;

        // Split the dialogue lines by namespace
        var createResult = CreateNamespaceLines(lines);
        if (createResult.IsFailure)
        {
            return Result.Fail($"Failed create namespace lines dictionary")
                .WithErrors(createResult);
        }
        var namespaceLines = createResult.Value;

        // Save a .scene file for each namespace
        var saveResult = await SaveSceneFilesAsync(namespaceLines, excelResource);
        if (saveResult.IsFailure)
        {
            return Result.Fail($"Failed to save .scene files")
                .WithErrors(loadResult);
        }

        return Result.Ok();
    }

    private Result<List<DialogueLine>> LoadDialogueLines(string filePath, string sheetName)
    {
        var lines = new List<DialogueLine>();

        // Open the Excel file
        using var workbook = new XLWorkbook(filePath);

        var worksheet = workbook.Worksheet(sheetName);

        // Find the used range
        var range = worksheet.RangeUsed();
        if (range == null)
        {
            return Result<List<DialogueLine>>.Fail("The sheet is empty.");
        }

        //
        // Read header row and map column names to indexes
        //

        var headerRow = range.FirstRowUsed();
        var columnMap = new Dictionary<string, int>();

        foreach (var cell in headerRow.CellsUsed())
        {
            var columnName = cell.GetValue<string>().Trim();
            columnMap[columnName] = cell.Address.ColumnNumber;
        }

        //
        // Validate required columns
        //

        string[] requiredColumns = { "DialogueKey", "Namespace", "Category", "SourceText", "ContextNotes" };
        foreach (var col in requiredColumns)
        {
            if (!columnMap.ContainsKey(col))
            {
                return Result<List<DialogueLine>>.Fail($"Missing required column '{col}'");
            }
        }

        //
        // Read data rows, skipping the header
        //

        foreach (var row in range.RowsUsed().Skip(1))
        {
            try
            {

                var line = new DialogueLine
                (
                    Category: TryGetValue<string>(row, columnMap, nameof(DialogueLine.Category)),
                    Namespace: TryGetValue<string>(row, columnMap, nameof(DialogueLine.Namespace)),
                    DialogueKey: TryGetValue<string>(row, columnMap, nameof(DialogueLine.DialogueKey)),
                    SourceText: TryGetValue<string>(row, columnMap, nameof(DialogueLine.SourceText))
                );

                lines.Add(line);
            }
            catch (Exception ex)
            {
                return Result<List<DialogueLine>>.Fail($"An error occurred when reading lines from Excel")
                    .WithException(ex);
            }
        }

        return Result<List<DialogueLine>>.Ok(lines);
    }

    private T TryGetValue<T>(IXLRangeRow row, Dictionary<string, int> columnMap, string columnName)
    {
        if (!columnMap.TryGetValue(columnName, out int colIndex))
            throw new Exception($"Missing column '{columnName}'");

        var cell = row.Cell(colIndex);
        try
        {
            return cell.GetValue<T>();
        }
        catch
        {
            throw new Exception($"Invalid data in column '{columnName}' at row {row.RowNumber()}");
        }
    }

    private Result<Dictionary<string, List<DialogueLine>>> CreateNamespaceLines(List<DialogueLine> lines)
    {
        var namespace_lines = new Dictionary<string, List<DialogueLine>>();

        var previous_namespace = string.Empty;
        int row_index = 1;
        foreach (var line in lines)
        {
            var namespace_key = line.Namespace;

            // Acquire a list of lines for the namespace
            List<DialogueLine> filtered_lines;
            if (!namespace_lines.TryGetValue(namespace_key, out filtered_lines!))
            {
                filtered_lines = new List<DialogueLine>();
                namespace_lines[namespace_key] = filtered_lines;
            }
            else
            {
                // Check that namespaces are defined contiguously
                if (previous_namespace != namespace_key)
                {
                    // This namespace has already been processed, but the previous line's namespace
                    // does not match the current line's namespace.
                    return Result<Dictionary<string, List<DialogueLine>>>.Fail($"Non-contiguous namespace at row {row_index}");
                }
            }

            // Add the line to the list of lines for the namespace
            filtered_lines.Add(line);

            previous_namespace = namespace_key;
            row_index++;
        }

        return Result<Dictionary<string, List<DialogueLine>>>.Ok(namespace_lines);
    }

    private async Task<Result> SaveSceneFilesAsync(Dictionary<string, List<DialogueLine>> namespaceLines, ResourceKey excelResource)
    {
        var screenplayFolderResource = Path.GetFileNameWithoutExtension(excelResource);

        var resourceRegistry = _explorerService.ResourceRegistry;
        var sceneFolderPath = resourceRegistry.GetResourcePath(screenplayFolderResource);

        var entityService = _workspaceWrapper.WorkspaceService.EntityService;
        var entityFolderPath = entityService.GetEntityDataPath(screenplayFolderResource);

        // Remove the .json extension
        entityFolderPath = entityFolderPath.Substring(0, entityFolderPath.LastIndexOf('.'));

        // Create a .scene file for each namespace
        foreach (var kv in namespaceLines)
        {
            var namespace_key = kv.Key;
            var line_list = kv.Value;

            if (line_list.Count == 0)
            {
                continue;
            }

            // Get the category from the first line in the list
            var category = line_list[0].Category;

            // Create a .scene file
            var sceneFilePath = Path.Combine(sceneFolderPath, category, $"{namespace_key}.scene");

            var sceneFolder = Path.GetDirectoryName(sceneFilePath);
            if (!string.IsNullOrEmpty(sceneFolder) &&
                !Directory.Exists(sceneFolder))
            {
                Directory.CreateDirectory(sceneFolder);
            }

            await File.WriteAllTextAsync(sceneFilePath, string.Empty);

            // Create an entity data .json file

            var entityFilePath = Path.Combine(entityFolderPath, category, $"{namespace_key}.scene.json");

            var entityFolder = Path.GetDirectoryName(entityFilePath);
            if (!string.IsNullOrEmpty(entityFolder) &&
                !Directory.Exists(entityFolder))
            {
                Directory.CreateDirectory(entityFolder);
            }

            // Generate component data for each line
            foreach (var line in line_list)
            {
                var dialogue_key = line.DialogueKey;
            }

            var entityData = $$"""
            {
                "_entityVersion": 1,
                "_components": [
                {
                    "_type": "Screenplay.Scene#1",
                    "dialogueFile": "{{excelResource}}"
                }
                ]
            }
            """;

            await File.WriteAllTextAsync(entityFilePath, entityData);
        }

        return Result.Ok();
    }
}
