namespace TodoTemplate.App.Components;

public partial class Header : IAsyncDisposable
{
    [Parameter] public EventCallback OnToggleMenu { get; set; }

    public bool IsUserAuthenticated { get; set; }

    protected override async Task OnInitAsync()
    {
        _authenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        IsUserAuthenticated = await _stateService.GetValue($"{nameof(Header)}-{nameof(IsUserAuthenticated)}", _authenticationStateProvider.IsUserAuthenticated);

        await base.OnInitAsync();
    }

    async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
    {
        try
        {
            IsUserAuthenticated = await _authenticationStateProvider.IsUserAuthenticated();
        }
        catch (Exception ex)
        {
            _exceptionHandler.Handle(ex);
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
        _authenticationStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;
    }
}
