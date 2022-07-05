namespace AdminPanelTemplate.App.Components;

public partial class Header : IAsyncDisposable
{
    [AutoInject] private IStateService stateService = default!;

    [AutoInject] private AppAuthenticationStateProvider authStateProvider = default!;

    [AutoInject] private IExceptionHandler exceptionHandler = default!;

    [Parameter] public EventCallback OnToggleMenu { get; set; }

    public bool IsUserAuthenticated { get; set; }

    protected async override Task OnInitAsync()
    {
        authStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        IsUserAuthenticated = await stateService.GetValue($"{nameof(Header)}-{nameof(IsUserAuthenticated)}", authStateProvider.IsUserAuthenticated);

        await base.OnInitAsync();
    }

    async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
    {
        try
        {
            IsUserAuthenticated = await authStateProvider.IsUserAuthenticated();
        }
        catch (Exception ex)
        {
            exceptionHandler.Handle(ex);
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

    public async ValueTask DisposeAsync()
    {
        authStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;
    }
}
