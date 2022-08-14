using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.App.Pages;

public partial class ResetPasswordPage
{
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
            await HttpClient.PostAsJsonAsync("Auth/ResetPassword", ResetPasswordModel, AppJsonContext.Default.ResetPasswordRequestDto);

            ResetPasswordMessageType = BitMessageBarType.Success;

            ResetPasswordMessage = AuthStrings.PasswordChangedSuccessfullyMessage;

            await AuthenticationService.SignIn(new SignInRequestDto
            {
                UserName = Email,
                Password = ResetPasswordModel.Password
            });

            NavigationManager.NavigateTo("/");
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
            if (await AuthenticationStateProvider.IsUserAuthenticated())
            {
                NavigationManager.NavigateTo("/");
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}
