namespace Boilerplate.Client.Core.Components.Pages.Main.Settings;

public partial class Accordion
{
    [Parameter] public int Index { get; set; }

    [Parameter] public int Value { get; set; }
    [Parameter] public EventCallback<int> ValueChanged { get; set; }

    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Subtitle { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }


    private async Task HandleOnClick()
    {
        Value = Value == Index ? 0 : Index;
        await ValueChanged.InvokeAsync(Value);
    }
}
