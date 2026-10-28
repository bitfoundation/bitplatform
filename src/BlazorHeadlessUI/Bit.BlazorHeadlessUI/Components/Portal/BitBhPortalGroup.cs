using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Groups multiple <see cref="BitBhPortal"/> components so they render within a
/// shared container element. When a <c>BitBhPortal</c> is nested inside a
/// <c>BitBhPortalGroup</c>, it renders its content in place (within the group)
/// rather than teleporting to the layout-level <see cref="BitBhPortalOutlet"/>.
///
/// <para>
/// This is useful when you want portalled content to stay within a specific
/// region of your layout (e.g. a panel or sidebar) rather than escaping to
/// the very end of the document.
/// </para>
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Scoped rendering</b> — Nested portals render inside this group's element.</item>
///   <item><b>Data attributes</b> — Emits <c>data-bit-bh-portal-group</c> for identification.</item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BitBhPortalGroup class="my-target"&gt;
///     &lt;BitBhPortal&gt;
///         &lt;div class="tooltip"&gt;I render inside the PortalGroup!&lt;/div&gt;
///     &lt;/BitBhPortal&gt;
/// &lt;/BitBhPortalGroup&gt;
/// </code>
/// </summary>
public class BitBhPortalGroup : BitBhComponentBase
{
    /// <summary>
    /// Child content. Should contain one or more <see cref="BitBhPortal"/> components.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BitBhPortalContext { TargetId = ComponentId };

        builder.OpenComponent<CascadingValue<BitBhPortalContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());

            if (Ref is not null)
                inner.AddElementReferenceCapture(30, Ref);

            if (ChildContent is not null)
                inner.AddContent(40, ChildContent);

            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["data-bit-bh-portal-group"] = "";
        return attrs;
    }
}
