namespace Boilerplate.Client.Core.Components.Common;

public partial class LockAccountModal
{
    [Parameter] public Action<string?>? OnResult { get; set; }


    private string? pin;
    private string? firstPin;
    private bool isMismatch;
    private PinInput pinInput = default!;


    private async Task OnPinFill(string? value)
    {
        if (value?.Length != AccountPinCipher.PinLength) return;

        isMismatch = false;

        if (firstPin is null)
        {
            firstPin = value;
        }
        else if (firstPin == value)
        {
            OnResult?.Invoke(value);
            return;
        }
        else
        {
            firstPin = null;
            isMismatch = true;
        }

        await pinInput.Clear();
    }
}
