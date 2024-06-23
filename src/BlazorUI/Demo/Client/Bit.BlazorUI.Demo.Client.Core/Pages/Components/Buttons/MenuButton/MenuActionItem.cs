namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public class MenuActionItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsSelected { get; set; }

    public string? Class { get; set; }
    public string? Style { get; set; }

    public RenderFragment<MenuActionItem>? Fragment { get; set; }

    public Action<MenuActionItem>? Clicked { get; set; }
}
