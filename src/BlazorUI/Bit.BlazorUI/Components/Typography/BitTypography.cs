using System.Globalization;

namespace Bit.BlazorUI;

public partial class BitTypography : BitComponentBase
{
    public static Dictionary<BitTypographyVariant, string> VariantMapping = new()
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

    /// <summary>
    /// The content of the Typography.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The component used for the root node.
    /// </summary>
    [Parameter] public string? Component { get; set; }

    /// <summary>
    /// If true, the text will not wrap, but instead will truncate with a text overflow ellipsis.
    /// Note that text overflow can only happen with block or inline-block level elements(the element needs to have a width in order to overflow).
    /// </summary>
    [Parameter] public bool NoWrap { get; set; }

    /// <summary>
    /// The variant of the Typography.
    /// </summary>
    [Parameter] public BitTypographyVariant Variant { get; set; } = BitTypographyVariant.Subtitle1;



    protected override string RootElementClass => "bit-tpg";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => $"bit-tpg-{Variant.ToString().ToLower(CultureInfo.InvariantCulture)}")
                    .Register(() => NoWrap ? "bit-tpg-nowrap" : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;
        builder.OpenElement(seq++, Component ?? VariantMapping[Variant]);
        builder.AddMultipleAttributes(seq++, Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.TypeCheck<IEnumerable<KeyValuePair<string, object>>>(HtmlAttributes));
        builder.AddAttribute(seq++, "style", StyleBuilder.Value);
        builder.AddAttribute(seq++, "class", ClassBuilder.Value);
        builder.AddElementReferenceCapture(seq++, v => RootElement = v);

        builder.AddContent(seq++, ChildContent);

        builder.CloseElement();

        base.BuildRenderTree(builder);
    }
}
