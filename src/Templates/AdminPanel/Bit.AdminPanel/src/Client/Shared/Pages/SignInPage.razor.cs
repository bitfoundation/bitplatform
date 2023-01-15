using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.Client.Shared.Pages;

public partial class SignInPage
{
    public bool _isLoading;
    public string? _signInMessage;
    public BitMessageBarType _signInMessageType;
    public SignInRequestDto _signInModel = new();

    [Parameter]
    [SupplyParameterFromQuery]
    public string? RedirectUrl { get; set; }

    protected async override Task OnAfterFirstRenderAsync()
    {
        if (await AuthenticationStateProvider.IsUserAuthenticatedAsync())
        {
            NavigationManager.NavigateTo("/");
        }

        await base.OnAfterFirstRenderAsync();
    }
    private async Task DoSignIn()
    {
        if (_isLoading) return;

        _isLoading = true;
        _signInMessage = null;

        try
        {
            await AuthenticationService.SignIn(_signInModel);

            NavigationManager.NavigateTo(RedirectUrl ?? "/");
        }
        catch (KnownException e)
        {
            _signInMessage = e.Message;
            _signInMessageType = BitMessageBarType.Error;
        }
        finally
        {
            _isLoading = false;
        }
    }
}

