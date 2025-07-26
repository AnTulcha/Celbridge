using Celbridge.Dialog;
using Celbridge.Documents.ViewModels;
using Celbridge.Explorer;
using Celbridge.Logging;
using Celbridge.UserInterface;
using Celbridge.Workspace;
using Microsoft.Extensions.Localization;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;

namespace Celbridge.Documents.Views;

public sealed partial class SpreadsheetDocumentView : DocumentView
{
    private ILogger _logger;
    private IStringLocalizer _stringLocalizer;
    private IDialogService _dialogService;
    private IResourceRegistry _resourceRegistry;

    public SpreadsheetDocumentViewModel ViewModel { get; }

    private WebView2? _webView;

    public SpreadsheetDocumentView(
        IServiceProvider serviceProvider,
        ILogger<SpreadsheetDocumentView> logger,
        IStringLocalizer stringLocalizer,
        IDialogService dialogService,
        IWorkspaceWrapper workspaceWrapper)
    {
        ViewModel = serviceProvider.GetRequiredService<SpreadsheetDocumentViewModel>();

        _logger = logger;
        _stringLocalizer = stringLocalizer;
        _dialogService = dialogService;
        _resourceRegistry = workspaceWrapper.WorkspaceService.ExplorerService.ResourceRegistry;

        Loaded += SpreadsheetDocumentView_Loaded;

        //
        // Set the data context
        // 

        this.DataContext(ViewModel);
    }

    private async void SpreadsheetDocumentView_Loaded(object sender, RoutedEventArgs e)
    {
        // Unregister for UI load events.
        // Switching tabs while spreadsheet view is loading triggers a load event.
        Loaded -= SpreadsheetDocumentView_Loaded;

        await InitSpreadsheetViewAsync();
    }

    private async Task InitSpreadsheetViewAsync()
    {
        try
        {
            var webView = new WebView2();

            // This fixes a visual bug where the WebView2 control would show a white background briefly when
            // switching between tabs. Similar issue described here: https://github.com/MicrosoftEdge/WebView2Feedback/issues/1412
            webView.DefaultBackgroundColor = Colors.Transparent;

            await webView.EnsureCoreWebView2Async();

            webView.CoreWebView2.Settings.IsWebMessageEnabled = true;

            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.isWebView = true;");

            // Todo: Download and embed spreadJS libs when making an installer build to allow full offline usage.
            // webView.CoreWebView2.SetVirtualHostNameToFolderMapping("spreadjs", "spreadJS", CoreWebView2HostResourceAccessKind.Allow);
            webView.CoreWebView2.Navigate("https://celbridge-app-static-files.celbridge.org/libs/spreadjs/spreadjs-18.1.4/");

            bool isEditorReady = false;
            TypedEventHandler<WebView2, CoreWebView2WebMessageReceivedEventArgs> onWebMessageReceived = (sender, e) =>
            {
                var message = e.TryGetWebMessageAsString();

                if (message == "editor_ready")
                {
                    isEditorReady = true;
                    return;
                }

                throw new InvalidOperationException($"Expected 'editor_ready' message, but received: {message}");
            };

            webView.WebMessageReceived += onWebMessageReceived;

            while (!isEditorReady)
            {
                await Task.Delay(50);
            }

            webView.WebMessageReceived -= onWebMessageReceived;

            // Fixes a visual bug where the WebView2 control would show a white background briefly when
            // switching between tabs. Similar issue described here: https://github.com/MicrosoftEdge/WebView2Feedback/issues/1412
            webView.DefaultBackgroundColor = Colors.Transparent;

            _webView = webView;

            var filePath = ViewModel.FilePath;
            await LoadSpreadsheet(filePath);

            this.Content = _webView;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Spreadsheet Web View.");
        }
    }

    public override async Task<Result> SetFileResource(ResourceKey fileResource)
    {
        var filePath = _resourceRegistry.GetResourcePath(fileResource);

        if (_resourceRegistry.GetResource(fileResource).IsFailure)
        {
            return Result.Fail($"File resource does not exist in resource registry: {fileResource}");
        }

        if (!File.Exists(filePath))
        {
            return Result.Fail($"File resource does not exist on disk: {fileResource}");
        }

        ViewModel.FileResource = fileResource;
        ViewModel.FilePath = filePath;

        await Task.CompletedTask;

        return Result.Ok();
    }

    public override async Task<Result> LoadContent()
    {
        return await ViewModel.LoadContent();
    }

    private async Task LoadSpreadsheet(string filePath)
    {
        Guard.IsNotNull(_webView);

        if (File.Exists(filePath))
        {
            byte[] bytes = await File.ReadAllBytesAsync(filePath);
            string base64 = Convert.ToBase64String(bytes);

            _webView.CoreWebView2.PostWebMessageAsString(base64);
        }

        _webView.WebMessageReceived -= WebView_WebMessageReceived;
        _webView.WebMessageReceived += WebView_WebMessageReceived;
    }

    private async void WebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        // Read the base 64 encoded spreadsheet data
        var spreadsheetData = args.TryGetWebMessageAsString();
        if (string.IsNullOrEmpty(spreadsheetData))
        {
            _logger.LogError("Failed to acquire spreadsheet data");
            return;
        }

        await SaveSpreadsheet(spreadsheetData);
    }

    private async Task SaveSpreadsheet(string spreadsheetData)
    {
        bool succeeded = false;
        try
        {
            byte[] fileBytes = Convert.FromBase64String(spreadsheetData);
            var filePath = ViewModel.FilePath;

            File.WriteAllBytes(filePath, fileBytes);
            succeeded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error saving Excel file: " + ex.Message);
        }

        if (!succeeded)
        {
            // Alert the user that the document failed to save
            var file = ViewModel.FilePath;
            var title = _stringLocalizer.GetString("Documents_SaveDocumentFailedTitle");
            var message = _stringLocalizer.GetString("Documents_SaveDocumentFailedGeneric", file);
            await _dialogService.ShowAlertDialogAsync(title, message);
        }
    }
}
