//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class SignInPage
{
    private bool isLoading;
    private bool requiresTwoFactor;
    private SignInRequestDto signInModel = new();

    private string? message;
    private BitMessageBarType messageType;

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
        if (isLoading) return;

        isLoading = true;
        message = null;

        try
        {
            if (requiresTwoFactor &&
                string.IsNullOrWhiteSpace(signInModel.TwoFactorCode) &&
                string.IsNullOrWhiteSpace(signInModel.TwoFactorRecoveryCode)) return;

            requiresTwoFactor = await AuthenticationManager.SignIn(signInModel, CurrentCancellationToken);

            if (requiresTwoFactor is false)
            {
                NavigationManager.NavigateTo(RedirectUrl ?? "/");
            }
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageType = BitMessageBarType.Error;
        }
        finally
        {
            isLoading = false;
        }
    }
}

