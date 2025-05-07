using System.Text.Json;
using Celbridge.Commands;
using Celbridge.Entities;
using Celbridge.Messaging;
using Celbridge.Screenplay.Commands;
using Celbridge.Screenplay.Services;

namespace Celbridge.Screenplay.Components;

public class ScreenplayDataEditor : ComponentEditorBase
{
    private IMessengerService _messengerService;
    private ICommandService _commandService;

    private const string _configPath = "Celbridge.Screenplay.Assets.Components.ScreenplayDataComponent.json";
    private const string _formPath = "Celbridge.Screenplay.Assets.Forms.ScreenplayDataForm.json";

    public const string ComponentType = "Screenplay.ScreenplayData";

    [ComponentProperty]
    public string ErrorMessage { get; set; } = string.Empty;

    public ScreenplayDataEditor(
        IMessengerService messengerService,
        ICommandService commandService)
    {
        _messengerService = messengerService;
        _commandService = commandService;
    }

    public override string GetComponentConfig()
    {
        return LoadEmbeddedResource(_configPath);
    }

    public override string GetComponentForm()
    {
        return LoadEmbeddedResource(_formPath);
    }

    public override ComponentSummary GetComponentSummary()
    {
        return new ComponentSummary(string.Empty, string.Empty);
    }

    public override void OnFormLoaded()
    {
        base.OnFormLoaded();
        _messengerService.Register<SaveScreenplayErrorMessage>(this, OnSaveScreenplayErrorMessage);
    }

    public override void OnFormUnloaded()
    {
        base.OnFormUnloaded();
        _messengerService.Unregister<SaveScreenplayErrorMessage>(this);
    }

    private void OnSaveScreenplayErrorMessage(object recipient, SaveScreenplayErrorMessage message)
    {
        var sceneResource = message.SceneResource;
        var sceneFile = Path.GetFileName(sceneResource);

        var errorMessage = $"Save failed. Please fix all errors in '{sceneFile}' and try again.";

        var json = JsonSerializer.Serialize(errorMessage);
        SetProperty("/errorMessage", json);
    }

    public override void OnButtonClicked(string buttonId)
    {
        // Clear the error message
        var json = JsonSerializer.Serialize(string.Empty);
        SetProperty("/errorMessage", json);

        if (buttonId == "LoadScreenplay")
        {
            _commandService.Execute<LoadScreenplayCommand>(command =>
            {
                command.WorkbookResource = Component.Key.Resource;
            });
        }
        else if (buttonId == "SaveScreenplay")
        {
            _commandService.Execute<SaveScreenplayCommand>(command =>
            {
                command.WorkbookResource = Component.Key.Resource;
            });
        }
    }
}
