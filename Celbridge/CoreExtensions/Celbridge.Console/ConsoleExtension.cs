﻿using Celbridge.BaseLibrary.Console;
using Celbridge.BaseLibrary.Extensions;
using Celbridge.CoreExtensions.Console;

namespace Celbridge.Console;

public class ConsoleExtension : IExtension
{
    public void ConfigureServices(IServiceConfiguration config)
    {
        config.AddSingleton<IConsoleService, ConsoleService>();
    }

    public void Initialize()
    {}
}