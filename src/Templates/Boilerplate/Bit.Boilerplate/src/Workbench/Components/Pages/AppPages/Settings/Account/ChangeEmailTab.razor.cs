using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Account;

public partial class ChangeEmailTab
{
    private bool isWaiting;
    private readonly ChangeEmailRequestDto changeModel = new();
    private readonly SendEmailTokenRequestDto sendModel = new();

    [AutoInject] private SettingsStore store = default!;

    [CascadingParameter] public SettingsPage Page { get; set; } = default!;

    private bool ShowConfirmation => store.PendingEmail is not null;

    private bool IsEmailUnavailable => string.IsNullOrEmpty(store.PendingEmail);

    protected override void OnInitialized()
    {
        Subscribe(store);

        changeModel.Email = sendModel.Email = string.IsNullOrEmpty(store.PendingEmail) ? null : store.PendingEmail;
    }

    private async Task SendToken()
    {
        if (isWaiting)
            return;

        if (sendModel.Email == Page.Profile?.Email)
        {
            SnackBarService.Error(AppLocalizer[nameof(AppStrings.SameEmailErrorMessage)]);
            return;
        }

        isWaiting = true;

        try
        {
            await ScenarioState.Save(CurrentCancellationToken);

            changeModel.Email = sendModel.Email;
            store.RequestEmailChange(sendModel.Email);

            SnackBarService.Success(AppLocalizer[nameof(AppStrings.SuccessfulSendChangeEmailTokenMessage)]);
        }
        finally
        {
            isWaiting = false;
        }
    }

    private async Task ChangeEmail()
    {
        if (isWaiting)
            return;

        isWaiting = true;

        try
        {
            await ScenarioState.Save(CurrentCancellationToken);

            if (Page.Profile is { } profile && changeModel.Email is { } email)
            {
                Page.UpdateProfile(profile with { Email = email });
            }

            SnackBarService.Warning(AppLocalizer[nameof(AppStrings.SignOutOfAllDevicesWarningMessage)]);

            store.RequestEmailChange(null);
            sendModel.Email = changeModel.Email = changeModel.Token = null;
        }
        finally
        {
            isWaiting = false;
        }
    }

    private void GoBack()
    {
        sendModel.Email = null;
        changeModel.Email = null;

        store.RequestEmailChange(null);
    }
}
