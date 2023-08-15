namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public class SplitActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;

    public string? Class { get; set; }
    public string? Style { get; set; }

    public RenderFragment<SplitActionItem>? Fragment { get; set; }

    public Action<SplitActionItem>? Clicked { get; set; }
}
