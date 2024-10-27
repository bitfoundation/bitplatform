//+:cnd:noEmit
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.SignIn;

public partial class SignInPage : IDisposable
{
    [Parameter, SupplyParameterFromQuery(Name = "return-url")]
    public string? ReturnUrlQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "userName")]
    public string? UserNameQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "email")]
    public string? EmailQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "phoneNumber")]
    public string? PhoneNumberQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "otp")]
    public string? OtpQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "error")]
    public string? ErrorQueryString { get; set; }


    [AutoInject] private ILocalHttpServer localHttpServer = default!;
    [AutoInject] private IIdentityController identityController = default!;
    [AutoInject] private IExternalNavigationService externalNavigationService = default!;

    private bool isWaiting;
    private bool isOtpSent;
    private bool requiresTwoFactor;
    private readonly SignInRequestDto model = new();
    private Action unsubscribeIdentityHeaderBackLinkClicked = default!;


    private const string OtpPayload = nameof(OtpPayload);
    private const string TfaPayload = nameof(TfaPayload);


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        model.UserName = UserNameQueryString;
        model.Email = EmailQueryString;
        model.PhoneNumber = PhoneNumberQueryString;
        model.DeviceInfo = AppPlatform.OSDescription;

        if (string.IsNullOrEmpty(OtpQueryString) is false)
        {
            model.Otp = OtpQueryString;

            if (InPrerenderSession is false &&
                (string.IsNullOrEmpty(model.UserName) is false ||
                 string.IsNullOrEmpty(model.Email) is false ||
                 string.IsNullOrEmpty(model.PhoneNumber) is false))
            {
                await DoSignIn();
            }
        }

        if (string.IsNullOrEmpty(ErrorQueryString) is false)
        {
            SnackBarService.Error(ErrorQueryString);
        }

        unsubscribeIdentityHeaderBackLinkClicked = PubSubService.Subscribe(PubSubMessages.IDENTITY_HEADER_BACK_LINK_CLICKED, async payload =>
        {
            var source = (string?)payload;

            if (source == OtpPayload)
            {
                isOtpSent = false;
                model.Otp = null;
            }

            if (source == TfaPayload)
            {
                requiresTwoFactor = false;
                model.TwoFactorCode = null;
            }

            await InvokeAsync(StateHasChanged);

            PubSubService.Publish(PubSubMessages.UPDATE_IDENTITY_HEADER_BACK_LINK, null);
        });
    }


    private async Task SocialSignIn(string provider)
    {
        try
        {
            var port = localHttpServer.Start(CurrentCancellationToken);

            var redirectUrl = await identityController.GetSocialSignInUri(provider, ReturnUrlQueryString, port is -1 ? null : port, CurrentCancellationToken);

            await externalNavigationService.NavigateToAsync(redirectUrl);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
    }

    private async Task DoSignIn()
    {
        if (isWaiting) return;
        if (isOtpSent && string.IsNullOrWhiteSpace(model.Otp)) return;

        isWaiting = true;

        try
        {
            if (requiresTwoFactor && string.IsNullOrWhiteSpace(model.TwoFactorCode)) return;

            requiresTwoFactor = await AuthenticationManager.SignIn(model, CurrentCancellationToken);

            if (requiresTwoFactor is false)
            {
                NavigationManager.NavigateTo(ReturnUrlQueryString ?? Urls.HomePage, replace: true);
            }
            else
            {
                PubSubService.Publish(PubSubMessages.UPDATE_IDENTITY_HEADER_BACK_LINK, TfaPayload);
            }
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

    private Task ResendOtp() => SendOtp(true);
    private Task SendOtp() => SendOtp(false);

    private async Task SendOtp(bool resend)
    {
        if (model.Email is null && model.PhoneNumber is null) return;

        try
        {
            var request = new IdentityRequestDto { UserName = model.UserName, Email = model.Email, PhoneNumber = model.PhoneNumber };

            await identityController.SendOtp(request, ReturnUrlQueryString, CurrentCancellationToken);

            if (resend is false)
            {
                isOtpSent = true;

                PubSubService.Publish(PubSubMessages.UPDATE_IDENTITY_HEADER_BACK_LINK, OtpPayload);
            }
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
    }

    private async Task SendTfaToken()
    {
        try
        {
            await identityController.SendTwoFactorToken(model, CurrentCancellationToken);

            SnackBarService.Success(Localizer[nameof(AppStrings.TfaTokenSentMessage)]);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
    }


    public void Dispose()
    {
        unsubscribeIdentityHeaderBackLinkClicked?.Invoke();
    }
}
