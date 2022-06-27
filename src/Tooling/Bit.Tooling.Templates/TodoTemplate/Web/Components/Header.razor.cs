
namespace TodoTemplate.App.Components;

public partial class Header : IAsyncDisposable
{
    [Parameter] public EventCallback OnToggleMenu { get; set; }

    [AutoInject]
    public IStateService StateService { get; set; } = default!;

    [AutoInject]
    public TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider { get; set; } = default!;

    [AutoInject]
    public IExceptionHandler ExceptionHandler { get; set; } = default!;

    public bool IsUserAuthenticated { get; set; }

    protected async override Task OnInitAsync()
    {
        TodoTemplateAuthenticationStateProvider.AuthenticationStateChanged += VerifyUserIsAuthenticatedOrNot;

        IsUserAuthenticated = await StateService.GetValue($"{nameof(Header)}-{nameof(IsUserAuthenticated)}", async () => await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated());

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
        TodoTemplateAuthenticationStateProvider.AuthenticationStateChanged -= VerifyUserIsAuthenticatedOrNot;
    }
}
