﻿//-:cnd:noEmit

namespace Boilerplate.Client.Core.Shared;

/// <summary>
/// https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors
/// </summary>
public partial class AppErrorBoundary
{
    private bool showException;

    [AutoInject] private IExceptionHandler exceptionHandler = default!;

    [AutoInject] private NavigationManager navigationManager = default!;

    protected override void OnInitialized()
    {
        showException = BuildConfigurationModeDetector.Current.IsDebug();
    }

    protected override async Task OnErrorAsync(Exception exception)
    {
        exceptionHandler.Handle(exception);
    }

    private void Refresh()
    {
        navigationManager.Refresh(forceReload: true);
    }

    private void GoHome()
    {
        navigationManager.NavigateTo("/", true);
    }
}
