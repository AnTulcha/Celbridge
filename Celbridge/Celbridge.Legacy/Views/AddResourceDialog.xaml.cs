﻿using Celbridge.Legacy.ViewModels;

namespace Celbridge.Legacy.Views;

public sealed partial class AddResourceDialog : ContentDialog
{
    public AddResourceViewModel ViewModel { get; set; }

    public AddResourceDialog()
    {
        this.InitializeComponent();
        ViewModel = LegacyServiceProvider.Services!.GetRequiredService<AddResourceViewModel>();
    }
}