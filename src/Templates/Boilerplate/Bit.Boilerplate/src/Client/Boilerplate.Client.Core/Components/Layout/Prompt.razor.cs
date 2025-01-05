namespace Boilerplate.Client.Core.Components.Layout;

public partial class Prompt
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public bool OtpInput { get; set; }
    [Parameter] public string? Body { get; set; }
    [Parameter] public Action<string?>? OnOk { get; set; }
    [Parameter] public Action? OnCancel { get; set; }

    private string? value;

    private void CloseModal()
    {
        OnCancel?.Invoke();
    }

    private void OnOkClick()
    {
        if (string.IsNullOrEmpty(value)) return;

        OnOk?.Invoke(value);
    }
}
