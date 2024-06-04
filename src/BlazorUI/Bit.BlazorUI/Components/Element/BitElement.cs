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
        var seq = 0;
        builder.OpenElement(seq++, Tag);
        builder.AddMultipleAttributes(seq++, Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<IEnumerable<KeyValuePair<string, object>>>(HtmlAttributes));
        builder.AddAttribute(seq++, "id", _Id);
        builder.AddAttribute(seq++, "style", StyleBuilder.Value);
        builder.AddAttribute(seq++, "class", ClassBuilder.Value);
        builder.AddAttribute(seq++, "dir", Dir?.ToString().ToLower());
        builder.AddElementReferenceCapture(seq++, v => RootElement = v);

        builder.AddContent(seq++, ChildContent);

        builder.CloseElement();

        base.BuildRenderTree(builder);
    }
}
