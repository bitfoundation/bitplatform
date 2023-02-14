using BlazorWeb.Shared.Dtos.Account;

namespace BlazorWeb.Web.Core.Pages;

public partial class ResetPasswordPage
{
    private bool _isLoading;
    private string? _resetPasswordMessage;
    private BitMessageBarType _resetPasswordMessageType;
    private ResetPasswordRequestDto _resetPasswordModel = new();

    [Parameter]
    [SupplyParameterFromQuery]
    public string? Email { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? Token { get; set; }

    protected override void OnInitialized()
    {
        _resetPasswordModel.Email = Email;
        _resetPasswordModel.Token = Token;

        base.OnInitialized();
    }

    protected async override Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if (await AuthenticationStateProvider.IsUserAuthenticatedAsync())
        {
            NavigationManager.NavigateTo("/");
        }
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
            _resetPasswordMessageType = BitMessageBarType.Error;

            _resetPasswordMessage = e.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
