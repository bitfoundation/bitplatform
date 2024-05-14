//+:cnd:noEmit
using Boilerplate.Client.Core.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class ForgotPasswordPage
{
    [AutoInject] IIdentityController identityController = default!;

    private bool isLoading;
    private string? forgotPasswordMessage;
    private BitSeverity forgotPasswordMessageSeverity;
    private SendResetPasswordTokenRequestDto forgotPasswordModel = new();

    private async Task DoSubmit()
    {
        if (isLoading) return;

        isLoading = true;
        forgotPasswordMessage = null;

        try
        {
            await identityController.SendResetPasswordToken(forgotPasswordModel, CurrentCancellationToken);

            forgotPasswordMessageSeverity = BitSeverity.Success;

            forgotPasswordMessage = Localizer[nameof(AppStrings.ResetPasswordTokenSentMessage)];
        }
        catch (KnownException e)
        {
            forgotPasswordMessageSeverity = BitSeverity.Error;

            forgotPasswordMessage = e.Message;
        }
        finally
        {
            isLoading = false;
        }
    }
}
