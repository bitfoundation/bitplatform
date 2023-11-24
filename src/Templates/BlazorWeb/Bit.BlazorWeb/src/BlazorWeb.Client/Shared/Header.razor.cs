﻿namespace BlazorWeb.Client.Shared;

public partial class Header : IDisposable
{
    private bool disposed;
    private bool isUserAuthenticated;

    [Parameter] public EventCallback OnToggleMenu { get; set; }

    protected override async Task OnInitAsync()
    {
        AuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        isUserAuthenticated = await PrerenderStateService.GetValue($"{nameof(Header)}-isUserAuthenticated", async () => (await AuthenticationStateTask).User.IsAuthenticated());

        await base.OnInitAsync();
    }

    async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
    {
        try
        {
            isUserAuthenticated = (await task).User.IsAuthenticated();
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
        if (disposed) return;

        AuthenticationStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;

        disposed = true;
    }
}
