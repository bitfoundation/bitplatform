namespace Boilerplate.Client.Core.Components.Pages.Settings.Account;

public partial class PinTab
{
    private bool hasPin;
    private bool isEditing;
    private bool isWaiting;
    private bool isMismatch;
    private string? pin;
    private string? firstPin;
    private PinInput? pinInput;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        hasPin = await AuthManager.HasPin();
    }

    private async Task StartChange()
    {
        if (isWaiting) return;

        isWaiting = true;

        try
        {
            isEditing = await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken);
        }
        finally
        {
            isWaiting = false;
        }
    }

    private async Task RemovePin()
    {
        if (isWaiting) return;

        isWaiting = true;

        try
        {
            if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false)
                return;

            await AuthManager.RemovePin();

            hasPin = false;

            SnackBarService.Success(Localizer[nameof(AppStrings.PinRemovedMessage)]);
        }
        finally
        {
            isWaiting = false;
        }
    }

    private async Task OnPinFill(string? value)
    {
        if (isWaiting || value?.Length != AccountPinCipher.PinLength) return;

        isMismatch = false;

        if (firstPin is null)
        {
            firstPin = value;
            pin = null;
        }
        else if (firstPin != value)
        {
            firstPin = pin = null;
            isMismatch = true;
        }
        else
        {
            isWaiting = true;

            try
            {
                await AuthManager.SetPin(value);
            }
            finally
            {
                isWaiting = false;
                firstPin = pin = null;
            }

            hasPin = true;
            isEditing = false;

            SnackBarService.Success(Localizer[nameof(AppStrings.PinSavedMessage)]);

            return;
        }

        if (pinInput is not null)
        {
            await pinInput.Focus();
        }
    }

    private void CancelChange()
    {
        isEditing = isMismatch = false;
        firstPin = pin = null;
    }
}
