﻿//+:cnd:noEmit
using Boilerplate.Client.Core.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class ForgotPasswordPage
{
    [AutoInject] IIdentityController identityController = default!;

    private bool isLoading;
    private string? forgotPasswordMessage;
    private BitMessageBarType forgotPasswordMessageType;
    private SendResetPasswordEmailRequestDto forgotPasswordModel = new();

    private async Task DoSubmit()
    {
        if (isLoading) return;

        isLoading = true;
        forgotPasswordMessage = null;

        try
        {
            await identityController.SendResetPasswordEmail(forgotPasswordModel, CurrentCancellationToken);

            forgotPasswordMessageType = BitMessageBarType.Success;

            forgotPasswordMessage = Localizer[nameof(AppStrings.ResetPasswordLinkSentMessage)];
        }
        catch (KnownException e)
        {
            forgotPasswordMessageType = BitMessageBarType.Error;

            forgotPasswordMessage = e.Message;
        }
        finally
        {
            isLoading = false;
        }
    }
}
