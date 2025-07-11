using Celbridge.Documents.ViewModels;
using Celbridge.Explorer;
using Celbridge.UserInterface;
using Celbridge.Workspace;
using Microsoft.Web.WebView2.Core;

namespace Celbridge.Documents.Views;

public sealed partial class SpreadsheetDocumentView : DocumentView
{
    private IResourceRegistry _resourceRegistry;

    public SpreadsheetDocumentViewModel ViewModel { get; }

    private WebView2 _webView;

    public SpreadsheetDocumentView(
        IServiceProvider serviceProvider,
        IWorkspaceWrapper workspaceWrapper)
    {
        ViewModel = serviceProvider.GetRequiredService<SpreadsheetDocumentViewModel>();

        _resourceRegistry = workspaceWrapper.WorkspaceService.ExplorerService.ResourceRegistry;

        Loaded += async (s, e) =>
        {
            _webView = await CreateUniverWebView();

            //_webView = new WebView2()
            //    .Source(x => x.Binding(() => ViewModel.Source));

            // Fixes a visual bug where the WebView2 control would show a white background briefly when
            // switching between tabs. Similar issue described here: https://github.com/MicrosoftEdge/WebView2Feedback/issues/1412

            _webView.DefaultBackgroundColor = Colors.Transparent;

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

    private static async Task<WebView2> CreateUniverWebView()
    {
        var webView = new WebView2();

        // This fixes a visual bug where the WebView2 control would show a white background briefly when
        // switching between tabs. Similar issue described here: https://github.com/MicrosoftEdge/WebView2Feedback/issues/1412
        webView.DefaultBackgroundColor = Colors.Transparent;

        await webView.EnsureCoreWebView2Async();
        webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "Univer",
            "univer",
            CoreWebView2HostResourceAccessKind.Allow);

        webView.CoreWebView2.Settings.AreHostObjectsAllowed = true;
        webView.CoreWebView2.Settings.IsWebMessageEnabled = true;

        await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("window.isWebView = true;");

        // Set Monaco color theme to match the user interface theme        
        var userInterfaceService = ServiceLocator.AcquireService<IUserInterfaceService>();
        var theme = userInterfaceService.UserInterfaceTheme;
        var vsTheme = theme == UserInterfaceTheme.Light ? "vs-light" : "vs-dark";
        await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync($"window.theme = '{vsTheme}';");

        webView.CoreWebView2.Navigate("http://Univer/index.html");

        return webView;
    }
}
