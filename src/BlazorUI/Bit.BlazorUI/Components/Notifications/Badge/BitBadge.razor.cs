namespace Bit.BlazorUI;

/// <summary>
/// Badge component is a small visual element used to highlight or indicate specific information within a user interface.
/// </summary>
public partial class BitBadge : BitComponentBase
{
    private string? _content;
    private bool _isZeroContent;

    // A template is content of its own, so a stale numeric Content of zero next to it is not what the badge
    // is showing and must not take the badge off the page.
    private bool _isBadgeVisible => Hidden is false && (ShowZero || ContentTemplate is not null || _isZeroContent is false);

    // A live region only announces what changes inside it, so it has to be on the page before the change
    // happens: a region inserted together with its own text is announced by nothing. The badge comes and
    // goes with the count, so the region cannot live inside it - unless the badge is a button, which is
    // focusable and therefore cannot be hidden from assistive technologies the way the rest of the badge is.
    // That case keeps the region where it is, and this one moves it out to the root.
    private bool _hasOwnLiveRegion => Live && OnClick.HasDelegate is false;

    // What the badge stands for in words: the description when there is one, and the counter itself otherwise.
    private string? _liveText => Description.HasValue() ? Description : (Dot ? null : _content);



    /// <summary>
    /// Draws a ring around the badge in the color of the page behind it, so it stays legible over a busy
    /// child such as an avatar or an image.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Bordered { get; set; }

    /// <summary>
    /// Child content of component, the content that the badge will apply to.
    /// </summary>
    /// <remarks>
    /// When it is not set the badge has nothing to overlay, so it renders standalone: in the normal flow
    /// of the page, at its own size, with <see cref="Position"/> and <see cref="Overlap"/> no longer applying.
    /// <br />
    /// To keep the child content and still lay the badge out beside it rather than over it, use <see cref="Inline"/>.
    /// </remarks>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitBadge.
    /// </summary>
    [Parameter] public BitBadgeClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the badge.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Content you want inside the badge.
    /// </summary>
    /// <remarks>
    /// An integral number is capped by <see cref="Max"/> and, when it is zero, hidden by <see cref="ShowZero"/>.
    /// A string is rendered as it is, and any other value is rendered through its <c>ToString()</c>.
    /// <br />
    /// For markup rather than text, use <see cref="ContentTemplate"/>.
    /// </remarks>
    [Parameter]
    [CallOnSet(nameof(OnSetContentAndMax))]
    public object? Content { get; set; }

    /// <summary>
    /// The custom template to render inside the badge, in place of <see cref="Content"/>.
    /// </summary>
    /// <remarks>
    /// A template is content of its own, so neither <see cref="Max"/> nor <see cref="ShowZero"/> reads it:
    /// the badge shows what the template renders and stays on the page while it is set.
    /// </remarks>
    [Parameter] public RenderFragment? ContentTemplate { get; set; }

    /// <summary>
    /// The text alternative of the badge for assistive technologies, for example "5 unread messages".
    /// </summary>
    /// <remarks>
    /// A badge conveys its meaning visually - through a number, a glyph or the color of a dot - and none of
    /// that reaches a screen reader on its own. This renders the given text into the badge, visible only to
    /// assistive technologies, and hides the visual content from them so the two are not announced twice.
    /// <br />
    /// It is what makes a <see cref="Dot"/> badge accessible at all, since a dot has no content to announce.
    /// </remarks>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Reduces the size of the badge and hide any of its content.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// Use a dot when the fact that something changed matters more than how much of it there is.
    /// Pair it with a <see cref="Description"/> so the change is not carried by color alone.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Dot { get; set; }

