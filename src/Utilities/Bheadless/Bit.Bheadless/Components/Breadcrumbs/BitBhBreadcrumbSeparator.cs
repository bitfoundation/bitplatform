using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

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
