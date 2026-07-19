using Boilerplate.Shared.Features.Identity;
using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Client.Core.Components.Pages.Settings.Account;

public partial class ChangePasswordTab
{
    [AutoInject] private IUserController userController = default!;


    private bool isWaiting;
    private readonly ChangePasswordRequestDto model = new();
    private AppDataAnnotationsValidator validatorRef = default!;


    private async Task ChangePassword()
    {
        if (isWaiting) return;

        isWaiting = true;

        try
        {
            await userController.ChangePassword(model, CurrentCancellationToken);

            model.OldPassword = model.NewPassword = model.ConfirmPassword = null;

            SnackBarService.Success(Localizer[nameof(AppStrings.PasswordChangedSuccessfullyMessage)]);
            // Changing the password regenerates the security stamp on the server, so every active session gets signed out on its next token refresh.
            SnackBarService.Warning(Localizer[nameof(AppStrings.SignOutOfAllDevicesWarningMessage)]);
        }
        catch (ResourceValidationException exp)
        {
            validatorRef.DisplayErrors(exp);
        }
        catch (KnownException exp)
        {
            SnackBarService.Error(exp.Message);
        }
        finally
        {
            isWaiting = false;
        }
    }
}
