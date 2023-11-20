using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Pages.Identity;

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
            var result = await (await HttpClient.PostAsJsonAsync("Identity/SignIn", _signInModel, AppJsonContext.Default.SignInRequestDto))
                .Content.ReadFromJsonAsync(AppJsonContext.Default.TokenResponseDto);

            await JSRuntime.StoreToken(result!, _signInModel.RememberMe);

            await AuthenticationStateProvider.RaiseAuthenticationStateHasChanged();

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

