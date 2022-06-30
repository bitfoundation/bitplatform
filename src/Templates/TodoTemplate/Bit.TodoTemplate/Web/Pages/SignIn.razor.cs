using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class SignIn
{
    public SignInRequestDto SignInModel { get; set; } = new();

    public bool IsLoading { get; set; }

    public BitMessageBarType SignInMessageType { get; set; }

    public string? SignInMessage { get; set; }

    [AutoInject] private NavigationManager NavigationManager = default!;

    [AutoInject] private ITodoTemplateAuthenticationService TodoTemplateAuthenticationService = default!;

    [AutoInject] private TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider = default!;

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
            await TodoTemplateAuthenticationService.SignIn(SignInModel);

            NavigationManager.NavigateTo(RedirectUrl ?? "/");
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
        SignInModel.UserName.HasValue()
        && SignInModel.Password.HasValue()
        && IsLoading is false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated())
                NavigationManager.NavigateTo("/");
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}

