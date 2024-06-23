namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Breadcrumb;

public class PageInfoModel
{
    public string Name { get; set; } = default!;

    public string Address { get; set; } = default!;

    public string HtmlClass { get; set; } = default!;

    public string HtmlStyle { get; set; } = default!;

    public bool IsCurrent { get; set; }

    public bool IsEnabled { get; set; } = true;
}
