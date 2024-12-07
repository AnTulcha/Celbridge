using Celbridge.Entities;
using Celbridge.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Celbridge.Inspector.ViewModels;

public partial class EntityEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private Visibility _componentEditorVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _componentPickerVisibility = Visibility.Collapsed;

    public EntityEditorViewModel(IMessengerService messengerService)
    {
        messengerService.Register<SelectedComponentChangedMessage>(this, OnSelectedComponentChangedMessage);
    }

    private void OnSelectedComponentChangedMessage(object recipient, SelectedComponentChangedMessage message)
    {
        bool isComponentSelected = message.componentIndex >= 0;
        UpdateVisibility(isComponentSelected);
    }

    private void UpdateVisibility(bool isComponentSelected)
    {
        ComponentEditorVisibility = isComponentSelected ? Visibility.Visible : Visibility.Collapsed;
        ComponentPickerVisibility = isComponentSelected ? Visibility.Collapsed : Visibility.Visible;
    }
}
