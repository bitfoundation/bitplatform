using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Account;

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

            await Page.SnackBar!.Success(AppLocalizer[nameof(AppStrings.PasswordChangedSuccessfullyMessage)]);
        }
        finally
        {
            isWaiting = false;
        }
    }
}
