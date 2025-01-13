//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

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
    [AutoInject] private ITelemetryContext telemetryContext = default!;
    [AutoInject] private IIdentityController identityController = default!;
    [AutoInject] private IExternalNavigationService externalNavigationService = default!;


    private bool isWaiting;
    private bool isOtpSent;
    private bool requiresTwoFactor;
    private SignInPanelTab currentSignInPanelTab;
    private readonly SignInRequestDto model = new();
    private AppDataAnnotationsValidator? validatorRef;
    private Action unsubscribeIdentityHeaderBackLinkClicked = default!;


    private const string OtpPayload = nameof(OtpPayload);
    private const string TfaPayload = nameof(TfaPayload);


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        model.UserName = UserNameQueryString;
        model.Email = EmailQueryString;
        model.PhoneNumber = PhoneNumberQueryString;

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

        unsubscribeIdentityHeaderBackLinkClicked = PubSubService.Subscribe(ClientPubSubMessages.IDENTITY_HEADER_BACK_LINK_CLICKED, async payload =>
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

            PubSubService.Publish(ClientPubSubMessages.UPDATE_IDENTITY_HEADER_BACK_LINK, null);
        });
    }


    private async Task SocialSignIn(string provider)
    {
        try
        {
            var port = localHttpServer.ShouldUseForSocialSignIn() ? localHttpServer.Start(CurrentCancellationToken) : -1;

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

            CleanModel();

            if (validatorRef?.EditContext.Validate() is false) return;

            model.DeviceInfo = telemetryContext.Platform;

            requiresTwoFactor = await AuthManager.SignIn(model, CurrentCancellationToken);

            if (requiresTwoFactor is false)
            {
                NavigationManager.NavigateTo(ReturnUrlQueryString ?? Urls.HomePage, replace: true);
            }
            else
            {
                PubSubService.Publish(ClientPubSubMessages.UPDATE_IDENTITY_HEADER_BACK_LINK, TfaPayload);
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

    private async Task SendOtp(bool resend)
    {
        try
        {
            CleanModel();

            if (model.Email is null && model.PhoneNumber is null) return;

            if (model.Email is not null && new EmailAddressAttribute().IsValid(model.Email) is false)
            {
                SnackBarService.Error(string.Format(AppStrings.EmailAddressAttribute_ValidationError, AppStrings.Email));
                return;
            }

            if (model.PhoneNumber is not null && new PhoneAttribute().IsValid(model.PhoneNumber) is false)
            {
                SnackBarService.Error(string.Format(AppStrings.PhoneAttribute_ValidationError, AppStrings.PhoneNumber));
                return;
            }

            var request = new IdentityRequestDto { UserName = model.UserName, Email = model.Email, PhoneNumber = model.PhoneNumber };

            await identityController.SendOtp(request, ReturnUrlQueryString, CurrentCancellationToken);

            if (resend is false)
            {
                isOtpSent = true;

                PubSubService.Publish(ClientPubSubMessages.UPDATE_IDENTITY_HEADER_BACK_LINK, OtpPayload);
            }
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
    }
    private Task ResendOtp() => SendOtp(true);
    private Task SendOtp() => SendOtp(false);

    private async Task SendTfaToken()
    {
        try
        {
            CleanModel();

            await identityController.SendTwoFactorToken(model, CurrentCancellationToken);

            SnackBarService.Success(Localizer[nameof(AppStrings.TfaTokenSentMessage)]);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
    }

    private void HandleOnSignInPanelTabChange(SignInPanelTab tab)
    {
        currentSignInPanelTab = tab;
    }

    private void CleanModel()
    {
        if (currentSignInPanelTab is SignInPanelTab.Email)
        {
            model.PhoneNumber = null;
            if (validatorRef is null) return;

            validatorRef.EditContext.NotifyFieldChanged(validatorRef.EditContext.Field(nameof(SignInRequestDto.PhoneNumber)));
        }
        else
        {
            model.Email = null;
            if (validatorRef is null) return;

            validatorRef.EditContext.NotifyFieldChanged(validatorRef.EditContext.Field(nameof(SignInRequestDto.Email)));
        }
    }


    public void Dispose()
    {
        unsubscribeIdentityHeaderBackLinkClicked?.Invoke();
    }
}
