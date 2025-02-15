using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// A simple component with a customizable HTML tag, offering full control over styling, attributes, and directional flow, designed to integrate seamlessly into the bit BlazorUI.
/// </summary>
public partial class BitElement : BitComponentBase
{
    /// <summary>
    /// The content of the element.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The custom html element used for the root node. The default is "div".
    /// </summary>
    [Parameter] public string? Element { get; set; }



    protected override string RootElementClass => "bit-elm";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Element ?? "div");
        builder.AddMultipleAttributes(1, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(2, "id", _Id);
        builder.AddAttribute(3, "style", StyleBuilder.Value);
        builder.AddAttribute(4, "class", ClassBuilder.Value);
        builder.AddAttribute(5, "dir", Dir?.ToString().ToLower());
        builder.AddAttribute(6, "aria-label", AriaLabel);
        builder.AddElementReferenceCapture(7, v => RootElement = v);
        builder.AddContent(8, ChildContent);
        builder.CloseElement();

        base.BuildRenderTree(builder);
    }
}
