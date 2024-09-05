using System.Globalization;
using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

public partial class BitText : BitComponentBase
{
    /// <summary>
    /// The content of the Text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The custom html element used for the root node.
    /// </summary>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// If true, the text will have a bottom margin.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Gutter { get; set; }

    /// <summary>
    /// If true, the text will not wrap, but instead will truncate with a text overflow ellipsis.
    /// Note that text overflow can only happen with block or inline-block level elements(the element needs to have a width in order to overflow).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoWrap { get; set; }

    /// <summary>
    /// The typography of the Text.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitTypography? Typography { get; set; }



    protected override string RootElementClass => "bit-txt";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => $"bit-txt-{(Typography ?? BitTypography.Subtitle1).ToString().ToLower(CultureInfo.InvariantCulture)}")
                    .Register(() => NoWrap ? "bit-txt-nowrap" : string.Empty)
                    .Register(() => Gutter ? "bit-txt-gutter" : string.Empty);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Element ?? _VariantMapping[Typography ?? BitTypography.Subtitle1]);
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



    protected static readonly Dictionary<BitTypography, string> _VariantMapping = new()
    {
        { BitTypography.H1, "h1" },
        { BitTypography.H2, "h2" },
        { BitTypography.H3, "h3" },
        { BitTypography.H4, "h4" },
        { BitTypography.H5, "h5" },
        { BitTypography.H6, "h6" },
        { BitTypography.Subtitle1, "h6" },
        { BitTypography.Subtitle2, "h6" },
        { BitTypography.Body1, "p" },
        { BitTypography.Body2, "p" },
        { BitTypography.Button, "span" },
        { BitTypography.Caption1, "span" },
        { BitTypography.Caption2, "span" },
        { BitTypography.Overline, "span" },
        { BitTypography.Inherit, "p" },
    };
}
