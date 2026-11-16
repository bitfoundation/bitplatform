namespace Boilerplate.Client.Core.Components.Common;

public partial class PinInput
{
    [Parameter] public string? Value { get; set; }

    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    [Parameter] public EventCallback<string?> OnFill { get; set; }

    [Parameter] public string? Label { get; set; }

    [Parameter] public string? Description { get; set; }

    [Parameter] public bool Invalid { get; set; }

    [Parameter] public bool IsLoading { get; set; }

    [Parameter] public bool AutoFocus { get; set; }


    private int otpKey;
    private bool useLetters;
    private BitOtpInput otpInput = default!;


    public async Task Clear()
    {
        otpKey++;

        await ValueChanged.InvokeAsync(null);

        StateHasChanged();

        await Focus();
    }

    public async Task Focus()
    {
        await Task.Yield();

        var index = Math.Min(Value?.Length ?? 0, AccountPinCipher.PinLength - 1);

        await otpInput.InputElements[index].FocusAsync();
    }

    private async Task ToggleKeyboard()
    {
        useLetters = useLetters is false;

        await Focus();
    }
}
