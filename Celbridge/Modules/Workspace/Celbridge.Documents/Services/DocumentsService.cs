using Celbridge.Commands;
using Celbridge.Documents.Views;
using Celbridge.Messaging;
using Celbridge.Workspace;
using CommunityToolkit.Diagnostics;

using Path = System.IO.Path;

namespace Celbridge.Documents.Services;

using IDocumentsLogger = Logging.ILogger<DocumentsService>;

public class DocumentsService : IDocumentsService, IDisposable
{
    private const string PreviousOpenDocumentsKey = "PreviousOpenDocuments";
    private const string PreviousSelectedDocumentKey = "PreviousSelectedDocument";

    private readonly IServiceProvider _serviceProvider;
    private readonly IMessengerService _messengerService;
    private readonly IDocumentsLogger _logger;
    private readonly ICommandService _commandService;
    private readonly IWorkspaceWrapper _workspaceWrapper;

    private IDocumentsPanel? _documentsPanel;
    public IDocumentsPanel DocumentsPanel => _documentsPanel!;

    public ResourceKey SelectedDocument { get; private set; }

    public List<ResourceKey> OpenDocuments { get; } = new();

    // This utility is only used internally and is not exposed via IDocumentService
    internal TextEditorWebViewPool TextEditorWebViewPool { get; } = new(3);

    private bool _isWorkspaceLoaded;

    private FileTypeHelper _fileTypeHelper;

    public DocumentsService(
        IServiceProvider serviceProvider,
        IDocumentsLogger logger,
        IMessengerService messengerService,
        ICommandService commandService,
        IWorkspaceWrapper workspaceWrapper)
    {
        _serviceProvider = serviceProvider;
        _messengerService = messengerService;
        _logger = logger;
        _commandService = commandService;
        _workspaceWrapper = workspaceWrapper;

        _messengerService.Register<WorkspaceWillPopulatePanelsMessage>(this, OnWorkspaceWillPopulatePanelsMessage);
        _messengerService.Register<WorkspaceLoadedMessage>(this, OnWorkspaceLoadedMessage);
        _messengerService.Register<OpenDocumentsChangedMessage>(this, OnOpenDocumentsChangedMessage);
        _messengerService.Register<SelectedDocumentChangedMessage>(this, OnSelectedDocumentChangedMessage);
        _messengerService.Register<DocumentResourceChangedMessage>(this, OnDocumentResourceChangedMessage);

        _fileTypeHelper = _serviceProvider.GetRequiredService<FileTypeHelper>();
        var loadResult = _fileTypeHelper.Initialize();
        if (loadResult.IsFailure)
        {
            throw new InvalidProgramException("Failed to initialize file type helper");
        }
    }

    private void OnWorkspaceWillPopulatePanelsMessage(object recipient, WorkspaceWillPopulatePanelsMessage message)
    {
        _documentsPanel = _serviceProvider.GetRequiredService<IDocumentsPanel>();
    }

    private void OnWorkspaceLoadedMessage(object recipient, WorkspaceLoadedMessage message)
    {
        // Once set, this will remain true for the lifetime of the service
        _isWorkspaceLoaded = true;
    }

    private void OnSelectedDocumentChangedMessage(object recipient, SelectedDocumentChangedMessage message)
    {
        SelectedDocument = message.DocumentResource;

        if (_isWorkspaceLoaded)
        {
            // Ignore change events that happen while loading the workspace
            _ = StoreSelectedDocument();
        }
    }

    private void OnOpenDocumentsChangedMessage(object recipient, OpenDocumentsChangedMessage message)
    {
        OpenDocuments.ReplaceWith(message.OpenDocuments);

        if (_isWorkspaceLoaded)
        {
            // Ignore change events that happen while loading the workspace
            _ = StoreOpenDocuments();
        }
    }

    public async Task<Result<IDocumentView>> CreateDocumentView(ResourceKey fileResource)
    {
        //
        // Create the appropriate document view control for this document type
        //

        var createResult = CreateDocumentViewInternal(fileResource);
        if (createResult.IsFailure)
        {
            var failure = Result<IDocumentView>.Fail($"Failed to create document view for file resource: '{fileResource}'");
            failure.MergeErrors(createResult);
            return failure;
        }
        var documentView = createResult.Value;

        //
        // Load the content from the document file
        //

        var setFileResult = await documentView.SetFileResource(fileResource);
        if (setFileResult.IsFailure)
        {
            var failure = Result<IDocumentView>.Fail($"Failed to set file resource for document view: '{fileResource}'");
            failure.MergeErrors(setFileResult);
            return failure;
        }

        var loadResult = await documentView.LoadContent();
        if (loadResult.IsFailure)
        {
            var failure = Result<IDocumentView>.Fail($"Failed to load content for document view: '{fileResource}'");
            failure.MergeErrors(loadResult);
            return failure;
        }

        return Result<IDocumentView>.Ok(documentView);
    }

