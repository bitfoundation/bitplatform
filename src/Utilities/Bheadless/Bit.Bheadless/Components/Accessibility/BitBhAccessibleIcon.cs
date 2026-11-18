using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// Gives an icon an accessible name (Radix AccessibleIcon): the icon is hidden from assistive technology and the
/// <see cref="Label"/> is read instead - for an icon that means something on its own (a status, an icon-only link
/// inside text). An icon-only button is better named with its own <c>aria-label</c>.
///
/// <code>
/// &lt;BitBhAccessibleIcon Label="Verified"&gt;&lt;svg …&gt;…&lt;/svg&gt;&lt;/BitBhAccessibleIcon&gt;
/// </code>
/// </summary>
public sealed class BitBhAccessibleIcon : BitBhComponentBase
{
    /// <summary>What the icon means, read by assistive technology.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>The icon.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.OpenElement(30, "span");
        builder.AddAttribute(31, "aria-hidden", "true");
        builder.AddContent(32, ChildContent);
        builder.CloseElement();
        builder.OpenComponent<BitBhVisuallyHidden>(40);
        builder.AddComponentParameter(41, nameof(BitBhVisuallyHidden.ChildContent), (RenderFragment)(b => b.AddContent(0, Label)));
        builder.CloseComponent();
        builder.CloseElement();
    }
}
