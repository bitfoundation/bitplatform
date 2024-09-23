namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public class Operation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
    public bool IsSelected { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }

    public RenderFragment<Operation>? Fragment { get; set; }

    public Action<Operation>? Clicked { get; set; }
}
