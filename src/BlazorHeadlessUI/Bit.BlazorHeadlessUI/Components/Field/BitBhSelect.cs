using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless native <c>&lt;select&gt;</c> that auto-wires <c>id</c>,
/// <c>aria-describedby</c>, <c>aria-invalid</c>, <c>required</c> and <c>disabled</c>
/// from the parent <see cref="BitBhField"/>, and supports <c>@bind-Value</c>.
/// For a fully custom select, use <see cref="BitBhListbox{TValue}"/>.
/// </summary>
public class BitBhSelect : BitBhTextControlBase
{
    /// <summary>The <c>&lt;option&gt;</c> elements.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "select";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ControlId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        AddValueBinding(builder, 30);
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }
}
