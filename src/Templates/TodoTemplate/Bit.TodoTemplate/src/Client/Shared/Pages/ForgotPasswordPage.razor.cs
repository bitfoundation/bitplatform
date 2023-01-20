using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Client.Shared.Pages;

public partial class ForgotPasswordPage
{
    private bool _isLoading;
    private string? _forgotPasswordMessage;
    private BitMessageBarType _forgotPasswordMessageType;
    private SendResetPasswordEmailRequestDto _forgotPasswordModel = new();

    private async Task DoSubmit()
    {
        if (_isLoading) return;

        _isLoading = true;
        _forgotPasswordMessage = null;

        try
        {
            await HttpClient.PostAsJsonAsync("Auth/SendResetPasswordEmail", _forgotPasswordModel, AppJsonContext.Default.SendResetPasswordEmailRequestDto);

            _forgotPasswordMessageType = BitMessageBarType.Success;

            _forgotPasswordMessage = Localizer[nameof(AppStrings.ResetPasswordLinkSentMessage)];
        }
        catch (KnownException e)
        {
            _forgotPasswordMessageType = BitMessageBarType.Error;

            _forgotPasswordMessage = e.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
