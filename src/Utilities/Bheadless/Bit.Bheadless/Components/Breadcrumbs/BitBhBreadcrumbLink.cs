using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

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
