using Celbridge.Commands;
using Celbridge.Documents;
using Celbridge.Entities;
using Celbridge.Explorer;
using Celbridge.Messaging;
using Celbridge.Screenplay.Commands;
using Celbridge.Screenplay.Services;
using System.Text.Json;

namespace Celbridge.Screenplay.Components;

public class ScreenplayDataEditor : ComponentEditorBase
{
    private IMessengerService _messengerService;
    private ICommandService _commandService;

    private const string _configPath = "Celbridge.Screenplay.Assets.Components.ScreenplayDataComponent.json";
    private const string _formPath = "Celbridge.Screenplay.Assets.Forms.ScreenplayDataForm.json";

    public const string ComponentType = "Screenplay.ScreenplayData";

    [ComponentProperty]
    public bool ShowInfo { get; set; }

    [ComponentProperty]
    public string InfoMessage { get; set; } = string.Empty;

    [ComponentProperty]
    public string InfoTitle { get; set; } = string.Empty;

    [ComponentProperty]
    public string InfoSeverity { get; set; } = nameof(InfoBarSeverity.Informational);

    private ResourceKey _errorSceneResource;

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
        _errorSceneResource = message.SceneResource;
        var sceneFile = Path.GetFileName(_errorSceneResource);

        var infoTitle = "Save failed";
        var infoMessage = $"Please fix '{sceneFile}' and try again.";

        SetProperty("/showInfo", JsonSerializer.Serialize(true));
        SetProperty("/infoTitle", JsonSerializer.Serialize(infoTitle));
        SetProperty("/infoMessage", JsonSerializer.Serialize(infoMessage));
        SetProperty("/infoSeverity", JsonSerializer.Serialize(nameof(InfoBarSeverity.Error)));
    }

    public override void OnButtonClicked(string buttonId)
    {
        bool clearErrorMessage = false;

        if (buttonId == "LoadScreenplay")
        {
            _commandService.Execute<LoadScreenplayCommand>(command =>
            {
                command.WorkbookResource = Component.Key.Resource;
            });

            clearErrorMessage = true;
        }
        else if (buttonId == "SaveScreenplay")
        {
            _commandService.Execute<SaveScreenplayCommand>(command =>
            {
                command.WorkbookResource = Component.Key.Resource;
            });

            clearErrorMessage = true;
        }
        else if (buttonId == "OpenScene")
        {
            _commandService.Execute<IOpenDocumentCommand>(command =>
            {
                command.FileResource= _errorSceneResource;
            });

            _commandService.Execute<ISelectResourceCommand>(command =>
            {
                command.Resource = _errorSceneResource;
            });
        }

        if (clearErrorMessage)
        {
            SetProperty("/showInfo", JsonSerializer.Serialize(Visibility.Collapsed.ToString()));
            SetProperty("/infoTitle", JsonSerializer.Serialize(string.Empty));
            SetProperty("/infoMessage", JsonSerializer.Serialize(string.Empty));
            SetProperty("/infoSeverity", JsonSerializer.Serialize(InfoBarSeverity.Informational));
            _errorSceneResource = string.Empty;
        }
    }
}
