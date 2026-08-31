using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A component for displaying icons that enhance visual communication and user experience.
/// Supports both built-in Fluent UI icons and external icon libraries.
/// </summary>
public partial class BitIcon : BitComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the icon component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple icon components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitIconParams.ParamName)]
    public BitIconParams? CascadingParameters { get; set; }



    /// <summary>
    /// Specifies a looping animation to play on the icon.
    /// </summary>
    /// <remarks>
    /// An animation is drawn with a transform of its own, so it replaces <see cref="Rotate"/> and
    /// <see cref="Flip"/> rather than combining with them - <see cref="BitIconAnimation.Fade"/>, which
    /// only changes opacity, is the exception and composes with both.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitIconAnimation? Animation { get; set; }

    /// <summary>
    /// The content rendered inside the icon element, for an icon set that is neither a font nor a
    /// class - an inline svg, an image, a ligature of your own.
    /// </summary>
    /// <remarks>
    /// The color, the size and the variant still apply around it, so an inline svg drawn with
    /// <c>fill="currentColor"</c> follows the icon's own color the way a glyph would.
    /// </remarks>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Specifies the color theme of the icon.
    /// Default value is <see cref="BitColor.Primary"/>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Renders the icon in a box of a fixed width so that a column of icons of different widths lines up.
    /// </summary>
    /// <remarks>
    /// The glyphs of an icon font are not all the same width, which is what makes the labels of a menu
    /// or a nav sit at a different place on every row. This gives every icon the same box and centers
    /// the glyph inside it.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool FixedWidth { get; set; }

    /// <summary>
    /// Mirrors the icon on the horizontal axis, the vertical axis, or both.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconFlip? Flip { get; set; }

    /// <summary>
    /// Mirrors the icon horizontally when it is rendered in a right-to-left direction.
    /// </summary>
    /// <remarks>
    /// A glyph that points somewhere - an arrow, a chevron, a reply - points the other way in a
    /// right-to-left layout, while a clock, a checkmark or a media rewind button does not. Only the
    /// first kind should be given this. The direction is read off the rendered document, so it follows
    /// an ancestor's <c>dir</c> as well as this component's own <see cref="BitComponentBase.Dir"/>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool FlipRtl { get; set; }

    /// <summary>
    /// Specifies the font size of the icon, as any CSS length or the <c>inherit</c> keyword.
    /// </summary>
    /// <remarks>
    /// This overrides <see cref="Size"/>, which is what an icon of one of the three sizes of the design
    /// system takes. Use it for the sizes in between, and for an icon that has to match whatever text
    /// it sits in (<c>FontSize="1em"</c> or <c>FontSize="inherit"</c>).
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? FontSize { get; set; }

    /// <summary>
    /// Specifies the icon configuration for rendering icons from external icon libraries using custom CSS classes.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property for external icon libraries such as FontAwesome, Bootstrap Icons, Material
    /// Icons or Material Symbols.
    /// For built-in Fluent UI icons, use the <see cref="IconName"/> property instead.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Specifies the name of the icon from the built-in Fluent UI icon library.
    /// This property is ignored when <see cref="Icon"/> is set.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? IconName { get; set; }

    /// <summary>
    /// The callback for when the icon is clicked.
    /// </summary>
    /// <remarks>
    /// An icon with a click handler becomes a control: it takes the tab order, answers Enter and Space
    /// as well as the pointer, and is announced as a button. It is still an icon and not a
    /// <see cref="BitButton"/> - there is no pressed state, no busy state and no label of its own - so
    /// give it an <see cref="BitComponentBase.AriaLabel"/> or a <see cref="Title"/> saying what it
    /// does, and reach for a button whenever the action deserves one.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Turns the icon by a quarter, a half, or three quarters of a turn.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconRotate? Rotate { get; set; }

    /// <summary>
    /// Specifies the size of the icon.
    /// Default value is <see cref="BitSize.Medium"/>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// The text shown in the native tooltip when the pointer rests on the icon.
    /// </summary>
    /// <remarks>
    /// A title also names the icon for assistive technology, so an icon that carries one is announced
    /// rather than skipped. Prefer <see cref="BitComponentBase.AriaLabel"/> when the name is meant to
    /// be read but not shown, and a <see cref="BitTooltip"/> when the text is meant to be seen - a
    /// native tooltip never appears for a keyboard or a touch user.
    /// </remarks>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Specifies the visual styling variant of the icon.
    /// Default value is <see cref="BitVariant.Text"/>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    protected override string RootElementClass => "bit-ico";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-ico-pri",
            BitColor.Secondary => "bit-ico-sec",
            BitColor.Tertiary => "bit-ico-ter",
            BitColor.Info => "bit-ico-inf",
            BitColor.Success => "bit-ico-suc",
            BitColor.Warning => "bit-ico-wrn",
            BitColor.SevereWarning => "bit-ico-swr",
            BitColor.Error => "bit-ico-err",
            BitColor.PrimaryBackground => "bit-ico-pbg",
            BitColor.SecondaryBackground => "bit-ico-sbg",
            BitColor.TertiaryBackground => "bit-ico-tbg",
            BitColor.PrimaryForeground => "bit-ico-pfg",
            BitColor.SecondaryForeground => "bit-ico-sfg",
            BitColor.TertiaryForeground => "bit-ico-tfg",
            BitColor.PrimaryBorder => "bit-ico-pbr",
            BitColor.SecondaryBorder => "bit-ico-sbr",
            BitColor.TertiaryBorder => "bit-ico-tbr",
            _ => "bit-ico-pri"
        });

        ClassBuilder.Register(() => BitIconInfo.From(Icon, IconName)?.GetCssClasses());

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-ico-sm",
            BitSize.Medium => "bit-ico-md",
            BitSize.Large => "bit-ico-lg",
            _ => "bit-ico-md"
        });

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-ico-fil",
            BitVariant.Outline => "bit-ico-out",
            BitVariant.Text => "bit-ico-txt",
            _ => "bit-ico-txt"
        });

        ClassBuilder.Register(() => FixedWidth ? "bit-ico-fxw" : string.Empty);

        ClassBuilder.Register(() => Rotate switch
        {
            BitIconRotate.Rotate90 => "bit-ico-rt90",
            BitIconRotate.Rotate180 => "bit-ico-rt180",
            BitIconRotate.Rotate270 => "bit-ico-rt270",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Flip switch
        {
            BitIconFlip.Horizontal => "bit-ico-flh",
            BitIconFlip.Vertical => "bit-ico-flv",
            BitIconFlip.Both => "bit-ico-flb",
            _ => string.Empty
        });

        ClassBuilder.Register(() => FlipRtl ? "bit-ico-frt" : string.Empty);

        // Rotation and mirroring are two halves of one transform, so the class that draws it is
        // registered once for whichever of them is in play - two rules each setting `transform`
        // would leave only the later one standing.
        ClassBuilder.Register(() => Rotate.HasValue || Flip.HasValue || FlipRtl ? "bit-ico-trn" : string.Empty);

        ClassBuilder.Register(() => Animation switch
        {
            BitIconAnimation.Spin => "bit-ico-spn",
            BitIconAnimation.SpinReverse => "bit-ico-spr",
            BitIconAnimation.Pulse => "bit-ico-pls",
            BitIconAnimation.Beat => "bit-ico-bet",
            BitIconAnimation.Fade => "bit-ico-fad",
            BitIconAnimation.Shake => "bit-ico-shk",
            _ => string.Empty
        });

        ClassBuilder.Register(() => OnClick.HasDelegate ? "bit-ico-int" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => FontSize.HasValue() ? $"font-size:{FontSize}" : string.Empty);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitIconParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    /// <summary>
    /// Gives focus to the icon element.
    /// </summary>
    /// <remarks>
    /// Only an icon the browser can focus takes it: one with an <see cref="OnClick"/> handler, or one
    /// given a <see cref="BitComponentBase.TabIndex"/> of its own. A decorative icon is not a tab stop
    /// and this does nothing to it.
    /// </remarks>
    /// <returns>
    /// A ValueTask that represents the asynchronous focus operation.
    /// </returns>
    public ValueTask FocusAsync() => RootElement.FocusAsync();



    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);
    }

    /// <summary>
    /// An icon that answers the pointer has to answer the keyboard as well: a native button is
    /// activated by Enter and by Space, and an element merely given the button role earns neither for
    /// free. Enter fires on the way down and Space on the way up, which is what a real button does and
    /// what lets a reader who pressed Space by mistake move off the icon before releasing it.
    /// </summary>
    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        if (e.Key != "Enter") return;

        await OnClick.InvokeAsync(ToActivationArgs(e));
    }

    private async Task HandleOnKeyUp(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        if (e.Key != " " && e.Key != "Spacebar" && e.Code != "Space") return;

        await OnClick.InvokeAsync(ToActivationArgs(e));
    }

    // A click that came from a key carries no pointer: a detail of 0 is how the platform itself marks
    // one, and the modifiers are the only thing the keystroke has to pass on.
    private static MouseEventArgs ToActivationArgs(KeyboardEventArgs e) => new()
    {
        Detail = 0,
        AltKey = e.AltKey,
        CtrlKey = e.CtrlKey,
        MetaKey = e.MetaKey,
        ShiftKey = e.ShiftKey
    };
}
