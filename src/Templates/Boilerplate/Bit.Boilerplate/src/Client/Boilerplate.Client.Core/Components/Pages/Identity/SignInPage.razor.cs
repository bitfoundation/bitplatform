//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class SignInPage
{
    private bool isWaiting;
    private bool isSendingOtp;
    private bool requiresTwoFactor;
    private bool isSendingTfaToken;
    private readonly SignInRequestDto model = new();

    private string? message;
    private BitSeverity messageSeverity;
    private ElementReference messageRef = default!;


    [AutoInject] private ILocalHttpServer localHttpServer = default!;
    [AutoInject] private IIdentityController identityController = default!;


    [Parameter, SupplyParameterFromQuery(Name = "return-url")]
    public string? ReturnUrlQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "email")]
    public string? EmailQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "phoneNumber")]
    public string? PhoneNumberQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "otp")]
    public string? OtpQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "error")]
    public string? ErrorQueryString { get; set; }


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (string.IsNullOrEmpty(EmailQueryString) is false)
        {
            model.Email = EmailQueryString;
        }

        if (string.IsNullOrEmpty(PhoneNumberQueryString) is false)
        {
            model.PhoneNumber = PhoneNumberQueryString;
        }

        if (string.IsNullOrEmpty(OtpQueryString) is false)
        {
            model.Otp = OtpQueryString;

            if (InPrerenderSession is false &&
                (string.IsNullOrEmpty(model.Email) is false ||
                 string.IsNullOrEmpty(model.PhoneNumber) is false))
            {
                await DoSignIn();
            }
        }

        if (string.IsNullOrEmpty(ErrorQueryString) is false)
        {
            message = ErrorQueryString;
            messageSeverity = BitSeverity.Error;
        }
    }

    private async Task DoSignIn()
    {
        if (isWaiting) return;

        isWaiting = true;
        message = null;

        try
        {
            if (requiresTwoFactor &&
                string.IsNullOrWhiteSpace(model.TwoFactorCode) &&
                string.IsNullOrWhiteSpace(model.TwoFactorRecoveryCode) &&
                string.IsNullOrWhiteSpace(model.TwoFactorToken)) return;

            requiresTwoFactor = await AuthenticationManager.SignIn(model, CurrentCancellationToken);

            if (requiresTwoFactor is false)
            {
                NavigationManager.NavigateTo(ReturnUrlQueryString ?? "/", replace: true);
            }
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageSeverity = BitSeverity.Error;
            await messageRef.ScrollIntoView();
        }
        finally
        {
            isWaiting = false;
        }
    }

    private async Task SendOtp()
    {
        if (isSendingOtp) return;

        isSendingOtp = true;
        message = null;

        try
        {
            var request = new IdentityRequestDto { UserName = model.UserName, Email = model.Email, PhoneNumber = model.PhoneNumber };
            await identityController.SendOtp(request, ReturnUrlQueryString, CurrentCancellationToken);

            message = Localizer[nameof(AppStrings.OtpSentMessage)];
            messageSeverity = BitSeverity.Success;
            await messageRef.ScrollIntoView();
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageSeverity = BitSeverity.Error;
            await messageRef.ScrollIntoView();
        }
        finally
        {
            isSendingOtp = false;
        }
    }

    private async Task SendTfaToken()
    {
        if (isSendingTfaToken) return;

        isSendingTfaToken = true;
        message = null;

        try
        {
            await identityController.SendTwoFactorToken(model, CurrentCancellationToken);

            message = Localizer[nameof(AppStrings.TfaTokenSentMessage)];
            messageSeverity = BitSeverity.Success;
            await messageRef.ScrollIntoView();
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageSeverity = BitSeverity.Error;
            await messageRef.ScrollIntoView();
        }
        finally
        {
            isSendingTfaToken = false;
        }
    }

    private async Task GoogleSignIn()
    {
        await SocialSignIn("Google");
    }

    private async Task GitHubSignIn()
    {
        await SocialSignIn("GitHub");
    }

    private async Task TwitterSignIn()
    {
        await SocialSignIn("Twitter");
    }

    private async Task SocialSignIn(string provider)
    {
        if (isWaiting) return;

        isWaiting = true;
        message = null;

        try
        {
            var port = await localHttpServer.Start();

            var redirectUrl = await identityController.GetSocialSignInUri(provider, ReturnUrlQueryString, port is -1 ? null : port);

            NavigationManager.NavigateTo(redirectUrl, true, true);
        }
        catch (KnownException e)
        {
            isWaiting = false;

            message = e.Message;
            messageSeverity = BitSeverity.Error;

            await messageRef.ScrollIntoView();
        }
    }
}
