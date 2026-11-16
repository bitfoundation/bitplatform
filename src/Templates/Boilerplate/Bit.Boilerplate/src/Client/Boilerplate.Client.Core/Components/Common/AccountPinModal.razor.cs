namespace Boilerplate.Client.Core.Components.Common;

public partial class AccountPinModal
{
    [Parameter] public DeviceAccountDto Account { get; set; } = default!;

    [Parameter] public Action<bool>? OnResult { get; set; }

    [Parameter] public Action? OnSignInAgain { get; set; }


    private string? pin;
    private bool isWrongPin;
    private bool isUnlocking;
    private PinInput pinInput = default!;


    private bool CanUnlock => isUnlocking is false && pin?.Length == AccountPinCipher.PinLength;

    private async Task Unlock()
    {
        if (CanUnlock is false) return;

        isUnlocking = true;
        isWrongPin = false;

        try
        {
            if (await AuthManager.SwitchAccount(Account.UserId, pin))
            {
                OnResult?.Invoke(true);
                return;
            }
        }
        finally
        {
            isUnlocking = false;
        }

        isWrongPin = true;
        pin = null;

        await pinInput.Focus();
    }
}
