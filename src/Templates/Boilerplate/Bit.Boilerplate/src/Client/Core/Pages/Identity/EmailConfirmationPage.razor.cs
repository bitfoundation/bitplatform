namespace Boilerplate.Client.Core.Pages.Identity;

public partial class EmailConfirmationPage
{
    private bool _isLoading;
    private string? _resendLinkErrors;
    private BitMessageBarType _emailConfirmationMessageType = BitMessageBarType.Error;

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
        if (_isLoading) return;

        _isLoading = true;
        _resendLinkErrors = Errors = null;

        try
        {
            await HttpClient.PostAsJsonAsync("Identity/SendConfirmationEmail", new() { Email = Email }, AppJsonContext.Default.SendConfirmationEmailRequestDto);

            _emailConfirmationMessageType = BitMessageBarType.Success;

            _resendLinkErrors = Localizer[nameof(AppStrings.ResendConfirmationLinkMessage)];
        }
        catch (KnownException e)
        {
            _emailConfirmationMessageType = BitMessageBarType.Error;

            _resendLinkErrors = e.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
