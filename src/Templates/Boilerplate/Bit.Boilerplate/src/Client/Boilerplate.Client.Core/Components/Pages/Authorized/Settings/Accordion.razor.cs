namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class Accordion
{
    [Parameter] public string? Name { get; set; }

    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    [Parameter] public string? Title { get; set; }
    [Parameter] public string? Subtitle { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }


    private async Task HandleOnClick()
    {
        Value = Value == Name ? null : Name;
        await ValueChanged.InvokeAsync(Value);
    }
}
