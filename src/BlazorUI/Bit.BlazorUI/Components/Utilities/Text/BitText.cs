using System.Globalization;
using Microsoft.AspNetCore.Components.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// Use text to present your design and content as clearly and efficiently as possible.
/// </summary>
public partial class BitText : BitComponentBase
{
    /// <summary>
    /// Sets the horizontal alignment of the text content.
    /// </summary>
    [Parameter] public BitTextAlign? Align { get; set; }

    /// <summary>
    /// The content of the Text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The general color of the text.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The custom html element used for the root node.
    /// </summary>
    [Parameter] public string? Element { get; set; }

    /// <summary>
    /// The kind of the foreground color of the text.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Foreground { get; set; }

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

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-txt-pri",
            BitColor.Secondary => "bit-txt-sec",
            BitColor.Tertiary => "bit-txt-ter",
            BitColor.Info => "bit-txt-inf",
            BitColor.Success => "bit-txt-suc",
            BitColor.Warning => "bit-txt-wrn",
            BitColor.SevereWarning => "bit-txt-swr",
            BitColor.Error => "bit-txt-err",
            BitColor.PrimaryBackground => "bit-txt-pbg",
            BitColor.SecondaryBackground => "bit-txt-sbg",
            BitColor.TertiaryBackground => "bit-txt-tbg",
            BitColor.PrimaryForeground => "bit-txt-pfg",
            BitColor.SecondaryForeground => "bit-txt-sfg",
            BitColor.TertiaryForeground => "bit-txt-tfg",
            BitColor.PrimaryBorder => "bit-txt-pbr",
            BitColor.SecondaryBorder => "bit-txt-sbr",
            BitColor.TertiaryBorder => "bit-txt-tbr",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Foreground switch
        {
            BitColorKind.Primary => "bit-txt-pfg",
            BitColorKind.Secondary => "bit-txt-sfg",
            BitColorKind.Tertiary => "bit-txt-tfg",
            BitColorKind.Transparent => "bit-txt-rfg",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Align.HasValue is false ? null :
            $"text-align:{Align switch
            {
                BitTextAlign.Start => "start",
                BitTextAlign.End => "end",
                BitTextAlign.Left => "left",
                BitTextAlign.Right => "right",
                BitTextAlign.Center => "center",
                BitTextAlign.Justify => "justify",
                BitTextAlign.JustifyAll => "justify-all",
                BitTextAlign.MatchParent => "match-parent",
                BitTextAlign.Inherit => "inherit",
                BitTextAlign.Initial => "initial",
                BitTextAlign.Revert => "revert",
                BitTextAlign.RevertLayer => "revert-layer",
                BitTextAlign.Unset => "unset",
                _ => "start"
            }}");
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
