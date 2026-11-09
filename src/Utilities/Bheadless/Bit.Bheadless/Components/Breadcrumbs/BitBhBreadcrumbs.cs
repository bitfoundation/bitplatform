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

/// <summary>An item (<c>&lt;li&gt;</c>) of a <see cref="BitBhBreadcrumbs"/>.</summary>
public class BitBhBreadcrumbItem : BitBhComponentBase
{
    /// <summary>The content: a <see cref="BitBhBreadcrumbLink"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "li";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }
}

/// <summary>
/// A link of a <see cref="BitBhBreadcrumbs"/>. The <see cref="Current"/> one has <c>aria-current="page"</c> and
/// <c>data-current</c>; without an <see cref="Href"/> it renders a <c>&lt;span&gt;</c>.
/// </summary>
public class BitBhBreadcrumbLink : BitBhComponentBase
{
    /// <summary>The address.</summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>Whether this is the current page.</summary>
    [Parameter]
    public bool Current { get; set; }

    /// <summary>Disables the link (<c>aria-disabled</c>, no <c>href</c>).</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => Href is not null && !Disabled ? "a" : "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Href is not null && !Disabled && Tag.Equals("a", StringComparison.OrdinalIgnoreCase)) attrs["href"] = Href;
        if (Current) attrs["aria-current"] = "page";
        if (Disabled)
        {
            attrs["aria-disabled"] = "true";
            if (Tag.Equals("span", StringComparison.OrdinalIgnoreCase) && Href is not null) attrs["role"] = "link";
        }
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "current", Current);
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }
}

/// <summary>A separator between <see cref="BitBhBreadcrumbItem"/>s (<c>&lt;li role="presentation" aria-hidden="true"&gt;</c>).</summary>
public class BitBhBreadcrumbSeparator : BitBhComponentBase
{
    /// <summary>The content ("/", "›", an icon).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "li";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "presentation";
        attrs["aria-hidden"] = "true";
        return attrs;
    }
}
