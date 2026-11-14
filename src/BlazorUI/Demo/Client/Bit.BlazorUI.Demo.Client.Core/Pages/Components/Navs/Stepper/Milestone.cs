namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Stepper;

public class Milestone
{
    public string? Name { get; set; }

    public string? Note { get; set; }

    public string? Symbol { get; set; }

    public BitIconInfo? Glyph { get; set; }

    public bool HasError { get; set; }

    public bool Blocked { get; set; }

    public string? Css { get; set; }

    public string? Inline { get; set; }
}
