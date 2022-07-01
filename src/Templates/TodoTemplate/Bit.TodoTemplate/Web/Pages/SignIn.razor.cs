using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class SignIn
{
    public SignInRequestDto SignInModel { get; set; } = new();

    public bool IsLoading { get; set; }

    public BitMessageBarType SignInMessageType { get; set; }

    public string? SignInMessage { get; set; }

    [AutoInject] private NavigationManager navigationManager = default!;

    [AutoInject] private ITodoTemplateAuthenticationService todoTemplateAuthenticationService = default!;

    [AutoInject] private TodoTemplateAuthenticationStateProvider todoTemplateAuthenticationStateProvider = default!;

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
            await todoTemplateAuthenticationService.SignIn(SignInModel);

            navigationManager.NavigateTo(RedirectUrl ?? "/");
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
            if (await todoTemplateAuthenticationStateProvider.IsUserAuthenticated())
                navigationManager.NavigateTo("/");
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}

