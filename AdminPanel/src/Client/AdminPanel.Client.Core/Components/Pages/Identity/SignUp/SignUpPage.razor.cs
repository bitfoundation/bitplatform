using AdminPanel.Shared.Dtos.Identity;
using AdminPanel.Shared.Controllers.Identity;

namespace AdminPanel.Client.Core.Components.Pages.Identity.SignUp;

public partial class SignUpPage
{
    private bool isWaiting;
    private readonly SignUpRequestDto signUpModel = new() { UserName = Guid.NewGuid().ToString() };


    [AutoInject] private ILocalHttpServer localHttpServer = default!;
    [AutoInject] private IIdentityController identityController = default!;
    [AutoInject] private IExternalNavigationService externalNavigationService = default!;


    private async Task DoSignUp()
    {
        if (isWaiting) return;


        isWaiting = true;

        try
        {
            await identityController.SignUp(signUpModel, CurrentCancellationToken);

            var queryParams = new Dictionary<string, object?>();
            if (string.IsNullOrEmpty(signUpModel.Email) is false)
            {
                queryParams.Add("email", signUpModel.Email);
            }
            if (string.IsNullOrEmpty(signUpModel.PhoneNumber) is false)
            {
                queryParams.Add("phoneNumber", signUpModel.PhoneNumber);
            }
            var confirmUrl = NavigationManager.GetUriWithQueryParameters(Urls.ConfirmPage, queryParams);
            NavigationManager.NavigateTo(confirmUrl);
        }
        catch (KnownException e)
        {
            var message = e is ResourceValidationException re
                            ? string.Join(" ", re.Payload.Details.SelectMany(d => d.Errors).Select(e => e.Message))
                            : e.Message;

            SnackBarService.Error(message);

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
            var port = localHttpServer.Start(CurrentCancellationToken);

            var redirectUrl = await identityController.GetSocialSignInUri(provider, localHttpPort: port is -1 ? null : port, cancellationToken: CurrentCancellationToken);

            await externalNavigationService.NavigateToAsync(redirectUrl);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
    }
}
