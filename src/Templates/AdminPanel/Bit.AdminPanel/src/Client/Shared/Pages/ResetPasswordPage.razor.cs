using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.Client.Shared.Pages;

public partial class ResetPasswordPage
{
    private bool _isLoading;
    private string? _resetPasswordMessage;
    private BitMessageBarType _resetPasswordMessageType;
    private readonly ResetPasswordRequestDto _resetPasswordModel = new();

    [Parameter]
    [SupplyParameterFromQuery]
    public string? Email { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? Token { get; set; }

    protected override async Task OnInitAsync()
    {
        _resetPasswordModel.Email = Email;
        _resetPasswordModel.Token = Token;

        await base.OnInitAsync();
    }

    protected async override Task OnAfterFirstRenderAsync()
    {
        if (await AuthenticationStateProvider.IsUserAuthenticatedAsync())
        {
            NavigationManager.NavigateTo("/");
        }

        await base.OnAfterFirstRenderAsync();
    }
    private async Task DoSubmit()
    {
        if (_isLoading) return;

        _isLoading = true;
        _resetPasswordMessage = null;

        try
        {
            await HttpClient.PostAsJsonAsync("Auth/ResetPassword", _resetPasswordModel, AppJsonContext.Default.ResetPasswordRequestDto);

            _resetPasswordMessageType = BitMessageBarType.Success;

            _resetPasswordMessage = Localizer[nameof(AppStrings.PasswordChangedSuccessfullyMessage)];

            await AuthenticationService.SignIn(new SignInRequestDto
            {
                UserName = Email,
                Password = _resetPasswordModel.Password
            });

            NavigationManager.NavigateTo("/");
        }
        catch (KnownException e)
        {
            _resetPasswordMessage = e.Message;
            _resetPasswordMessageType = BitMessageBarType.Error;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
