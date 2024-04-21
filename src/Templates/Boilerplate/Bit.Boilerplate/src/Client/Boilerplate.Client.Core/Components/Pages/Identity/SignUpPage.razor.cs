//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class SignUpPage
{
    [AutoInject] private IIdentityController identityController = default!;

    private bool isLoading;
    private bool isSignedUp;
    private string? signUpMessage;
    private BitMessageBarType signUpMessageType;
    private SignUpRequestDto signUpModel = new();

    private async Task DoSignUp()
    {
        if (isLoading) return;

        //#if (captcha == "reCaptcha")
        var googleRecaptchaResponse = await JSRuntime.GoogleRecaptchaGetResponse();
        if (string.IsNullOrWhiteSpace(googleRecaptchaResponse))
        {
            signUpMessageType = BitMessageBarType.Error;
            signUpMessage = Localizer[nameof(AppStrings.InvalidGoogleRecaptchaChallenge)];
            return;
        }

        signUpModel.GoogleRecaptchaResponse = googleRecaptchaResponse;
        //#endif

        isLoading = true;
        signUpMessage = null;

        try
        {
            await identityController.SignUp(signUpModel, CurrentCancellationToken);

            isSignedUp = true;
        }
        catch (ResourceValidationException e)
        {
            signUpMessageType = BitMessageBarType.Error;
            signUpMessage = string.Join(Environment.NewLine, e.Payload.Details.SelectMany(d => d.Errors).Select(e => e.Message));
        }
        catch (KnownException e)
        {
            signUpMessage = e.Message;
            signUpMessageType = BitMessageBarType.Error;
        }
        finally
        {
            //#if (captcha == "reCaptcha")
            await JSRuntime.GoogleRecaptchaReset();
            //#endif
            isLoading = false;
        }
    }

    private async Task DoResendLink()
    {
        if (isLoading) return;

        isLoading = true;
        signUpMessage = null;

        try
        {
            var sendConfirmationEmailRequest = new SendConfirmationEmailRequestDto { Email = signUpModel.Email };

            //#if (captcha == "reCaptcha")
            var googleRecaptchaResponse = await JSRuntime.GoogleRecaptchaGetResponse();
            if (string.IsNullOrWhiteSpace(googleRecaptchaResponse))
            {
                signUpMessageType = BitMessageBarType.Error;
                signUpMessage = Localizer[nameof(AppStrings.InvalidGoogleRecaptchaChallenge)];
                return;
            }

            sendConfirmationEmailRequest.GoogleRecaptchaResponse = googleRecaptchaResponse;
            //#endif

            await identityController.SendConfirmationEmail(sendConfirmationEmailRequest, CurrentCancellationToken);

            signUpMessageType = BitMessageBarType.Success;
            signUpMessage = Localizer[nameof(AppStrings.ResendConfirmationLinkMessage)];
        }
        catch (KnownException e)
        {
            signUpMessage = e.Message;
            signUpMessageType = BitMessageBarType.Error;
        }
        finally
        {
            //#if (captcha == "reCaptcha")
            await JSRuntime.GoogleRecaptchaReset();
            //#endif
            isLoading = false;
        }
    }
}
