using Bit.Tooling.SourceGenerators;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class ForgotPassword
{
    public SendResetPasswordEmailRequestDto ForgotPasswordModel { get; set; } = new();

    public bool IsLoading { get; set; }

    public BitMessageBarType ForgotPasswordMessageType { get; set; }

    public string? ForgotPasswordMessage { get; set; }

    [AutoInject] public HttpClient HttpClient { get; set; } = default!;

    private bool IsSubmitButtonEnabled =>
        ForgotPasswordModel.Email.HasValue() 
        && IsLoading is false;

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
            await HttpClient.PostAsJsonAsync("Auth/SendResetPasswordEmail", ForgotPasswordModel, TodoTemplateJsonContext.Default.SendResetPasswordEmailRequestDto);

            ForgotPasswordMessageType = BitMessageBarType.Success;

            ForgotPasswordMessage = "The reset password link has been sent.";
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
