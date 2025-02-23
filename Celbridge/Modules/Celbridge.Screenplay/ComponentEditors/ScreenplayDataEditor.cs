using System;
using Celbridge.Entities;
using Celbridge.Logging;
using Celbridge.Screenplay.Services;

namespace Celbridge.Screenplay.Components;

public class ScreenplayDataEditor : ComponentEditorBase
{
    private IServiceProvider _serviceProvider;
    private ILogger<ScreenplayDataEditor> _logger;

    private const string _configPath = "Celbridge.Screenplay.Assets.Components.ScreenplayDataComponent.json";
    private const string _formPath = "Celbridge.Screenplay.Assets.Forms.ScreenplayDataForm.json";

    public const string ComponentType = "Screenplay.ScreenplayData";

    public ScreenplayDataEditor(
        IServiceProvider serviceProvider,
        ILogger<ScreenplayDataEditor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
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

    public override void OnButtonClicked(string buttonId)
    {
        var dataLoader = _serviceProvider.AcquireService<ScreenplayDataLoader>();

        var resource = Component.Key.Resource;

        var loadResult = dataLoader.LoadData(resource);
        if (loadResult.IsFailure)
        {
            _logger.LogError($"Failed to load data: {loadResult.Error}");
            return;
        }
    }
}
