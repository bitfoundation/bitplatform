using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class EmailConfirmationPage
{
    [AutoInject] IIdentityController identityController = default!;

    private bool isLoading;
    private string? resendLinkErrors;
    private BitMessageBarType emailConfirmationMessageType = BitMessageBarType.Error;

    [SupplyParameterFromQuery, Parameter] public string? Email { get; set; }

    /// <summary>
    /// Email confirmation errors populated by api/Identity/ConfirmEmail endpoint.
    /// </summary>
    [SupplyParameterFromQuery, Parameter] public string? Errors { get; set; }

    [SupplyParameterFromQuery(Name = "email-confirmed"), Parameter] public bool EmailConfirmed { get; set; }

    private void RedirectToSignIn()
    {
        NavigationManager.NavigateTo("/sign-in");
    }

    private async Task DoResendLink()
    {
        if (isLoading) return;

        isLoading = true;
        resendLinkErrors = Errors = null;

        try
        {
            await identityController.SendConfirmationEmail(new() { Email = Email }, CurrentCancellationToken);

            emailConfirmationMessageType = BitMessageBarType.Success;

            resendLinkErrors = Localizer[nameof(AppStrings.ResendConfirmationLinkMessage)];
        }
        catch (KnownException e)
        {
            emailConfirmationMessageType = BitMessageBarType.Error;

            resendLinkErrors = e.Message;
        }
        finally
        {
            isLoading = false;
        }
    }
}
