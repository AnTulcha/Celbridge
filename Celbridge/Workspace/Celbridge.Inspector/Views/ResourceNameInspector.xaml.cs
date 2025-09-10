using Celbridge.Inspector.ViewModels;
using Microsoft.Extensions.Localization;

namespace Celbridge.Inspector.Views;

public sealed partial class ResourceNameInspector : UserControl, IInspector
{
    private readonly IStringLocalizer _stringLocalizer;

    public ResourceNameInspectorViewModel ViewModel => (DataContext as ResourceNameInspectorViewModel)!;

    private LocalizedString OpenString => _stringLocalizer.GetString("ResourceTree_Open");
    private LocalizedString OpenInString => _stringLocalizer.GetString("ResourceTree_OpenIn");
    private LocalizedString OpenInExplorerString => _stringLocalizer.GetString("ResourceTree_OpenInExplorer");
    private LocalizedString OpenInApplicationString => _stringLocalizer.GetString("ResourceTree_OpenInApplication");

    private LocalizedString DeleteString => _stringLocalizer.GetString("ResourceTree_Delete");
    
    public ResourceKey Resource
    {
        set => ViewModel.Resource = value;
        get => ViewModel.Resource;
    }

    // Code gen requires a parameterless constructor
    public ResourceNameInspector()
    {
        throw new NotImplementedException();
    }

    public ResourceNameInspector(ResourceNameInspectorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _stringLocalizer = ServiceLocator.AcquireService<IStringLocalizer>();

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.Resource))
        {
            if (!ViewModel.Resource.IsEmpty)
            {
                var fontFamily = (FontFamily)Application.Current.Resources[ViewModel.Icon.FontFamily];
                ResourceIcon.FontFamily = fontFamily;

                ToolTipService.SetPlacement( ResourceNameText, PlacementMode.Bottom);
                ToolTipService.SetToolTip(ResourceNameText, ViewModel.Resource);
            }
        }
    }
}

