using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

public partial class BitElement : BitComponentBase
{
    /// <summary>
    /// The content of the element.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The HTML tag used for the root node.
    /// </summary>
    [Parameter] public string Tag { get; set; } = "div";


    protected override string RootElementClass => "bit-elm";


    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(1, RuntimeHelpers.TypeCheck(HtmlAttributes));
        builder.AddAttribute(2, "id", _Id);
        builder.AddAttribute(3, "style", StyleBuilder.Value);
        builder.AddAttribute(4, "class", ClassBuilder.Value);
        builder.AddAttribute(5, "dir", Dir?.ToString().ToLower());
        builder.AddElementReferenceCapture(6, v => RootElement = v);
        builder.AddContent(7, ChildContent);
        builder.CloseElement();

        base.BuildRenderTree(builder);
    }
}
