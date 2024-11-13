using Celbridge.Entities;
using Celbridge.ExtensionAPI;
using Celbridge.Logging;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Mvvm.ComponentModel;

using Path = System.IO.Path;

namespace Celbridge.Documents.ViewModels;

public partial class TextEditorDocumentViewModel : ObservableObject
{
    private readonly ILogger<TextEditorDocumentViewModel> _logger;
    private readonly IMessengerService _messengerService;
    private readonly IDocumentsService _documentsService;
    private readonly IResourceDataService _resourceDataService;

    private ResourceKey _fileResource;

    [ObservableProperty]
    private bool _showEditor = true;

    [ObservableProperty]
    private bool _showPreview = true;

    public TextEditorDocumentViewModel(
        ILogger<TextEditorDocumentViewModel> logger,
        IMessengerService messengerService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _logger = logger;
        _messengerService = messengerService;
        _documentsService = workspaceWrapper.WorkspaceService.DocumentsService;
        _resourceDataService = workspaceWrapper.WorkspaceService.ResourceDataService;
    }

    public void SetFileResource(ResourceKey fileResource)
    {
        _messengerService.Unregister<EntityPropertyChangedMessage>(this);
        _messengerService.Register<EntityPropertyChangedMessage>(this, OnEntityPropertyChangedMessage);

        _fileResource = fileResource;

        UpdatePanelVisibility();
    }

    public Result<PreviewProvider> GetPreviewProvider()
    {
        var fileExtension = Path.GetExtension(_fileResource);
        if (string.IsNullOrEmpty(fileExtension))
        {
            return Result<PreviewProvider>.Fail();
        }

        var getResult = _documentsService.GetPreviewProvider(fileExtension);
        if (getResult.IsFailure)
        {
            return getResult;
        }

        var provider = getResult.Value;

        return Result<PreviewProvider>.Ok(provider);
    }

    private void OnEntityPropertyChangedMessage(object recipient, EntityPropertyChangedMessage message)
    {
        var (resource, propertyPath, _) = message;

        if (resource != _fileResource)
        {
            return;
        }

        if (propertyPath == ResourceDataConstants.TextEditor_ShowEditor ||
            propertyPath == ResourceDataConstants.TextEditor_ShowPreview)
        {
            UpdatePanelVisibility();
        }
    }

    private void UpdatePanelVisibility()
    {
        try
        {
            // Todo: Is it safe to hold onto the acquired resource data?
            var acquireResult = _resourceDataService.AcquireResourceData(_fileResource);
            if (acquireResult.IsFailure)
            {
                _logger.LogError(acquireResult.Error);
                return;
            }
            var resourceData = acquireResult.Value;

            ShowEditor = resourceData.GetProperty(ResourceDataConstants.TextEditor_ShowEditor, true);
            ShowPreview = resourceData.GetProperty(ResourceDataConstants.TextEditor_ShowPreview, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}
