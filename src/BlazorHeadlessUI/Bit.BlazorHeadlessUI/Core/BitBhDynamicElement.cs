using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Renders an arbitrary HTML element whose tag name is determined at runtime.
/// This is the Blazor equivalent of React's polymorphic "as" prop pattern
/// used throughout headless UI libraries.
///
/// Accepts any HTML attributes via <see cref="Attributes"/> (using
/// CaptureUnmatchedValues) and forwards an optional
/// <see cref="ElementReference"/> via <see cref="ElementRefCallback"/>.
///
/// Used internally by headless components that prefer .razor markup
/// over manual RenderTreeBuilder code.
/// </summary>
public class BitBhDynamicElement : ComponentBase
{
    [Parameter, EditorRequired]
    public string Tag { get; set; } = "div";

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public Action<ElementReference>? ElementRefCallback { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? Attributes { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);

        if (Attributes is not null)
            builder.AddMultipleAttributes(10, Attributes);

        if (ElementRefCallback is not null)
            builder.AddElementReferenceCapture(20, ElementRefCallback);

        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }
}
