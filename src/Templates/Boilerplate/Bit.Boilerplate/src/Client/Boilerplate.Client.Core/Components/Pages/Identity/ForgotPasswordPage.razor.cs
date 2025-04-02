//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class ForgotPasswordPage
{
    [AutoInject] IIdentityController identityController = default!;

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
            await identityController.SendResetPasswordToken(model, CurrentCancellationToken);

            var queryParams = new Dictionary<string, object?>();
            if (string.IsNullOrEmpty(model.Email) is false)
            {
                queryParams.Add("email", model.Email);
            }
            if (string.IsNullOrEmpty(model.PhoneNumber) is false)
            {
                queryParams.Add("phoneNumber", model.PhoneNumber);
            }
            var resetPasswordUrl = NavigationManager.GetUriWithQueryParameters(Urls.ResetPasswordPage, queryParams);
            NavigationManager.NavigateTo(resetPasswordUrl);
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
