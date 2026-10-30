using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless <c>&lt;textarea&gt;</c> that auto-wires <c>id</c>,
/// <c>aria-describedby</c>, <c>aria-invalid</c>, <c>required</c> and <c>disabled</c>
/// from the parent <see cref="BitBhField"/>, and supports <c>@bind-Value</c>.
/// </summary>
public class BitBhTextarea : BitBhTextControlBase
{
    /// <summary>Initial text when not bound.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "textarea";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ControlId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        AddValueBinding(builder, 30);
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (!IsProvided(nameof(Value)))
            builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }
}
