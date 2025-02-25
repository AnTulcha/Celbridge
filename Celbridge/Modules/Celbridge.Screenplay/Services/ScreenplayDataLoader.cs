using Celbridge.Explorer;
using Celbridge.Logging;
using Celbridge.Screenplay.Models;
using ClosedXML.Excel;

namespace Celbridge.Screenplay.Services;

public class ScreenplayDataLoader
{
    private ILogger<ScreenplayDataLoader> _logger;
    private IExplorerService _explorerService;

    public ScreenplayDataLoader(
        ILogger<ScreenplayDataLoader> logger,
        IExplorerService explorerService)
    {
        _logger = logger;
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
        var namespace_lines = createResult.Value;

        // Create a .scene file for each namespace
        foreach (var kv in namespace_lines)
        {
            var namespace_key = kv.Key;
            var line_list = kv.Value;

            foreach (var line in line_list)
            {
                var category = line.Category;
                var dialogue_key = line.DialogueKey;
    
                // Todo: Create an entity file for each namespace, save it in the entities folder
                // Either serialize DialogueLine to JSON, or add a method that formats the DialogueLine as JSON text.
                _logger.LogInformation($"{category}, {namespace_key}, {dialogue_key}");
                break;
            }
        }


        await Task.CompletedTask;

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
}
