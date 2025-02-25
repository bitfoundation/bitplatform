using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// A component to easily use predefined bit BlazorUI media queries in Blazor components.
/// </summary>
public partial class BitMediaQuery : BitComponentBase
{
    /// <summary>
    /// The content of the element.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Specifies the media query to match for the child content.
    /// </summary>
    [Parameter] public BitScreenQuery? Query { get; set; }



    protected override string RootElementClass => "bit-mdq";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Query switch
        {
            BitScreenQuery.Xs => "bit-mdq-xs",
            BitScreenQuery.Sm => "bit-mdq-sm",
            BitScreenQuery.Md => "bit-mdq-md",
            BitScreenQuery.Lg => "bit-mdq-lg",
            BitScreenQuery.Xl => "bit-mdq-xl",
            BitScreenQuery.Xxl => "bit-mdq-xxl",
            BitScreenQuery.LtSm => "bit-mdq-ltsm",
            BitScreenQuery.LtMd => "bit-mdq-ltmd",
            BitScreenQuery.LtLg => "bit-mdq-ltlg",
            BitScreenQuery.LtXl => "bit-mdq-ltxl",
            BitScreenQuery.LtXxl => "bit-mdq-ltxxl",
            BitScreenQuery.GtXs => "bit-mdq-gtxs",
            BitScreenQuery.GtSm => "bit-mdq-gtsm",
            BitScreenQuery.GtMd => "bit-mdq-gtmd",
            BitScreenQuery.GtLg => "bit-mdq-gtlg",
            BitScreenQuery.GtXl => "bit-mdq-gtxl",
            _ => string.Empty
        });
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
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
