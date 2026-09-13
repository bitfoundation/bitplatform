namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class ConfirmPage
{
    private bool isWaiting;
    private bool isEmailConfirmed;
    private bool isPhoneConfirmed;
    private bool showEmailConfirmation;
    private bool showPhoneConfirmation;
    private readonly ConfirmEmailRequestDto emailModel = new();
    private readonly ConfirmPhoneRequestDto phoneModel = new();

    [AutoInject] private ITelemetryContext telemetryContext = default!;
    [AutoInject] private IIdentityController identityController = default!;

    [Parameter, SupplyParameterFromQuery(Name = "return-url")]
    public string? ReturnUrlQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "email")]
    public string? EmailQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "emailToken")]
    public string? EmailTokenQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "phoneNumber")]
    public string? PhoneNumberQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "phoneToken")]
    public string? PhoneTokenQueryString { get; set; }

    private string GetSafeReturnUrl() => Uri.IsAppRelativeUrl(ReturnUrlQueryString, requireLeadingSlash: false) ? ReturnUrlQueryString : PageUrls.Home;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (string.IsNullOrWhiteSpace(EmailQueryString) is false)
        {
            emailModel.Email = EmailQueryString;
            showEmailConfirmation = true;

            if (string.IsNullOrWhiteSpace(EmailTokenQueryString) is false)
            {
                emailModel.Token = EmailTokenQueryString;
                if (InPrerenderSession is false)
                {
                    await ConfirmEmail();
                }
            }
        }

        if (string.IsNullOrWhiteSpace(PhoneNumberQueryString) is false)
        {
            phoneModel.PhoneNumber = PhoneNumberQueryString;
            showPhoneConfirmation = true;

            if (string.IsNullOrWhiteSpace(PhoneTokenQueryString) is false)
            {
                phoneModel.Token = PhoneTokenQueryString;
                if (InPrerenderSession is false)
                {
                    await ConfirmPhone();
                }
            }
        }

        if (string.IsNullOrWhiteSpace(EmailQueryString) && string.IsNullOrWhiteSpace(PhoneNumberQueryString))
        {
            showEmailConfirmation = showPhoneConfirmation = true;
        }
    }

    private async Task ConfirmEmail()
    {
        if (isWaiting || string.IsNullOrWhiteSpace(emailModel.Email) || string.IsNullOrWhiteSpace(emailModel.Token)) return;

        await WrapRequest(async () =>
        {
            var signInResponse = await identityController.ConfirmEmail(new()
            {
                Email = emailModel.Email,
                Token = emailModel.Token
            }, CurrentCancellationToken);

            await AuthManager.StoreTokens(signInResponse, true);

            NavigationManager.NavigateTo(GetSafeReturnUrl(), replace: true);

            isEmailConfirmed = true;
        });
    }

    private async Task ResendEmailToken()
    {
        if (isWaiting || string.IsNullOrWhiteSpace(emailModel.Email)) return;

        await WrapRequest(async () =>
        {
            await identityController.SendConfirmEmailToken(new() { Email = emailModel.Email, ReturnUrl = GetSafeReturnUrl() }, CurrentCancellationToken);
        });
    }

    private async Task ConfirmPhone()
    {
        if (isWaiting || string.IsNullOrWhiteSpace(phoneModel.PhoneNumber) || string.IsNullOrWhiteSpace(phoneModel.Token)) return;

        await WrapRequest(async () =>
        {
            var signInResponse = await identityController.ConfirmPhone(new()
            {
                Token = phoneModel.Token,
                PhoneNumber = phoneModel.PhoneNumber
            }, CurrentCancellationToken);

            await AuthManager.StoreTokens(signInResponse, true);

            NavigationManager.NavigateTo(GetSafeReturnUrl(), replace: true);

            isPhoneConfirmed = true;
        });
    }

    private async Task ResendPhoneToken()
    {
        if (isWaiting || string.IsNullOrWhiteSpace(phoneModel.PhoneNumber)) return;

        await WrapRequest(async () =>
        {
            await identityController.SendConfirmPhoneToken(new() { PhoneNumber = phoneModel.PhoneNumber }, CurrentCancellationToken);
        });
    }

    private async Task WrapRequest(Func<Task> action)
    {
        isWaiting = true;

        try
        {
            await action();
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isWaiting = false;
        }
    }
}
