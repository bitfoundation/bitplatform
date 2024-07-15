using System.Globalization;
using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

public partial class BitTypography : BitComponentBase
{
    /// <summary>
    /// The content of the Typography.
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
    /// The variant of the Typography.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitTypographyVariant Variant { get; set; } = BitTypographyVariant.Subtitle1;



    protected override string RootElementClass => "bit-tpg";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => $"bit-tpg-{Variant.ToString().ToLower(CultureInfo.InvariantCulture)}")
                    .Register(() => NoWrap ? "bit-tpg-nowrap" : string.Empty)
                    .Register(() => Gutter ? "bit-tpg-gutter" : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Element ?? _VariantMapping[Variant]);
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



    protected static readonly Dictionary<BitTypographyVariant, string> _VariantMapping = new()
    {
        { BitTypographyVariant.Body1, "p" },
        { BitTypographyVariant.Body2, "p" },
        { BitTypographyVariant.Button, "span" },
        { BitTypographyVariant.Caption, "span" },
        { BitTypographyVariant.H1, "h1" },
        { BitTypographyVariant.H2, "h2" },
        { BitTypographyVariant.H3, "h3" },
        { BitTypographyVariant.H4, "h4" },
        { BitTypographyVariant.H5, "h5" },
        { BitTypographyVariant.H6, "h6" },
        { BitTypographyVariant.Inherit, "p" },
        { BitTypographyVariant.Overline, "span" },
        { BitTypographyVariant.Subtitle1, "h6" },
        { BitTypographyVariant.Subtitle2, "h6" },
    };
}
