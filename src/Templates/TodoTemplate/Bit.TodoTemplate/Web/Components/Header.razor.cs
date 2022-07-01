namespace TodoTemplate.App.Components;

public partial class Header : IAsyncDisposable
{
    [Parameter] public EventCallback OnToggleMenu { get; set; }

    [AutoInject] private IStateService stateService = default!;

    [AutoInject] private TodoTemplateAuthenticationStateProvider todoTemplateAuthenticationStateProvider = default!;

    [AutoInject] private IExceptionHandler exceptionHandler = default!;

    public bool IsUserAuthenticated { get; set; }

    protected async override Task OnInitAsync()
    {
        todoTemplateAuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        IsUserAuthenticated = await stateService.GetValue($"{nameof(Header)}-{nameof(IsUserAuthenticated)}", async () => await todoTemplateAuthenticationStateProvider.IsUserAuthenticated());

        await base.OnInitAsync();
    }

    async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
    {
        try
        {
            IsUserAuthenticated = await todoTemplateAuthenticationStateProvider.IsUserAuthenticated();
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
        todoTemplateAuthenticationStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;
    }
}
