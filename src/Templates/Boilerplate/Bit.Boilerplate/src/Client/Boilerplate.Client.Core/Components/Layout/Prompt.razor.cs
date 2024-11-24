using System.Threading.Tasks;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class Prompt
{
    private string? value;

    [Parameter] public string? Body { get; set; }
    [Parameter] public Action<string?>? OnOk { get; set; }

    private void OnOkClick()
    {
        OnOk?.Invoke(value);
    }
}
