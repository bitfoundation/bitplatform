using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Account;

/// <summary>The Workbench copy of the app's change-password tab; a valid form only shows the success message.</summary>
public partial class ChangePasswordTab
{
    private bool isWaiting;
    private readonly ChangePasswordRequestDto model = new();

    [CascadingParameter] public SettingsPage Page { get; set; } = default!;

    private async Task ChangePassword()
    {
        if (isWaiting)
            return;

        isWaiting = true;

        try
        {
            await ScenarioState.Save(CurrentCancellationToken);

            model.OldPassword = model.NewPassword = model.ConfirmPassword = null;

            SnackBarService.Success(AppLocalizer[nameof(AppStrings.PasswordChangedSuccessfullyMessage)]);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            isWaiting = false;
        }
    }
}
