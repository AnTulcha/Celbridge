using Celbridge.Dialog;
using Windows.System;

namespace Celbridge.UserInterface.ViewModels;

public sealed partial class InputTextDialog : ContentDialog, IInputTextDialog
{
    private readonly IStringLocalizer _stringLocalizer;

    public InputTextDialogViewModel ViewModel { get; }

    public string TitleText
    {
        get => ViewModel.TitleText;
        set => ViewModel.TitleText = value;
    }

    public string HeaderText
    {
        get => ViewModel.HeaderText;
        set => ViewModel.HeaderText = value;
    }

    private LocalizedString OkString => _stringLocalizer.GetString($"DialogButton_Ok");
    private LocalizedString CancelSting => _stringLocalizer.GetString($"DialogButton_Cancel");

    private TextBox _inputTextbox;
    private bool _pressedEnter;

    public InputTextDialog()
    {
        _stringLocalizer = ServiceLocator.AcquireService<IStringLocalizer>();

        var userInterfaceService = ServiceLocator.AcquireService<IUserInterfaceService>();
        XamlRoot = userInterfaceService.XamlRoot as XamlRoot;

        ViewModel = ServiceLocator.AcquireService<InputTextDialogViewModel>();

        _inputTextbox = new TextBox()
            .Header
            (
                x => x.Binding(() => ViewModel.HeaderText)
                      .Mode(BindingMode.OneWay)
            )
            .Text
            (
                x => x.Binding(() => ViewModel.InputText)
                      .Mode(BindingMode.TwoWay)
                      .UpdateSourceTrigger(UpdateSourceTrigger.PropertyChanged)
            )
            .IsSpellCheckEnabled(false)
            .AcceptsReturn(false);

        _inputTextbox.KeyDown += InputTextbox_KeyDown;

        this.DataContext
        (
            ViewModel, (dialog, vm) => dialog
            .Title(x => x.Binding(() => ViewModel.TitleText).Mode(BindingMode.OneWay))
            .PrimaryButtonText(OkString)
            .SecondaryButtonText(CancelSting)
            .IsPrimaryButtonEnabled(x => x.Binding(() => ViewModel.IsSubmitEnabled).Mode(BindingMode.OneWay))
            .Content
            (
                new StackPanel()
                    .Orientation(Orientation.Vertical)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Children
                    (
                        _inputTextbox,
                        new TextBlock()
                            .Text
                            (
                                x => x.Binding(() => ViewModel.ErrorText)
                                      .Mode(BindingMode.OneWay)
                            )
                            .Foreground(ThemeResource.Get<Brush>("ErrorTextBrush"))
                            .Margin(6, 4, 0, 0)
                            .Opacity
                            (
                                x => x.Binding(() => ViewModel.IsTextValid)
                                      .Mode(BindingMode.OneWay)
                                      .Convert((valid) => valid ? 0 : 1)
                            )
                    )
            )
        );
    }

    private void InputTextbox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            // Set a flag so that we can tell that the user pressed Enter
            _pressedEnter = true;
            Hide();
        }
        else if (e.Key == VirtualKey.Escape)
        {
            Hide();
        }
    }

    public async Task<Result<string>> ShowDialogAsync()
    {
        var contentDialogResult = await ShowAsync();
        if (contentDialogResult == ContentDialogResult.Primary || _pressedEnter)
        {
            return Result<string>.Ok(ViewModel.InputText);
        }

        return Result<string>.Fail("Failed to input text");
    }

    public void SetDefaultText(string defaultText, Range selectionRange)
    {
        ViewModel.InputText = defaultText;
        _inputTextbox.Text = defaultText;

        // Todo: This appears to have no effect on Skia.Gtk platform

        var (offset, length) = selectionRange.GetOffsetAndLength(defaultText.Length);
        _inputTextbox.Select(offset, length);
    }
}




