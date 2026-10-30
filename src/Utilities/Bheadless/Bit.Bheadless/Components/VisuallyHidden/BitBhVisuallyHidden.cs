using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// Hides content visually while keeping it available to screen readers — for
/// icon-button labels, extra context, skip links. Set <see cref="Focusable"/> to
/// reveal the content when it (or a descendant) receives keyboard focus.
///
/// <code>
/// &lt;button&gt;&lt;svg aria-hidden="true" … /&gt;&lt;BitBhVisuallyHidden&gt;Close&lt;/BitBhVisuallyHidden&gt;&lt;/button&gt;
/// </code>
/// </summary>
public class BitBhVisuallyHidden : BitBhComponentBase
{
    internal const string Style =
        "position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);clip-path:inset(50%);white-space:nowrap;border:0;";

    /// <summary>Show the content while it has keyboard focus (skip links). Default: false.</summary>
    [Parameter]
    public bool Focusable { get; set; }

    /// <summary>The hidden content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

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
        if (Focusable)
        {
            // The JS runtime removes the clipping while focus is inside.
            attrs["data-bit-bh-visually-hidden"] = "focusable";
        }
        attrs["style"] = Style;
        return attrs;
    }
}
