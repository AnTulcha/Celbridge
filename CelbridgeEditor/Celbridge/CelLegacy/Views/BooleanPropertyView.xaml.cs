﻿namespace CelLegacy.Views;

public partial class BooleanPropertyView : UserControl, IPropertyView
{
    public BooleanPropertyViewModel ViewModel { get; }

    public BooleanPropertyView()
    {
        this.InitializeComponent();

        var services = LegacyServiceProvider.Services!;
        ViewModel = services.GetRequiredService<BooleanPropertyViewModel>();
    }

    public void SetProperty(Property property, string labelText)
    {
        ViewModel.SetProperty(property, labelText);
    }

    public int ItemIndex
    {
        get => ViewModel.ItemIndex;
        set => ViewModel.ItemIndex = value;
    }

    public Result CreateChildViews()
    {
        return new SuccessResult();
    }
}