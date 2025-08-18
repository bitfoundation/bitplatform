//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class ForgotPasswordPage
{
    [AutoInject] IIdentityController identityController = default!;

    [Parameter, SupplyParameterFromQuery(Name = "return-url")]
    public string? ReturnUrlQueryString { get; set; }

    private bool isWaiting;
    private readonly SendResetPasswordTokenRequestDto model = new();

    private const string EmailKey = nameof(EmailKey);
    private const string PhoneKey = nameof(PhoneKey);


    private void OnPivotChange(BitPivotItem item)
    {
        if (item.Key == EmailKey)
        {
            model.PhoneNumber = null;
        }

        if (item.Key == PhoneKey)
        {
            model.Email = null;
        }
    }

    private async Task Submit()
    {
        if (isWaiting) return;

        isWaiting = true;

        try
        {
            model.ReturnUrl = ReturnUrlQueryString;

            try
            {
                await identityController.SendResetPasswordToken(model, CurrentCancellationToken);
            }
            catch (TooManyRequestsException e)
            {
                SnackBarService.Error(e.Message);
                // Let's go to the reset password page anyway.
            }

            var queryParams = new Dictionary<string, object?>
            {
                { "return-url", ReturnUrlQueryString }
            };
            if (string.IsNullOrEmpty(model.Email) is false)
            {
                queryParams.Add("email", model.Email);
            }
            if (string.IsNullOrEmpty(model.PhoneNumber) is false)
            {
                queryParams.Add("phoneNumber", model.PhoneNumber);
            }

            var resetPasswordUrl = NavigationManager.GetUriWithQueryParameters(PageUrls.ResetPassword, queryParams);
            NavigationManager.NavigateTo(resetPasswordUrl);
        }
        catch (BadRequestException e) when (e.Key == nameof(AppStrings.UserIsNotConfirmed))
        {
            NavigateToConfirmPage(); // Check out SignInModalService for more details
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

    private void NavigateToConfirmPage()
    {
        var queryParams = new Dictionary<string, object?>
        {
            { "return-url", ReturnUrlQueryString }
        };
        if (string.IsNullOrEmpty(model.Email) is false)
        {
            queryParams.Add("email", model.Email);
        }
        if (string.IsNullOrEmpty(model.PhoneNumber) is false)
        {
            queryParams.Add("phoneNumber", model.PhoneNumber);
        }
        var confirmUrl = NavigationManager.GetUriWithQueryParameters(PageUrls.Confirm, queryParams);
        NavigationManager.NavigateTo(confirmUrl, replace: true);
    }
}
