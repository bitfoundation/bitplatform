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
    private BitSeverity signUpMessageSeverity;
    private SignUpRequestDto signUpModel = new() { UserName = Guid.NewGuid().ToString() /* You can bind userName to the UI */ };

    private async Task DoSignUp()
    {
        if (isLoading) return;

        //#if (captcha == "reCaptcha")
        var googleRecaptchaResponse = await JSRuntime.GoogleRecaptchaGetResponse();
        if (string.IsNullOrWhiteSpace(googleRecaptchaResponse))
        {
            signUpMessageSeverity = BitSeverity.Error;
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
            signUpMessageSeverity = BitSeverity.Error;
            signUpMessage = string.Join(Environment.NewLine, e.Payload.Details.SelectMany(d => d.Errors).Select(e => e.Message));
        }
        catch (KnownException e)
        {
            signUpMessage = e.Message;
            signUpMessageSeverity = BitSeverity.Error;
        }
        finally
        {
            //#if (captcha == "reCaptcha")
            await JSRuntime.GoogleRecaptchaReset();
            //#endif
            isLoading = false;
        }
    }

    private async Task DoResendToken()
    {
        if (isLoading) return;

        isLoading = true;
        signUpMessage = null;

        try
        {
            var sendConfirmEmailTokenRequest = new SendEmailTokenRequestDto { Email = signUpModel.Email };

            await identityController.SendConfirmEmailToken(sendConfirmEmailTokenRequest, CurrentCancellationToken);

            signUpMessageSeverity = BitSeverity.Success;
            signUpMessage = Localizer[nameof(AppStrings.ResendConfirmationTokenMessage)];
        }
        catch (KnownException e)
        {
            signUpMessage = e.Message;
            signUpMessageSeverity = BitSeverity.Error;
        }
        finally
        {
            isLoading = false;
        }
    }
}
