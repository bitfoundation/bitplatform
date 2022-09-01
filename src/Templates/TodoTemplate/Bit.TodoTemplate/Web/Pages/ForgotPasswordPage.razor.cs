using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class ForgotPasswordPage
{
    public SendResetPasswordEmailRequestDto ForgotPasswordModel { get; set; } = new();

    public bool IsLoading { get; set; }

    public BitMessageBarType ForgotPasswordMessageType { get; set; }

    public string? ForgotPasswordMessage { get; set; }

    private bool IsSubmitButtonEnabled => IsLoading is false;

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
            await HttpClient.PostAsJsonAsync("Auth/SendResetPasswordEmail", ForgotPasswordModel, AppJsonContext.Default.SendResetPasswordEmailRequestDto);

            ForgotPasswordMessageType = BitMessageBarType.Success;

            ForgotPasswordMessage = Localizer[nameof(AppStrings.ResetPasswordLinkSentMessage)];
        }
        catch (KnownException e)
        {
            ForgotPasswordMessageType = BitMessageBarType.Error;

            ForgotPasswordMessage = e.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
