namespace Bit.BlazorUI;

public partial class BitPanel : BitComponentBase
{
    /// <summary>
    /// The content of the Typography.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The component used for the root node.
    /// </summary>
    [Parameter] public string? Component { get; set; }



    protected override string RootElementClass => "bit-pnl";

    protected override void RegisterComponentClasses() { }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;
        builder.OpenElement(seq++, Component ?? "div");
        builder.AddMultipleAttributes(seq++, Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<IEnumerable<KeyValuePair<string, object>>>(HtmlAttributes));
        builder.AddAttribute(seq++, "style", StyleBuilder.Value);
        builder.AddAttribute(seq++, "class", ClassBuilder.Value);
        builder.AddElementReferenceCapture(seq++, v => RootElement = v);

        builder.AddContent(seq++, ChildContent);

        builder.CloseElement();

        base.BuildRenderTree(builder);
    }
}
