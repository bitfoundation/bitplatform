using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A headless <c>&lt;input&gt;</c> that auto-wires <c>id</c>, <c>aria-describedby</c>,
/// <c>aria-invalid</c>, <c>required</c> and <c>disabled</c> from the parent
/// <see cref="BitBhField"/>, and supports <c>@bind-Value</c> with <c>EditForm</c>
/// validation.
///
/// <para>
/// Emits <c>data-disabled</c>, <c>data-invalid</c> and <c>data-required</c>. All
/// standard input attributes (<c>type</c>, <c>placeholder</c>, <c>autocomplete</c> …)
/// pass through.
/// </para>
/// </summary>
public class BitBhInput : BitBhTextControlBase
{
    protected override string DefaultTag => "input";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ControlId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        AddValueBinding(builder, 30);
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.CloseElement();
    }
}
