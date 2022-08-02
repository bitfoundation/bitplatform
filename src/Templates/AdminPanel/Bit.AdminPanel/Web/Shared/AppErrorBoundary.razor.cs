﻿//-:cnd:noEmit
namespace AdminPanel.App.Shared;

public partial class AppErrorBoundary
{
    [AutoInject] private IExceptionHandler exceptionHandler = default!;

    [AutoInject] private NavigationManager navigationManager = default!;

    private bool ShowException { get; set; }

#if DEBUG
    protected override void OnInitialized()
    {
        ShowException = true;
    }
#endif

    protected override async Task OnErrorAsync(Exception exception)
    {
        exceptionHandler.Handle(exception);
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