    /// <summary>
    /// The visibility of the badge.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// A hidden badge is removed from the DOM while its child content keeps rendering.
    /// </remarks>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: Icon="BitIconInfo.Bi(\"gear-fill\")"
    /// FontAwesome: Icon="BitIconInfo.Fa(\"solid house\")"
    /// Custom CSS: Icon="BitIconInfo.Css(\"my-icon-class\")"
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Emoji</c>).
    /// <br />
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Lays the badge out next to its child content in the normal flow of the page instead of over it.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// An overlaid badge has to stay small enough not to bury the element underneath it. An inline one is
    /// given room of its own, which is what a counter at the end of a navigation item or a status next to a
    /// heading is built from.
    /// <br />
    /// <see cref="Overlap"/> stops applying, and of <see cref="Position"/> only the side is read: the Start
    /// and Left families put the badge before the child content, every other one after it. <see cref="OffsetX"/>
    /// and <see cref="OffsetY"/> keep nudging the badge from wherever the row leaves it.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Inline { get; set; }

    /// <summary>
    /// Announces the badge to assistive technologies whenever its content changes, by turning it into a
    /// polite live region.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// Turn it on for a count that updates while the page stays open (an inbox, a cart) and off for one that
    /// only reflects what is already on the screen, since every change of a live region interrupts the reader.
    /// <br />
    /// The region is kept on the page whether or not the badge itself is, so a counter that appears, changes
    /// and disappears is announced every time, and the badge is hidden from assistive technologies while it
    /// is on so nothing is announced twice. A badge that is a button keeps the region inside itself instead,
    /// since a focusable element cannot be hidden from a screen reader.
    /// </remarks>
    [Parameter] public bool Live { get; set; }

    /// <summary>
    /// Max value to display when content is an integral number.
    /// </summary>
    /// <remarks>
    /// A content above it renders as the max followed by a plus sign, for example <c>99+</c>.
    /// </remarks>
    [Parameter]
    [CallOnSet(nameof(OnSetContentAndMax))]
    public int? Max { get; set; }

    /// <summary>
    /// Moves the badge along the horizontal axis by the given CSS length, on top of its <see cref="Position"/>.
    /// </summary>
    /// <remarks>
    /// A positive value moves the badge to the right in both directions of writing, since the offset is a
    /// nudge for a specific child rather than a part of the layout.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? OffsetX { get; set; }

    /// <summary>
    /// Moves the badge along the vertical axis by the given CSS length, on top of its <see cref="Position"/>.
    /// A positive value moves the badge down.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? OffsetY { get; set; }

    /// <summary>
    /// The click event of the badge, which also turns the badge into a button.
    /// </summary>
    /// <remarks>
    /// While it is set the badge is focusable and can be activated with the keyboard, and it stops being so
    /// as soon as <c>IsEnabled</c> is false. A badge with no handler never takes focus: it is a label on the
    /// element it belongs to, and that element is what a keyboard user reaches.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Overlaps the badge on top of the child content.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// Turn it on for a child with a rounded outline, such as an avatar, where a badge sitting on the bounding
    /// box leaves a visible gap.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Overlap { get; set; }

    /// <summary>
    /// The position of the badge.
    /// </summary>
    /// <remarks>
    /// The Left/Right positions are physical and stay where they are in right-to-left, while the Start/End
    /// ones follow the direction of writing.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitPosition? Position { get; set; }

    /// <summary>
    /// Renders an expanding ring around the badge to report that something is in progress.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The ring is decorative: it slows down rather than stops under a reduced-motion preference, and it
    /// carries no meaning of its own, so pair it with a <see cref="Description"/> when the state matters.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Pulse { get; set; }

    /// <summary>
    /// Reverses the direction flow of the content of the badge, which puts the icon after the content.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// The corner shape of the badge.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitBadgeShape? Shape { get; set; }

    /// <summary>
    /// Renders the badge when its content is the number zero.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    /// <remarks>
    /// Turn it off for a counter that should disappear once it is emptied, which saves keeping a
    /// <see cref="Hidden"/> flag of your own next to the count.
    /// <br />
    /// Only an integral <see cref="Content"/> counts as zero: a string is rendered as it is, and a
    /// <see cref="ContentTemplate"/> is content of its own that keeps the badge on the page either way.
    /// </remarks>
    [Parameter] public bool ShowZero { get; set; } = true;

    /// <summary>
    /// The size of badge, Possible values: Small | Medium | Large
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitBadge.
    /// </summary>
    [Parameter] public BitBadgeClassStyles? Styles { get; set; }

    /// <summary>
    /// The visual variant of the badge.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    protected override string RootElementClass => "bit-bdg";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-bdg-pri",
            BitColor.Secondary => "bit-bdg-sec",
            BitColor.Tertiary => "bit-bdg-ter",
            BitColor.Info => "bit-bdg-inf",
            BitColor.Success => "bit-bdg-suc",
            BitColor.Warning => "bit-bdg-wrn",
            BitColor.SevereWarning => "bit-bdg-swr",
            BitColor.Error => "bit-bdg-err",
            BitColor.PrimaryBackground => "bit-bdg-pbg",
            BitColor.SecondaryBackground => "bit-bdg-sbg",
            BitColor.TertiaryBackground => "bit-bdg-tbg",
            BitColor.PrimaryForeground => "bit-bdg-pfg",
            BitColor.SecondaryForeground => "bit-bdg-sfg",
            BitColor.TertiaryForeground => "bit-bdg-tfg",
            BitColor.PrimaryBorder => "bit-bdg-pbr",
            BitColor.SecondaryBorder => "bit-bdg-sbr",
            BitColor.TertiaryBorder => "bit-bdg-tbr",
            _ => "bit-bdg-pri"
        });

        ClassBuilder.Register(() => Dot ? "bit-bdg-dot" : string.Empty);

        ClassBuilder.Register(() => Inline ? "bit-bdg-inl" : string.Empty);

        ClassBuilder.Register(() => Overlap ? "bit-bdg-orp" : string.Empty);

        ClassBuilder.Register(() => Bordered ? "bit-bdg-brd" : string.Empty);

        ClassBuilder.Register(() => Pulse ? "bit-bdg-pls" : string.Empty);

        ClassBuilder.Register(() => Reversed ? "bit-bdg-rvs" : string.Empty);

        ClassBuilder.Register(() => Position switch
        {
            BitPosition.TopLeft => "bit-bdg-tlf",
            BitPosition.TopCenter => "bit-bdg-tcr",
            BitPosition.TopRight => "bit-bdg-trg",
            BitPosition.TopStart => "bit-bdg-tst",
            BitPosition.TopEnd => "bit-bdg-ten",
            BitPosition.CenterLeft => "bit-bdg-clf",
            BitPosition.Center => "bit-bdg-ctr",
            BitPosition.CenterRight => "bit-bdg-crg",
            BitPosition.CenterStart => "bit-bdg-cst",
            BitPosition.CenterEnd => "bit-bdg-cen",
            BitPosition.BottomLeft => "bit-bdg-blf",
            BitPosition.BottomCenter => "bit-bdg-bcr",
            BitPosition.BottomRight => "bit-bdg-brg",
            BitPosition.BottomStart => "bit-bdg-bst",
            BitPosition.BottomEnd => "bit-bdg-ben",
            // An overlaid badge has always landed on the physical top right by default and stays there. An
            // inline one reads only the side of the position, and a side that is left unsaid should follow
            // the direction of writing rather than pin the badge to the leading edge in right-to-left.
            _ => Inline ? "bit-bdg-ten" : "bit-bdg-trg"
        });

        ClassBuilder.Register(() => Shape switch
        {
            BitBadgeShape.Circular => "bit-bdg-cir",
            BitBadgeShape.Rounded => "bit-bdg-rnd",
            BitBadgeShape.Square => "bit-bdg-sqr",
            _ => "bit-bdg-cir"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-bdg-sm",
            BitSize.Medium => "bit-bdg-md",
            BitSize.Large => "bit-bdg-lg",
            _ => "bit-bdg-md"
        });

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-bdg-fil",
            BitVariant.Outline => "bit-bdg-otl",
            BitVariant.Text => "bit-bdg-txt",
            _ => "bit-bdg-fil"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => OffsetX.HasValue() ? $"--bit-bdg-ofs-x:{OffsetX}" : string.Empty);

        StyleBuilder.Register(() => OffsetY.HasValue() ? $"--bit-bdg-ofs-y:{OffsetY}" : string.Empty);
    }



    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);
    }

    private void OnSetContentAndMax()
    {
        _isZeroContent = false;

        if (Content is null)
        {
            _content = null;
        }
        else if (Content is string stringContent)
        {
            _content = stringContent;
        }
        else if (TryGetInteger(Content, out var number))
        {
            _isZeroContent = number == 0;

            _content = (Max.HasValue && number > Max.Value) ? $"{Max.Value}+" : number.ToString();
        }
        else
        {
            // Anything the badge cannot count is still something it can print, so it is rendered
            // through its own ToString() instead of silently dropping out of the badge.
            _content = Content.ToString();
        }
    }

    /// <summary>
    /// Reads any of the integral numeric types as a long, which is what <see cref="Max"/> and the zero check
    /// compare against. An unsigned value too large for a long is left to be rendered as plain text: it is
    /// beyond every max a badge can be given anyway.
    /// </summary>
    private static bool TryGetInteger(object value, out long result)
    {
        switch (value)
        {
            case int intValue: result = intValue; return true;
            case long longValue: result = longValue; return true;
            case short shortValue: result = shortValue; return true;
            case byte byteValue: result = byteValue; return true;
            case sbyte sbyteValue: result = sbyteValue; return true;
            case ushort ushortValue: result = ushortValue; return true;
            case uint uintValue: result = uintValue; return true;
            case ulong ulongValue when ulongValue <= long.MaxValue: result = (long)ulongValue; return true;
            case nint nintValue: result = nintValue; return true;
            case nuint nuintValue when nuintValue <= long.MaxValue: result = (long)nuintValue; return true;
            default: result = 0; return false;
        }
    }
}
