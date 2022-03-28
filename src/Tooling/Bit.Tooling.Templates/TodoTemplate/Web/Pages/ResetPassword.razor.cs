using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class ResetPassword
{
    [Parameter]
    [SupplyParameterFromQuery]
    public string? Email { get; set; }
    [Parameter]
    [SupplyParameterFromQuery]
    public string? Token { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmNewPassword { get; set; }

    public string? NewPasswordErrorMessage { get; set; }
    public string? ConfirmNewPasswordErrorMessage { get; set; }

    public bool IsSubmitButtonEnabled { get; set; }
    public bool IsLoading { get; set; }

    [Inject] public HttpClient HttpClient { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    [Inject] public ITodoTemplateAuthenticationService TodoTemplateAuthenticationService { get; set; } = default!;

    public BitMessageBarType ResetPasswordMessageType { get; set; }
    public string? ResetPasswordMessage { get; set; }

    private void CheckSubmitButtonEnabled()
    {
        if (string.IsNullOrEmpty(Email))
        {
            IsSubmitButtonEnabled = false;
            return;
        }
        IsSubmitButtonEnabled = true;
    }

    private bool ValidateResetPassword()
    {
        NewPasswordErrorMessage = string.IsNullOrEmpty(NewPassword)
            ? "Please enter your new password."
            : null;

        ConfirmNewPasswordErrorMessage = string.IsNullOrEmpty(ConfirmNewPassword)
            ? "Please enter your confirmation of the new password."
            : null;

        if (string.IsNullOrEmpty(NewPasswordErrorMessage) is false || string.IsNullOrEmpty(ConfirmNewPasswordErrorMessage) is false)
        {
            return false;
        }

        ConfirmNewPasswordErrorMessage = NewPassword != ConfirmNewPassword
            ? "Password confirmation doesn't match Password."
            : null;

        if (string.IsNullOrEmpty(ConfirmNewPasswordErrorMessage) is false)
        {
            return false;
        }

        NewPasswordErrorMessage = NewPassword!.Length < 6
            ? "The password must have at least 6 characters."
            : null;

        if (string.IsNullOrEmpty(NewPasswordErrorMessage) is false)
        {
            return false;
        }

        return true;
    }

    private async Task Submit()
    {
        if (IsLoading || ValidateResetPassword() is false)
        {
            return;
        }

        IsLoading = true;
        IsSubmitButtonEnabled = false;
        ResetPasswordMessage = null;

        try
        {
            await HttpClient.PostAsJsonAsync("Auth/ResetPassword", new()
            {
                Email = Email,
                Token = Token,
                Password = NewPassword
            }, ToDoTemplateJsonContext.Default.ResetPasswordRequestDto);

            ResetPasswordMessageType = BitMessageBarType.Success;

            ResetPasswordMessage = "Your password changed successfully.";

            await TodoTemplateAuthenticationService.SignIn(new SignInRequestDto
            {
                UserName = Email,
                Password = NewPassword
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
            IsSubmitButtonEnabled = true;
        }
    }
}
