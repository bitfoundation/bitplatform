using BlazorWeb.Shared.Dtos.Identity;

namespace BlazorWeb.Client.Pages.Identity;

public partial class SignInPage
{
    private bool _isLoading;
    private string? _signInMessage;
    private BitMessageBarType _signInMessageType;
    private SignInRequestDto _signInModel = new();

    [SupplyParameterFromQuery(Name = "redirect-url"), Parameter] public string? RedirectUrl { get; set; }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if ((await AuthenticationStateTask).User.IsAuthenticated())
        {
            NavigationManager.NavigateTo("/");
        }
    }

    private async Task DoSignIn()
    {
        if (_isLoading) return;

        _isLoading = true;
        _signInMessage = null;

        try
        {
            await AuthenticationManager.SignIn(_signInModel);

            NavigationManager.NavigateTo(RedirectUrl ?? "/");
        }
        catch (KnownException e)
        {
            _signInMessageType = BitMessageBarType.Error;

            _signInMessage = e.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }
}

