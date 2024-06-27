namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Nav;

public class CarMenu
{
    public string Name { get; set; } = string.Empty;
    public string? Tooltip { get; set; }
    public string? PageUrl { get; set; }
    public string? UrlTarget { get; set; }
    public string? ExpandedAriaLabel { get; set; }
    public string? CollapsedAriaLabel { get; set; }
    public bool IsExpandedParent { get; set; }
    public List<CarMenu> Links { get; set; } = [];
    public string? Comment { get; set; }
}
