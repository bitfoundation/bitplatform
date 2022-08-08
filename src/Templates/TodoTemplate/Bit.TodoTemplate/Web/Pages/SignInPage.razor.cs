using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class SignInPage
{
    public SignInRequestDto SignInModel { get; set; } = new();

    public bool IsLoading { get; set; }

    public BitMessageBarType SignInMessageType { get; set; }

    public string? SignInMessage { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? RedirectUrl { get; set; }

    private async Task DoSignIn()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        SignInMessage = null;

        try
        {
            await _authenticationService.SignIn(SignInModel);

            _navigationManager.NavigateTo(RedirectUrl ?? "/");
        }
        catch (KnownException e)
        {
            SignInMessageType = BitMessageBarType.Error;

            SignInMessage = ErrorStrings.ResourceManager.Translate(e.Message, SignInModel.UserName!);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool IsSubmitButtonEnabled =>
        string.IsNullOrWhiteSpace(SignInModel.UserName) is false &&
        string.IsNullOrWhiteSpace(SignInModel.Password) is false && 
        IsLoading is false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (await _authenticationStateProvider.IsUserAuthenticated())
                _navigationManager.NavigateTo("/");
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}

