namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
    public RenderFragment<MenuItem>? Fragment { get; set; }
    public string? CssClass { get; set; }
    public string? Style { get; set; }
    public bool Disabled { get; set; }
}
