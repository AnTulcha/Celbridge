using Celbridge.Documents.ViewModels;
using Celbridge.Explorer;
using Celbridge.Logging;
using Celbridge.Workspace;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using Windows.Foundation;

namespace Celbridge.Documents.Views;

public sealed partial class SpreadsheetDocumentView : DocumentView
{
    private IResourceRegistry _resourceRegistry;

    public SpreadsheetDocumentViewModel ViewModel { get; }

    private WebView2? _webView;

    public SpreadsheetDocumentView(
        IServiceProvider serviceProvider,
        ILogger<SpreadsheetDocumentView> logger,
        IWorkspaceWrapper workspaceWrapper)
    {
        ViewModel = serviceProvider.GetRequiredService<SpreadsheetDocumentViewModel>();

        _resourceRegistry = workspaceWrapper.WorkspaceService.ExplorerService.ResourceRegistry;

        Loaded += async (s, e) =>
        {
            var createResult = await CreateSpreadsheetWebView();

            if (createResult.IsFailure)
            {
                logger.LogError(createResult.Error);
                return;
            }
            _webView = createResult.Value;

            // Fixes a visual bug where the WebView2 control would show a white background briefly when
            // switching between tabs. Similar issue described here: https://github.com/MicrosoftEdge/WebView2Feedback/issues/1412
            _webView.DefaultBackgroundColor = Colors.Transparent;

            var filePath = ViewModel.FilePath;

            await LoadSpreadsheet(filePath);

            this.Content = _webView;
        };

        //
        // Set the data context
        // 

        this.DataContext(ViewModel);
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

    private static async Task<Result<WebView2>> CreateSpreadsheetWebView()
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

            return Result<WebView2>.Ok(webView);
        }
        catch (Exception ex) 
        {
            return Result<WebView2>.Fail("Failed to create Spreadsheet Web View")
                .WithException(ex);
        }
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

    private void WebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            var base64 = args.TryGetWebMessageAsString();

            if (string.IsNullOrEmpty(base64))
            {
                // Todo: Log error
                return;
            }

            byte[] fileBytes = Convert.FromBase64String(base64);
            var filePath = ViewModel.FilePath;

            File.WriteAllBytes(filePath, fileBytes);

            Debug.WriteLine("Excel file saved to: " + filePath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error saving Excel file: " + ex.Message);
        }
    }
}
