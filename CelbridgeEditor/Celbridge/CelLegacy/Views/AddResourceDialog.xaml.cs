﻿using CelLegacy.ViewModels;

namespace CelLegacy.Views;

public sealed partial class AddResourceDialog : ContentDialog
{
    public AddResourceViewModel ViewModel { get; set; }

    public AddResourceDialog()
    {
        this.InitializeComponent();
        ViewModel = LegacyServiceProvider.Services!.GetRequiredService<AddResourceViewModel>();
    }
}