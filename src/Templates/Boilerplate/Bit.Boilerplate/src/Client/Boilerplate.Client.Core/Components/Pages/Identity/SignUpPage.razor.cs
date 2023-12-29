using Boilerplate.Client.Core.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class SignUpPage
{
    [AutoInject] IIdentityController identityController = default!;

    private bool isLoading;
    private bool isSignedUp;
    private string? signUpMessage;
    private BitMessageBarType signUpMessageType;
    private SignUpRequestDto signUpModel = new();

    private async Task DoSignUp()
    {
        if (isLoading) return;

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
            await identityController.SendConfirmationEmail(new() { Email = signUpModel.Email }, CurrentCancellationToken);

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
            isLoading = false;
        }
    }
}
