namespace TodoTemplate.App.Components;

public partial class Header : IAsyncDisposable
{
    public bool IsUserAuthenticated { get; set; }

    [Inject]
    public IStateService StateService { get; set; } = default!;

    [Inject]
    public TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider { get; set; } = default!;

    [Inject]
    public IExceptionHandler ExceptionHandler { get; set; } = default!;

    protected async override Task OnInitAsync()
    {
        TodoTemplateAuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        IsUserAuthenticated = await StateService.GetValue(nameof(IsUserAuthenticated), async () => await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated());

        await base.OnInitAsync();
    }

    async void VerifyUserIsAuthenticatedOrNot(Task<AuthenticationState> task)
    {
        try
        {
            IsUserAuthenticated = await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated();
        }
        catch (Exception ex)
        {
            ExceptionHandler.OnExceptionReceived(ex);
        }
        finally
        {
            StateHasChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        TodoTemplateAuthenticationStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;
    }
}
