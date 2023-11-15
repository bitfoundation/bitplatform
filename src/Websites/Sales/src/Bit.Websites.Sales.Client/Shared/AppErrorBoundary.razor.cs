﻿
namespace Bit.Websites.Sales.Client.Shared;

/// <summary>
/// https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors
/// </summary>
public partial class AppErrorBoundary
{
    private bool _showException;

    [AutoInject] private IExceptionHandler _exceptionHandler = default!;

    [AutoInject] private NavigationManager _navigationManager = default!;

#if DEBUG
    protected override void OnInitialized()
    {
        _showException = true;
    }
#endif

    protected override async Task OnErrorAsync(Exception exception)
    {
        _exceptionHandler.Handle(exception);
    }

    private void Refresh()
    {
        _navigationManager.Refresh(forceReload: true);
    }

    private void GoHome()
    {
        _navigationManager.NavigateTo("/", true);
    }
}
