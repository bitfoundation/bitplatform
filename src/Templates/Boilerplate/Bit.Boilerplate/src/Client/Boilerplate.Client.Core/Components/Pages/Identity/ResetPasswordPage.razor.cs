using Boilerplate.Client.Core.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class ResetPasswordPage
{
    [AutoInject] IIdentityController identityController = default!;

    private bool isLoading;
    private bool passwordChanged;
    private string? resetPasswordMessage;
    private BitMessageBarType resetPasswordMessageType;
    private ResetPasswordRequestDto resetPasswordModel = new();

    [Parameter, SupplyParameterFromQuery] public string? Email { get; set; }

    [Parameter, SupplyParameterFromQuery] public string? Token { get; set; }

    protected override async Task OnInitAsync()
    {
        resetPasswordModel.Email = Email;
        resetPasswordModel.Token = Token;

        await base.OnInitAsync();
    }

    private void RedirectToSignIn()
    {
        NavigationManager.NavigateTo($"/sign-in?email={Uri.EscapeDataString(Email ?? string.Empty)}");
    }

    private async Task DoSubmit()
    {
        if (isLoading) return;

        //#if (captcha == "reCaptcha")
        var googleRecaptchaResponse = await JSRuntime.GoogleRecaptchaGetResponse();
        if (string.IsNullOrWhiteSpace(googleRecaptchaResponse))
        {
            resetPasswordMessageType = BitMessageBarType.Error;
            resetPasswordMessage = Localizer[nameof(AppStrings.InvalidGoogleRecaptchaChallenge)];
            return;
        }

        resetPasswordModel.GoogleRecaptchaResponse = googleRecaptchaResponse;
        //#endif

        isLoading = true;
        resetPasswordMessage = null;

        try
        {
            await identityController.ResetPassword(resetPasswordModel, CurrentCancellationToken);

            resetPasswordMessageType = BitMessageBarType.Success;

            resetPasswordMessage = Localizer[nameof(AppStrings.PasswordChangedSuccessfullyMessage)];

            passwordChanged = true;
        }
        catch (KnownException e)
        {
            resetPasswordMessageType = BitMessageBarType.Error;

            resetPasswordMessage = e.Message;
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
