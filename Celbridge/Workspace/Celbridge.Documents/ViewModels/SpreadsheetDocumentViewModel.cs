using CommunityToolkit.Mvvm.ComponentModel;

namespace Celbridge.Documents.ViewModels;

public partial class SpreadsheetDocumentViewModel : DocumentViewModel
{
    [ObservableProperty]
    private string _source = string.Empty;
    
    public async Task<Result> LoadContent()
    {
        try
        {
            var fileUri = new Uri(FilePath);
            Source = fileUri.ToString();
            await Task.CompletedTask;
 
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"An exception occured when loading document from file: {FilePath}")
                .WithException(ex);
        }
    }
}
