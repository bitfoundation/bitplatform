namespace Boilerplate.Client.Core.Components.Common;

public partial class UnlockAccountModal
{
    [Parameter] public DeviceUserSessionDto UserSession { get; set; } = default!;

    [Parameter] public Action<bool>? OnResult { get; set; }

    [Parameter] public Action? OnSignInAgain { get; set; }


    private string? pin;
    private int? attemptsLeft;
    private bool isForgotten;
    private bool isUnlocking;
    private PinInput pinInput = default!;


    private bool CanUnlock => isUnlocking is false && pin?.Length == AccountPinCipher.PinLength;

    private async Task Unlock()
    {
        if (CanUnlock is false) return;

        isUnlocking = true;

        try
        {
            if (await AuthManager.SwitchAccount(UserSession.UserId, pin))
            {
                OnResult?.Invoke(true);
                return;
            }
        }
        finally
        {
            isUnlocking = false;
        }

        var userSession = (await AuthManager.GetOtherUserSessions()).FirstOrDefault(us => us.UserId == UserSession.UserId);

        if (userSession is null)
        {
            isForgotten = true;
            return;
        }

        attemptsLeft = AccountPinCipher.MaxFailedAttempts - userSession.FailedPinAttempts;

        await pinInput.Clear();
    }
}
