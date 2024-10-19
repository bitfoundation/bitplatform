namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public class PageInfo
{
    public string? Name { get; set; }

    public string? Address { get; set; }

    public string? HtmlClass { get; set; }

    public string? HtmlStyle { get; set; }

    public string? Icon { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;

    public RenderFragment<PageInfo>? Fragment { get; set; }

    public RenderFragment<PageInfo>? OverflowFragment { get; set; }
}
