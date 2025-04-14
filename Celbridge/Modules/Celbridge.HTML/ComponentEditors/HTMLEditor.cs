using Celbridge.Commands;
using Celbridge.Documents;
using Celbridge.Entities;
using Celbridge.Logging;
using System.Text.Json;

namespace Celbridge.HTML.Components;

public class HTMLEditor : ComponentEditorBase
{
    private readonly ILogger<HTMLEditor> _logger;
    private readonly ICommandService _commandService;

    private const string _configPath = "Celbridge.HTML.Assets.Components.HTMLComponent.json";
    private const string _componentFormPath = "Celbridge.HTML.Assets.Forms.HTMLForm.json";
    private const string _componentRootFormPath = "Celbridge.HTML.Assets.Forms.HTMLRootForm.json";

    private const string _openDocumentButtonId = "OpenDocument";
    private const string _editorButtonId = "Editor";
    private const string _editorAndPreviewButtonId = "EditorAndPreview";
    private const string _previewButtonId = "Preview";

    public const string ComponentType = "HTML.HTML";

    public HTMLEditor(
        ILogger<HTMLEditor> logger,
        ICommandService commandService)
    {
        _logger = logger;
        _commandService = commandService;
    }

    public override string GetComponentConfig()
    {
        return LoadEmbeddedResource(_configPath);
    }

    public override string GetComponentForm()
    {
        return LoadEmbeddedResource(_componentFormPath);
    }

    public override string GetComponentRootForm()
    {
        return LoadEmbeddedResource(_componentRootFormPath);
    }

    public override ComponentSummary GetComponentSummary()
    {
        return new ComponentSummary(string.Empty, string.Empty);
    }

    protected override void OnFormPropertyChanged(string propertyPath)
    {
        if (propertyPath == "/editorMode")
        {
            // Notify updates to "virtual" form properties
            NotifyFormPropertyChanged("/editorEnabled");
            NotifyFormPropertyChanged("/editorAndPreviewEnabled");
            NotifyFormPropertyChanged("/previewEnabled");
        }
    }

    public override void OnButtonClicked(string buttonId)
    {
        switch (buttonId)
        {
            case _openDocumentButtonId:
                OpenDocument();
                break;

            case _editorButtonId:
                SetEditorMode(EditorMode.Editor);
                OpenDocument();
                break;

            case _editorAndPreviewButtonId:
                SetEditorMode(EditorMode.EditorAndPreview);
                OpenDocument();
                break;

            case _previewButtonId:
                SetEditorMode(EditorMode.Preview);
                OpenDocument();
                break;
        }
    }

    protected override Result<string> TryGetProperty(string propertyPath)
    {
        if (propertyPath == "/editorEnabled")
        {
            var editorMode = Component.GetString(DocumentConstants.EditorMode);

            bool isEnabled = editorMode == "EditorAndPreview" || editorMode == "Preview";
            var jsonValue = JsonSerializer.Serialize(isEnabled);

            return Result<String>.Ok(jsonValue);
        }
        else if (propertyPath == "/editorAndPreviewEnabled")
        {
            var editorMode = Component.GetString(DocumentConstants.EditorMode);

            bool isEnabled = editorMode == "Editor" || editorMode == "Preview";
            var jsonValue = JsonSerializer.Serialize(isEnabled);

            return Result<String>.Ok(jsonValue);
        }
        else if (propertyPath == "/previewEnabled")
        {
            var editorMode = Component.GetString(DocumentConstants.EditorMode);

            bool isEnabled = editorMode == "Editor" || editorMode == "EditorAndPreview";
            var jsonValue = JsonSerializer.Serialize(isEnabled);

            return Result<String>.Ok(jsonValue);
        }

        return Result<string>.Fail();
    }

    private void OpenDocument()
    {
        var resource = Component.Key.Resource;

        // Execute a command to open the HTML document.
        _commandService.Execute<IOpenDocumentCommand>(command =>
        {
            command.FileResource = resource;
            command.ForceReload = false;
        });
    }

    private void SetEditorMode(EditorMode editorMode)
    {
        Component.SetString(DocumentConstants.EditorMode, editorMode.ToString());
    }
}
