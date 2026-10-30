using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Account;

public partial class ChangePhoneNumberTab
{
    private bool isWaiting;
    private readonly SendPhoneTokenRequestDto sendModel = new();
    private readonly ChangePhoneNumberRequestDto changeModel = new();

    [AutoInject] private SettingsStore store = default!;

    [CascadingParameter] public SettingsPage Page { get; set; } = default!;

    private bool ShowConfirmation => store.PendingPhoneNumber is not null;

    private bool IsPhoneNumberUnavailable => string.IsNullOrEmpty(store.PendingPhoneNumber);

    protected override void OnInitialized()
    {
        Subscribe(store);

        changeModel.PhoneNumber = sendModel.PhoneNumber = string.IsNullOrEmpty(store.PendingPhoneNumber) ? null : store.PendingPhoneNumber;
    }

    private async Task SendToken()
    {
        if (isWaiting)
            return;

        if (sendModel.PhoneNumber == Page.Profile?.PhoneNumber)
        {
            await Page.SnackBar!.Error(AppLocalizer[nameof(AppStrings.SamePhoneNumberErrorMessage)]);
            return;
        }

        isWaiting = true;

        try
        {
            await ScenarioState.Save(CurrentCancellationToken);

            changeModel.PhoneNumber = sendModel.PhoneNumber;
            store.RequestPhoneNumberChange(sendModel.PhoneNumber);

            await Page.SnackBar!.Success(AppLocalizer[nameof(AppStrings.SuccessfulSendChangePhoneNumberTokenMessage)]);
        }
        finally
        {
            isWaiting = false;
        }
    }

    private async Task ChangePhoneNumber()
    {
        if (isWaiting)
            return;

        isWaiting = true;

        try
        {
            await ScenarioState.Save(CurrentCancellationToken);

            if (Page.Profile is { } profile && changeModel.PhoneNumber is { } phoneNumber)
            {
                Page.UpdateProfile(profile with { PhoneNumber = phoneNumber });
            }

            await Page.SnackBar!.Warning(AppLocalizer[nameof(AppStrings.SignOutOfAllDevicesWarningMessage)]);

            store.RequestPhoneNumberChange(null);
            sendModel.PhoneNumber = changeModel.PhoneNumber = changeModel.Token = null;
        }
        finally
        {
            isWaiting = false;
        }
    }

    private void GoBack()
    {
        sendModel.PhoneNumber = null;
        changeModel.PhoneNumber = null;

        store.RequestPhoneNumberChange(null);
    }
}
