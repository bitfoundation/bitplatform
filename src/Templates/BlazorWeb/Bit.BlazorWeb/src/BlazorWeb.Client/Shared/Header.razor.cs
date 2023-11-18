﻿namespace BlazorWeb.Client.Shared;

public partial class Header : IDisposable
{
    private bool _disposed;
    private bool _isUserAuthenticated;

    [Parameter] public EventCallback OnToggleMenu { get; set; }

    protected override async Task OnInitAsync()
    {
        AuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        _isUserAuthenticated = await PrerenderStateService.GetValue($"{nameof(Header)}-isUserAuthenticated", async () => (await AuthenticationStateTask).User.IsAuthenticated());

        await base.OnInitAsync();
    }

    async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
    {
        try
        {
            _isUserAuthenticated = (await AuthenticationStateTask).User.IsAuthenticated();
        }
        catch (Exception ex)
        {
            ExceptionHandler.Handle(ex);
        }
        finally
        {
            StateHasChanged();
        }
    }

    private async Task ToggleMenu()
    {
        await OnToggleMenu.InvokeAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        AuthenticationStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;

        _disposed = true;
    }
}
