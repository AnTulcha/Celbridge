﻿using Celbridge.Documents.Commands;
using Celbridge.Documents.Services;
using Celbridge.Documents.ViewModels;
using Celbridge.Documents.Views;
using Celbridge.Extensions;

namespace Celbridge.Documents;

public static class ServiceConfiguration
{
    public static void ConfigureServices(IExtensionServiceCollection config)
    {
        //
        // Register services
        //

        config.AddTransient<IDocumentsService, DocumentsService>();
        config.AddTransient<FileTypeHelper>();

        //
        // Register Views
        //

        config.AddTransient<DocumentsPanel>();
        config.AddTransient<TextBoxDocumentView>();
        config.AddTransient<WebPageDocumentView>();
        config.AddTransient<TextEditorDocumentView>();
        config.AddTransient<FileViewerDocumentView>();

        //
        // Register ViewModels
        //

        config.AddTransient<DocumentsPanelViewModel>();
        config.AddTransient<DocumentTabViewModel>();
        config.AddTransient<DefaultDocumentViewModel>();
        config.AddTransient<WebPageDocumentViewModel>();
        config.AddTransient<TextEditorDocumentViewModel>();
        config.AddTransient<FileViewerDocumentViewModel>();

        //
        // Register commands
        //

        config.AddTransient<IOpenDocumentCommand, OpenDocumentCommand>();
        config.AddTransient<ICloseDocumentCommand, CloseDocumentCommand>();
        config.AddTransient<ISelectDocumentCommand, SelectDocumentCommand>();
    }
}