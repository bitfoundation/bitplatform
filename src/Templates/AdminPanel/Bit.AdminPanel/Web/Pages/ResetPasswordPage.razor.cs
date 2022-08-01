using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.App.Pages;

public partial class ResetPasswordPage
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private NavigationManager navigationManager = default!;

    [AutoInject] private IAuthenticationService authService = default!;

    [AutoInject] private AppAuthenticationStateProvider authStateProvider = default!;

    [Parameter]
    [SupplyParameterFromQuery]
    public string? Email { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? Token { get; set; }

    public ResetPasswordRequestDto ResetPasswordModel { get; set; } = new();

    public bool IsLoading { get; set; }

    public BitMessageBarType ResetPasswordMessageType { get; set; }

    public string? ResetPasswordMessage { get; set; }

    private bool IsSubmitButtonEnabled =>
        string.IsNullOrWhiteSpace(ResetPasswordModel.Password) is false &&
        string.IsNullOrWhiteSpace(ResetPasswordModel.ConfirmPassword) is false && 
        IsLoading is false;

    private async Task Submit()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        ResetPasswordMessage = null;

        try
        {
            await httpClient.PostAsJsonAsync("Auth/ResetPassword", ResetPasswordModel, AppJsonContext.Default.ResetPasswordRequestDto);

            ResetPasswordMessageType = BitMessageBarType.Success;

            ResetPasswordMessage = AuthStrings.PasswordChangedSuccessfullyMessage;

            await authService.SignIn(new SignInRequestDto
            {
                UserName = Email,
                Password = ResetPasswordModel.Password
            });

            navigationManager.NavigateTo("/");
        }
        catch (KnownException e)
        {
            ResetPasswordMessageType = BitMessageBarType.Error;

            ResetPasswordMessage = ErrorStrings.ResourceManager.Translate(e.Message, Email!);
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected override async Task OnInitAsync()
    {
        ResetPasswordModel.Email = Email;
        ResetPasswordModel.Token = Token;

        await base.OnInitAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (await authStateProvider.IsUserAuthenticated())
            {
                navigationManager.NavigateTo("/");
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}
