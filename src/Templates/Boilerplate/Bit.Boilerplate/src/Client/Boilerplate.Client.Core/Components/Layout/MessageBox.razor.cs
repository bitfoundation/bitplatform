namespace Boilerplate.Client.Core.Components.Layout;

public partial class MessageBox
{
    [Parameter] public string? Body { get; set; }
    [Parameter] public Action? OnOk { get; set; }


    private void OnOkClick()
    {
        OnOk?.Invoke();
    }
}
