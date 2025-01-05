//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.SignUp;

public partial class SignUpPage
{
    [Parameter, SupplyParameterFromQuery(Name = "return-url")]
    public string? ReturnUrlQueryString { get; set; }

    private bool isWaiting;
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
        signUpModel.ReturnUrl = ReturnUrlQueryString ?? Urls.HomePage;

        isWaiting = true;

        try
        {
            await identityController.SignUp(signUpModel, CurrentCancellationToken);

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
            var confirmUrl = NavigationManager.GetUriWithQueryParameters(Urls.ConfirmPage, queryParams);
            NavigationManager.NavigateTo(confirmUrl, replace: true);
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

    private async Task SocialSignUp(string provider)
    {
        try
        {
            var port = localHttpServer.ShouldUseForSocialSignIn() ? localHttpServer.Start(CurrentCancellationToken) : -1;

            var redirectUrl = await identityController.GetSocialSignInUri(provider, localHttpPort: port is -1 ? null : port, cancellationToken: CurrentCancellationToken);

            await externalNavigationService.NavigateToAsync(redirectUrl);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
    }
}
