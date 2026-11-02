using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>A link inside a <see cref="BitBhToolbar"/> (roving tab stop).</summary>
public class BitBhToolbarLink : BitBhRovingItemBase
{
    /// <summary>Link content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "a";

    /// <inheritdoc />
    public override bool IsItemDisabled => false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        AddRovingHandlers(builder, 30);
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }
}
