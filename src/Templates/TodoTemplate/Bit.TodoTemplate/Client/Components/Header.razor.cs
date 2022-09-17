namespace TodoTemplate.Client.Components;

public partial class Header : IAsyncDisposable
{
    [Parameter] public EventCallback OnToggleMenu { get; set; }

    public bool IsUserAuthenticated { get; set; }

    protected override async Task OnInitAsync()
    {
        AuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        IsUserAuthenticated = await StateService.GetValue($"{nameof(Header)}-{nameof(IsUserAuthenticated)}", AuthenticationStateProvider.IsUserAuthenticated);

        await base.OnInitAsync();
    }

    async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
    {
        try
        {
            IsUserAuthenticated = await AuthenticationStateProvider.IsUserAuthenticated();
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

    public async ValueTask DisposeAsync()
    {
        AuthenticationStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;
    }
}
