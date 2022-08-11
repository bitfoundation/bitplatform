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
            await AuthenticationService.SignIn(SignInModel);

            NavigationManager.NavigateTo(RedirectUrl ?? "/");
        }
        catch (KnownException e)
        {
            SignInMessageType = BitMessageBarType.Error;

            SignInMessage = e.Message;
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
            if (await AuthenticationStateProvider.IsUserAuthenticated())
                NavigationManager.NavigateTo("/");
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}

