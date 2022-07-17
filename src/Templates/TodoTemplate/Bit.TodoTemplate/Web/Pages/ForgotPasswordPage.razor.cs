using Microsoft.AspNetCore.Components.Forms;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class ForgotPasswordPage
{
    [AutoInject] private HttpClient httpClient = default!;

    public SendResetPasswordEmailRequestDto ForgotPasswordModel { get; set; } = new();

    public bool IsLoading { get; set; }

    public BitMessageBarType ForgotPasswordMessageType { get; set; }

    public string? ForgotPasswordMessage { get; set; }

    private EditContext? FormContext;

    private bool IsSubmitButtonEnabled =>
        FormContext is not null 
        && FormContext.Validate()
        && ForgotPasswordModel.Email.HasValue()
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
            await httpClient.PostAsJsonAsync("Auth/SendResetPasswordEmail", ForgotPasswordModel, AppJsonContext.Default.SendResetPasswordEmailRequestDto);

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

    protected override void OnInitialized()
    {
        FormContext = new EditContext(ForgotPasswordModel);

        base.OnInitialized();
    }
}