    /// <summary>
    /// Returns the document view type for the specified file resource.
    /// </summary>
    public DocumentViewType GetDocumentViewType(ResourceKey fileResource)
    {
        var extension = Path.GetExtension(fileResource).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            return DocumentViewType.UnsupportedFormat;
        }

        return _fileTypeHelper.GetDocumentViewType(extension);
    }

    public string GetDocumentLanguage(ResourceKey fileResource)
    {
        var extension = Path.GetExtension(fileResource).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            return string.Empty;
        }

        return _fileTypeHelper.GetTextEditorLanguage(extension);
    }

    public async Task<Result> OpenDocument(ResourceKey fileResource)
    {
        var resourceRegistry = _workspaceWrapper.WorkspaceService.ExplorerService.ResourceRegistry;

        var filePath = resourceRegistry.GetResourcePath(fileResource);
        if (string.IsNullOrEmpty(filePath) ||
            !File.Exists(filePath))
        {
            return Result.Fail($"File path does not exist: '{filePath}'");
        }

        var openResult = await DocumentsPanel.OpenDocument(fileResource, filePath);
        if (openResult.IsFailure)
        {
            var failure = Result.Fail($"Failed to open document for file resource '{fileResource}'");
            failure.MergeErrors(openResult);
            return failure;
        }

        _logger.LogTrace($"Opened document for file resource '{fileResource}'");

        return Result.Ok();
    }

    public async Task<Result> CloseDocument(ResourceKey fileResource, bool forceClose)
    {
        var closeResult = await DocumentsPanel.CloseDocument(fileResource, forceClose);
        if (closeResult.IsFailure)
        {
            var failure = Result.Fail($"Failed to close document for file resource '{fileResource}'");
            failure.MergeErrors(closeResult);
            return failure;
        }

        _logger.LogTrace($"Closed document for file resource '{fileResource}'");

        return Result.Ok();
    }

    public Result SelectDocument(ResourceKey fileResource)
    {
        var selectResult = DocumentsPanel.SelectDocument(fileResource);
        if (selectResult.IsFailure)
        {
            var failure = Result.Fail($"Failed to select opened document for file resource '{fileResource}'");
            failure.MergeErrors(selectResult);
            return failure;
        }

        _logger.LogTrace($"Selected document for file resource '{fileResource}'");

        return Result.Ok();
    }

    public async Task<Result> SaveModifiedDocuments(double deltaTime)
    {
        var saveResult = await DocumentsPanel.SaveModifiedDocuments(deltaTime);
        if (saveResult.IsFailure)
        {
            var failure = Result.Fail("Failed to save modified documents");
            failure.MergeErrors(saveResult);
            return failure;
        }

        return Result.Ok();
    }

    public async Task StoreOpenDocuments()
    {
        var workspaceSettings = _workspaceWrapper.WorkspaceService.WorkspaceSettings;
        Guard.IsNotNull(workspaceSettings);

        List<string> documents = [];
        foreach (var fileResource in OpenDocuments)
        {
            documents.Add(fileResource.ToString());
        }

        await workspaceSettings.SetPropertyAsync(PreviousOpenDocumentsKey, documents);
    }

    public async Task StoreSelectedDocument()
    {
        var workspaceSettings = _workspaceWrapper.WorkspaceService.WorkspaceSettings;
        Guard.IsNotNull(workspaceSettings);

        var fileResource = SelectedDocument.ToString();

        await workspaceSettings.SetPropertyAsync(PreviousSelectedDocumentKey, fileResource);
    }

    public async Task RestorePanelState()
    {
        var workspaceSettings = _workspaceWrapper.WorkspaceService.WorkspaceSettings;
        Guard.IsNotNull(workspaceSettings);

        var openDocuments = await workspaceSettings.GetPropertyAsync<List<string>>(PreviousOpenDocumentsKey);
        if (openDocuments is null ||
            openDocuments.Count == 0)
        {
            return;
        }

        var resourceRegistry = _workspaceWrapper.WorkspaceService.ExplorerService.ResourceRegistry;

        foreach (var resourceKey in openDocuments)
        {
            if (!ResourceKey.IsValidKey(resourceKey))
            {
                // An invalid resource key was saved in the settings somehow.
                _logger.LogWarning($"Invalid resource key '{resourceKey}' found in previously open documents");
                continue;
            }

            var fileResource = new ResourceKey(resourceKey);
            var getResourceResult = resourceRegistry.GetResource(fileResource);
            if (getResourceResult.IsFailure)
            {
                // This resource doesn't exist now so we can't open it again.
                _logger.LogWarning(getResourceResult, $"Failed to open document because '{fileResource}' resource does not exist.");
                continue;
            }

            // Execute a command to load the document
            // Use ExecuteNow() to ensure the command is executed while the workspace is still loading.
            var openResult = await _commandService.ExecuteNow<IOpenDocumentCommand>(command =>
            {
                command.FileResource = fileResource;
            });

            if (openResult.IsFailure)
            {
                _logger.LogWarning(openResult, $"Failed to open previously open document '{fileResource}'");
            }
        }

        var selectedDocument = await workspaceSettings.GetPropertyAsync<string>(PreviousSelectedDocumentKey);
        if (string.IsNullOrEmpty(selectedDocument))
        {
            return;
        }

        if (!ResourceKey.IsValidKey(selectedDocument))
        {
            _logger.LogWarning($"Invalid resource key '{selectedDocument}' found for previously selected document");
            return;
        }

        // Execute a command to select the previously selected document
        // Use ExecuteNow() to ensure the command is executed while the workspace is still loading.
        var selectResult = await _commandService.ExecuteNow<ISelectDocumentCommand>(command =>
        {
            command.FileResource = new ResourceKey(selectedDocument);
        });

        if (selectResult.IsFailure)
        {
            _logger.LogWarning($"Failed to select previously selected document '{selectedDocument}'");
        }
    }

    private Result<IDocumentView> CreateDocumentViewInternal(ResourceKey fileResource)
    {
        var viewType = GetDocumentViewType(fileResource);

        IDocumentView? documentView = null;
        switch (viewType)
        {
            case DocumentViewType.UnsupportedFormat:
                return Result<IDocumentView>.Fail($"File resource is not a supported document format: '{fileResource}'");

#if WINDOWS
            case DocumentViewType.TextDocument:
                documentView = _serviceProvider.GetRequiredService<TextEditorDocumentView>();
                break;

            case DocumentViewType.WebPageDocument:
                documentView = _serviceProvider.GetRequiredService<WebPageDocumentView>();
                break;

            case DocumentViewType.FileViewer:
                documentView = _serviceProvider.GetRequiredService<FileViewerDocumentView>();
                break;
#else
            case DocumentViewType.TextDocument:
            case DocumentViewType.WebPageDocument:
            case DocumentViewType.FileViewer:

                // On non-Windows platforms, use the text editor document view for all document types
                documentView = _serviceProvider.GetRequiredService<TextBoxDocumentView>();
                break;
#endif

        }

        if (documentView is null)
        {
            return Result<IDocumentView>.Fail($"Failed to create document view for file: '{fileResource}'");
        }

        return Result<IDocumentView>.Ok(documentView);
    }

    private void OnDocumentResourceChangedMessage(object recipient, DocumentResourceChangedMessage message)
    {
        var oldResource = message.OldResource.ToString();
        var newResource = message.NewResource.ToString();

        var resourceRegistry = _workspaceWrapper.WorkspaceService.ExplorerService.ResourceRegistry;
        var newResourcePath = resourceRegistry.GetResourcePath(message.NewResource);

        Guard.IsTrue(File.Exists(newResourcePath));

        var oldExtension = Path.GetExtension(oldResource);
        var oldDocumentType = _fileTypeHelper.GetDocumentViewType(oldExtension);

        var newExtension = Path.GetExtension(newResource);
        var newDocumentType = _fileTypeHelper.GetDocumentViewType(newExtension);

        var changeDocumentResource = async Task () =>
        {
            var changeResult = await DocumentsPanel.ChangeDocumentResource(oldResource, oldDocumentType, newResource, newResourcePath, newDocumentType);
            if (changeResult.IsFailure)
            {
                // Log the error and close the document to get back to a consistent state
                _logger.LogError(changeResult, $"Failed to change document resource from '{oldResource}' to '{newResource}'");
                await CloseDocument(oldResource, true);
            }
        };

        _ = changeDocumentResource();
    }

    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed objects here
                _messengerService.Unregister<WorkspaceLoadedMessage>(this);
                _messengerService.Unregister<OpenDocumentsChangedMessage>(this);
                _messengerService.Unregister<SelectedDocumentChangedMessage>(this);
                _messengerService.Unregister<DocumentResourceChangedMessage>(this);
            }

            _disposed = true;
        }
    }

    ~DocumentsService()
    {
        Dispose(false);
    }
}