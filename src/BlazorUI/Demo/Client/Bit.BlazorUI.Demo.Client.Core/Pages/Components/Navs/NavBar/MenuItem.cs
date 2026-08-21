namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public class MenuItem
{
    public string? Title { get; set; }
    public string? ImageName { get; set; }
    public BitIconInfo? Image { get; set; }
    public RenderFragment<MenuItem>? Fragment { get; set; }
    public string? CssClass { get; set; }
    public string? Style { get; set; }
    public bool Disabled { get; set; }
    public string? Link { get; set; }
    public IEnumerable<string>? ExtraLinks { get; set; }
    public BitNavMatch? Matching { get; set; }
    public string? Counter { get; set; }
    public bool Marker { get; set; }
}
