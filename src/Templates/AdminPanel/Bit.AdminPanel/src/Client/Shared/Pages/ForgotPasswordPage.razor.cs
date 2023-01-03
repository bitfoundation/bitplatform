using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.Client.Shared.Pages;

public partial class ForgotPasswordPage
{
    public bool _isLoading;
    public string? _forgotPasswordMessage;
    public BitMessageBarType _forgotPasswordMessageType;
    public SendResetPasswordEmailRequestDto _forgotPasswordModel = new();

    private async Task Submit()
    {
        if (_isLoading) return;

        _isLoading = true;
        _forgotPasswordMessage = null;

        try
        {
            await HttpClient.PostAsJsonAsync("Auth/SendResetPasswordEmail", _forgotPasswordModel, AppJsonContext.Default.SendResetPasswordEmailRequestDto);

            _forgotPasswordMessageType = BitMessageBarType.Success;

            _forgotPasswordMessage = @Localizer[nameof(AppStrings.ResetPasswordLinkSentMessage)];
        }
        catch (KnownException e)
        {
            _forgotPasswordMessage = e.Message;
            _forgotPasswordMessageType = BitMessageBarType.Error;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
