//+:cnd:noEmit
using System.Threading;
using Boilerplate.Client.Core.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class SignInPage
{
    [AutoInject] private IIdentityController identityController = default!;

    private bool isSigningIn;
    private bool requiresTwoFactor;
    private bool isGeneratingToken;
    private SignInRequestDto signInModel = new();

    private string? message;
    private BitSeverity messageSeverity;

    [SupplyParameterFromQuery(Name = "email"), Parameter] public string? Email { get; set; }
    [SupplyParameterFromQuery(Name = "redirect-url"), Parameter] public string? RedirectUrl { get; set; }

    protected override async Task OnParamsSetAsync()
    {
        await base.OnParamsSetAsync();

        if (string.IsNullOrEmpty(signInModel.UserName))
        {
            signInModel.UserName = Email;
        }
    }

    private async Task DoSignIn()
    {
        if (isSigningIn) return;

        isSigningIn = true;
        message = null;

        try
        {
            if (requiresTwoFactor &&
                string.IsNullOrWhiteSpace(signInModel.TwoFactorCode) &&
                string.IsNullOrWhiteSpace(signInModel.TwoFactorRecoveryCode) &&
                string.IsNullOrWhiteSpace(signInModel.TwoFactorToken)) return;

            requiresTwoFactor = await AuthenticationManager.SignIn(signInModel, CurrentCancellationToken);

            if (requiresTwoFactor is false)
            {
                NavigationManager.NavigateTo(RedirectUrl ?? "/");
            }
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageSeverity = BitSeverity.Error;
        }
        finally
        {
            isSigningIn = false;
        }
    }

    private async Task SendTwoFactorToken()
    {
        if (isGeneratingToken) return;

        isGeneratingToken = true;
        message = null;

        try
        {
            await identityController.SendTwoFactorToken(signInModel, CurrentCancellationToken);

            message = Localizer[nameof(AppStrings.TfaTokenEmailSent)];
            messageSeverity = BitSeverity.Success;
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageSeverity = BitSeverity.Error;
        }
        finally
        {
            isGeneratingToken = false;
        }
    }
}

