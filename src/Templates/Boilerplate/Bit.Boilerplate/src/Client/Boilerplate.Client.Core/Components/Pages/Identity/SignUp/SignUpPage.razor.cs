//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.SignUp;

public partial class SignUpPage
{
    [Parameter, SupplyParameterFromQuery(Name = "return-url")]
    public string? ReturnUrlQueryString { get; set; }

    private bool isWaiting;
    private Action? pubSubUnsubscribe;
    private readonly SignUpRequestDto signUpModel = new() { UserName = Guid.NewGuid().ToString() };

    [AutoInject] private ILocalHttpServer localHttpServer = default!;
    [AutoInject] private IIdentityController identityController = default!;
    [AutoInject] private IExternalNavigationService externalNavigationService = default!;

    private async Task DoSignUp()
    {
        if (isWaiting) return;

        //#if (captcha == "reCaptcha")
        var googleRecaptchaResponse = await JSRuntime.GoogleRecaptchaGetResponse();
        if (string.IsNullOrWhiteSpace(googleRecaptchaResponse))
        {
            SnackBarService.Error(Localizer[nameof(AppStrings.InvalidGoogleRecaptchaChallenge)]);
            return;
        }

        signUpModel.GoogleRecaptchaResponse = googleRecaptchaResponse;
        //#endif
        signUpModel.ReturnUrl = ReturnUrlQueryString ?? PageUrls.Home;

        isWaiting = true;

        try
        {
            await identityController.SignUp(signUpModel, CurrentCancellationToken);

            NavigateToConfirmPage();
        }
        catch (BadRequestException e) when (e.Key == nameof(AppStrings.UserIsNotConfirmed))
        {
            NavigateToConfirmPage();
        }
        catch (TooManyRequestsException e)
        {
            SnackBarService.Error(e.Message);
            NavigateToConfirmPage();
        }
        catch (KnownException e)
        {
            var message = e is ResourceValidationException re
                            ? string.Join(" ", re.Payload.Details.SelectMany(d => d.Errors).Select(e => e.Message))
                            : e.Message;

            SnackBarService.Error(message);

            //#if (captcha == "reCaptcha")
            await JSRuntime.GoogleRecaptchaReset();
            //#endif
        }
        finally
        {
            isWaiting = false;
        }
    }

    private void NavigateToConfirmPage()
    {
        var queryParams = new Dictionary<string, object?>
        {
            { "return-url", ReturnUrlQueryString }
        };
        if (string.IsNullOrEmpty(signUpModel.Email) is false)
        {
            queryParams.Add("email", signUpModel.Email);
        }
        if (string.IsNullOrEmpty(signUpModel.PhoneNumber) is false)
        {
            queryParams.Add("phoneNumber", signUpModel.PhoneNumber);
        }
        var confirmUrl = NavigationManager.GetUriWithQueryParameters(PageUrls.Confirm, queryParams);
        NavigationManager.NavigateTo(confirmUrl, replace: true);
    }

    private async Task SocialSignUp(string provider)
    {
        try
        {
            pubSubUnsubscribe = PubSubService.Subscribe(ClientPubSubMessages.SOCIAL_SIGN_IN_CALLBACK, async (uriString) =>
            {
                // Social sign-in creates a new user automatically, so we only need to navigate to the sign-in page to automatically sign-in the user by provided OTP.
                NavigationManager.NavigateTo(uriString!.ToString()!, replace: true);
            });

            var port = localHttpServer.EnsureStarted();

            var redirectUrl = await identityController.GetSocialSignInUri(provider, ReturnUrlQueryString, port is -1 ? null : port, CurrentCancellationToken);

            await externalNavigationService.NavigateToAsync(redirectUrl);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        pubSubUnsubscribe?.Invoke();

        await base.DisposeAsync(disposing);
    }
}
