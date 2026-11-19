using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// Headless breadcrumbs (WAI-ARIA Breadcrumb pattern, React Aria Breadcrumbs): a <c>&lt;nav&gt;</c> landmark named
/// "Breadcrumbs" around an ordered list; the current page's link has <c>aria-current="page"</c>; separators are hidden
/// from assistive technology.
///
/// <code>
/// &lt;BitBhBreadcrumbs class="crumbs"&gt;
///     &lt;BitBhBreadcrumbItem&gt;&lt;BitBhBreadcrumbLink Href="/"&gt;Home&lt;/BitBhBreadcrumbLink&gt;&lt;/BitBhBreadcrumbItem&gt;
///     &lt;BitBhBreadcrumbSeparator&gt;/&lt;/BitBhBreadcrumbSeparator&gt;
///     &lt;BitBhBreadcrumbItem&gt;&lt;BitBhBreadcrumbLink Current="true"&gt;Settings&lt;/BitBhBreadcrumbLink&gt;&lt;/BitBhBreadcrumbItem&gt;
/// &lt;/BitBhBreadcrumbs&gt;
/// </code>
/// </summary>
public class BitBhBreadcrumbs : BitBhComponentBase
{
    /// <summary>The items and separators.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The element of the list inside the landmark. Default: <c>ol</c>.</summary>
    [Parameter]
    public string ListAs { get; set; } = "ol";

    protected override string DefaultTag => "nav";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.OpenElement(30, ListAs);
        builder.AddContent(31, ChildContent);
        builder.CloseElement();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (!Tag.Equals("nav", StringComparison.OrdinalIgnoreCase)) attrs["role"] = "navigation";
        attrs["aria-label"] = Strings.Breadcrumbs;
        return attrs;
    }
}
