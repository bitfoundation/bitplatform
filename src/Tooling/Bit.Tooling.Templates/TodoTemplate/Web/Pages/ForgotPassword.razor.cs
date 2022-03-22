using System.Text.RegularExpressions;

namespace TodoTemplate.App.Pages;

public partial class ForgotPassword
{
    public string? Email { get; set; }

    public string? EmailErrorMessage { get; set; }

    public bool IsSubmitButtonEnabled { get; set; }
    public bool IsLoading { get; set; }

    [Inject] public HttpClient HttpClient { get; set; } = default!;

    public BitMessageBarType ForgotPasswordMessageType { get; set; }
    public string? ForgotPasswordMessage { get; set; }

    private void CheckSubmitButtonEnabled()
    {
        if (string.IsNullOrEmpty(Email))
        {
            IsSubmitButtonEnabled = false;
            return;
        }
        IsSubmitButtonEnabled = true;
    }

    private bool ValidateForgotPassword()
    {
        EmailErrorMessage = string.IsNullOrEmpty(Email)
            ? "Please enter your email."
            : null;

        if (string.IsNullOrEmpty(EmailErrorMessage) is false)
        {
            return false;
        }

        var isCorrectEmailFormat = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");

        EmailErrorMessage = isCorrectEmailFormat.Match(Email!).Success is false
            ? "The email address format is incorrect."
            : null;

        if (string.IsNullOrEmpty(EmailErrorMessage) is false)
        {
            return false;
        }

        return true;
    }

    private async Task Submit()
    {
        if (IsLoading)
        {
            return;
        }

        if (ValidateForgotPassword() is false)
        {
            IsLoading = false;
            IsSubmitButtonEnabled = true;
            return;
        }

        IsLoading = true;
        IsSubmitButtonEnabled = false;
        ForgotPasswordMessage = null;

        try
        {
            await HttpClient.PostAsJsonAsync("Auth/SendResetPasswordEmail", new()
            {
                Email = Email
            }, ToDoTemplateJsonContext.Default.SendResetPasswordEmailRequestDto);

            ForgotPasswordMessageType = BitMessageBarType.Success;

            ForgotPasswordMessage = "The reset password link has been sent.";
        }
        catch (KnownException e)
        {
            ForgotPasswordMessageType = BitMessageBarType.Error;

            ForgotPasswordMessage = ErrorStrings.ResourceManager.Translate(e.Message, Email!);
        }
        finally
        {
            IsLoading = false;
            IsSubmitButtonEnabled = true;
        }
    }
}
