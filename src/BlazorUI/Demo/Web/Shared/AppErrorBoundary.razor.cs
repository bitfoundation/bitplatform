﻿using System;
using System.Threading.Tasks;
using Bit.BlazorUI.Demo.Web.Services.Contracts;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI.Demo.Web.Shared;

public partial class AppErrorBoundary
{
    [Inject] private IExceptionHandler exceptionHandler { get; set; } = default!;

    [Inject] private NavigationManager navigationManager { get; set; } = default!;

    private bool ShowException { get; set; }

#if DEBUG
    protected override void OnInitialized()
    {
        ShowException = true;
    }
#endif

    protected override Task OnErrorAsync(Exception exception)
    {
        exceptionHandler.Handle(exception);

        return Task.CompletedTask;
    }

    private void Refresh()
    {
        navigationManager.NavigateTo(navigationManager.Uri, true);
    }

    private void GoHome()
    {
        navigationManager.NavigateTo("/", true);
    }
}
