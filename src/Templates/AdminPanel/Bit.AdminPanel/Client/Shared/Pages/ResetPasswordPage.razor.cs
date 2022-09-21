using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.Client.Shared.Pages;

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

    private bool IsSubmitButtonEnabled => IsLoading is false;

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

            ResetPasswordMessage = Localizer[nameof(AppStrings.PasswordChangedSuccessfullyMessage)];

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

            ResetPasswordMessage = e.Message;
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

    protected async override Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if (await AuthenticationStateProvider.IsUserAuthenticated())
        {
            NavigationManager.NavigateTo("/");
        }
    }
}
