using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.App.Pages;

public partial class ForgotPasswordPage
{
    [AutoInject] private HttpClient httpClient = default!;

    public SendResetPasswordEmailRequestDto ForgotPasswordModel { get; set; } = new();

    public bool IsLoading { get; set; }

    public BitMessageBarType ForgotPasswordMessageType { get; set; }

    public string? ForgotPasswordMessage { get; set; }

    private bool IsSubmitButtonEnabled =>
        string.IsNullOrWhiteSpace(ForgotPasswordModel.Email) is false &&
        IsLoading is false;

    private async Task Submit()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        ForgotPasswordMessage = null;

        try
        {
            await httpClient.PostAsJsonAsync("Auth/SendResetPasswordEmail", ForgotPasswordModel, AppJsonContext.Default.SendResetPasswordEmailRequestDto);

            ForgotPasswordMessageType = BitMessageBarType.Success;

            ForgotPasswordMessage = AuthStrings.ResetPasswordLinkSentMessage;
        }
        catch (KnownException e)
        {
            ForgotPasswordMessageType = BitMessageBarType.Error;

            ForgotPasswordMessage = ErrorStrings.ResourceManager.Translate(e.Message, ForgotPasswordModel.Email!);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
