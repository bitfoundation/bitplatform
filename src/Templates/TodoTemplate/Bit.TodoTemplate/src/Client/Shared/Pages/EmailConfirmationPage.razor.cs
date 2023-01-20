namespace TodoTemplate.Client.Shared.Pages;

public partial class EmailConfirmationPage
{
    private bool _isLoading;
    private string? _emailConfirmationMessage;
    private BitMessageBarType _emailConfirmationMessageType;

    [Parameter]
    [SupplyParameterFromQuery]
    public string? Email { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "email-confirmed")]
    public bool EmailConfirmed { get; set; }

    private void RedirectToSignIn()
    {
        NavigationManager.NavigateTo("/sign-in");
    }

    private async Task DoResendLink()
    {
        if (_isLoading) return;

        _isLoading = true;
        _emailConfirmationMessage = null;

        try
        {
            await HttpClient.PostAsJsonAsync("Auth/SendConfirmationEmail", new() { Email = Email }, AppJsonContext.Default.SendConfirmationEmailRequestDto);

            _emailConfirmationMessageType = BitMessageBarType.Success;

            _emailConfirmationMessage = Localizer[nameof(AppStrings.ResendConfirmationLinkMessage)];
        }
        catch (KnownException e)
        {
            _emailConfirmationMessageType = BitMessageBarType.Error;

            _emailConfirmationMessage = e.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
