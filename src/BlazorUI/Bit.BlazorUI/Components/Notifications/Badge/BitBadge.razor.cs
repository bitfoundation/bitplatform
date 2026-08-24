namespace Bit.BlazorUI;

/// <summary>
/// Badge component is a small visual element used to highlight or indicate specific information within a user interface.
/// </summary>
public partial class BitBadge : BitComponentBase
{
    private bool? _bump;
    private string? _content;
    private string? _rel;
    private bool _isZeroContent;
    private bool _isContentCapped;
    private string? _shownText;
    private bool _wasBadgeVisible;

    // A counter that ticks over while the page stays open answers the change with a short bump. A keyframe
    // animation only restarts when the class carrying it changes, so the two classes alternate; the class is
    // unset while the badge is off the page, which leaves a badge arriving on it to its entry animation
    // alone - the two are declared on the same element, and the bump would otherwise win the cascade for
    // the rest of the badge's life (see OnParametersSet).
    private string? _bumpClass => _bump switch { true => "bit-bdg-bm1", false => "bit-bdg-bm2", _ => null };

    // A glyph and a template are content of their own rather than a number, so an emptied counter next to
    // them is not what the badge is showing and must not take the badge off the page.
    private bool _hasOwnContent => Dot is false && (ContentTemplate is not null || Icon is not null || IconName.HasValue());

    // ShowZero takes an emptied counter off the badge; whatever else the badge holds stays on it.
    private bool _isZeroSuppressed => _isZeroContent && ShowZero is false;

    // A badge is on the page while it has something to report - a mark, a number, a glyph, a template or a
    // text alternative - so a badge given none of them is not rendered as an empty pill on top of its child.
    private bool _isBadgeVisible => Hidden is false
                                 && (_isZeroSuppressed is false || _hasOwnContent)
                                 && (Dot || _hasOwnContent || _content.HasValue() || Description.HasValue());

    // A badge that navigates or does something of its own is the control a screen reader lands on and the
    // element a keyboard user reaches, whichever of the two it is built from.
    private bool _isClickable => Href.HasValue() || OnClick.HasDelegate;

    // A live region only announces what changes inside it, so it has to be on the page before the change
    // happens: a region inserted together with its own text is announced by nothing. The badge comes and
    // goes with the count, so the region cannot live inside it - unless the badge is a button or a link,
    // which is focusable and therefore cannot be hidden from assistive technologies the way the rest of
    // the badge is. That case keeps the region where it is, and this one moves it out to the root.
    private bool _hasOwnLiveRegion => Live && _isClickable is false;

    // A template is markup rather than a count, so nothing can be read out of it in words: what a badge
    // showing one says is whatever the template renders, not the content sitting behind it.
    private bool _hasTemplateContent => Dot is false && ContentTemplate is not null;

    // What the badge stands for in words: the description when there is one, and the counter itself
    // otherwise - unless that counter is an emptied one, or one a template has taken the badge over from,
    // in which case it is not what the badge is showing.
    private string? _liveText => Description.HasValue()
                               ? Description
                               : (Dot || _isZeroSuppressed || _hasTemplateContent ? null : _content);

    // The region speaks for the badge, so the badge is hidden from assistive technologies while it does -
    // otherwise the count would reach a screen reader twice. A template is the one thing the region cannot
    // speak for, so a badge showing one keeps its own voice instead of being silenced for nothing.
    private bool _isBadgeMuted => _hasOwnLiveRegion && (Description.HasValue() || _hasTemplateContent is false);

    // What the badge is showing in words right now, which is what a change of it is worth a bump for: a
    // badge that is off the page, showing a template or showing an emptied counter is showing no text at all.
    private string? _shownContent => _isBadgeVisible && _hasTemplateContent is false && Dot is false && _isZeroSuppressed is false
                                   ? _content
                                   : null;

    // A cap is the one thing the badge shortens, so the figure behind it is spelled out on hover on its own -
    // a "99+" a reader cannot get the real count out of is the whole reason the tooltip is worth having. A
    // Title of its own always wins, and a badge showing its content in full has nothing left to reveal.
    private string? _titleText => Title ?? (_isContentCapped && _shownContent is not null ? Content?.ToString() : null);



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
    /// A number is capped by <see cref="Max"/> and, when it is zero, hidden by <see cref="ShowZero"/>.
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
    /// <br />
    /// It is markup rather than words, so nothing can be read out of it in text either: a template badge that
    /// is also <see cref="Live"/> needs a <see cref="Description"/> for the live region to have anything to
    /// announce.
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
    /// The URL the badge navigates to, which also turns the badge into a link.
    /// </summary>
    /// <remarks>
    /// A badge that leads somewhere is a real anchor: it is focusable, it is activated with the Enter key,
    /// it offers the context menu and the middle click every link on the page offers, and a screen reader
    /// announces it as a link. Use it for a counter that opens what it counts - an inbox, a cart, a list of
    /// alerts - and <see cref="OnClick"/> for one that acts on the page it is already on. The two can be set
    /// together, in which case the handler runs and the navigation still happens.
    /// <br />
    /// While <c>IsEnabled</c> is false the href is dropped and the badge is taken out of the tab order, so a
    /// disabled link cannot be followed by either the pointer or the keyboard.
    /// </remarks>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public string? Href { get; set; }

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
    /// <br />
    /// What the region reads out is the <see cref="Description"/> when there is one and the counter itself
    /// otherwise. A <see cref="ContentTemplate"/> is markup neither of them can be read out of, so a badge
    /// showing one keeps its own voice and needs a <see cref="Description"/> for the region to say anything.
    /// </remarks>
    [Parameter] public bool Live { get; set; }

    /// <summary>
    /// Max value to display when content is a number.
    /// </summary>
    /// <remarks>
    /// A content above it renders as the max followed by a plus sign, for example <c>99+</c>. It reads every
    /// numeric type, integral or fractional, and leaves everything else the badge is given untouched.
    /// <br />
    /// A capped badge is the one a reader cannot get the real figure out of, so it carries that figure as its
    /// tooltip unless a <see cref="Title"/> of its own says something better.
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
    /// as soon as <c>IsEnabled</c> is false. A badge with no handler and no <see cref="Href"/> never takes
    /// focus: it is a label on the element it belongs to, and that element is what a keyboard user reaches.
    /// <br />
    /// A control needs a name, so a badge that carries no text of its own - a <see cref="Dot"/> or an
    /// icon-only badge - should be given a <see cref="Description"/> or an <c>AriaLabel</c> as soon as it
    /// becomes one.
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
    /// The relationship between the current document and the linked one, rendered as the rel attribute of
    /// the anchor the badge becomes while <see cref="Href"/> is set.
    /// </summary>
    /// <remarks>
    /// With no value of its own, a badge opening in a new browsing context (<see cref="Target"/> of
    /// <c>_blank</c>) gets <c>rel="noopener"</c> on its own, which is what keeps the opened page from
    /// reaching back into this one.
    /// </remarks>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public BitLinkRels? Rel { get; set; }

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
    /// Only a numeric <see cref="Content"/> counts as zero, and a string is rendered as it is. An icon or a
    /// <see cref="ContentTemplate"/> is content of its own, so it keeps the badge on the page and only the
    /// emptied number is taken off it.
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
    /// The browsing context the <see cref="Href"/> of the badge is opened in, for example <c>_blank</c>.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetHrefAndRel))]
    public string? Target { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the badge.
    /// </summary>
    /// <remarks>
    /// It is rendered on the badge itself rather than on the child content underneath it, which is what makes
    /// it the place to spell out what the badge shortens: the exact count behind a <see cref="Max"/> of
    /// <c>99+</c>, or the reading behind an icon. A title is not a text alternative, so what a screen reader
    /// should hear still belongs in <see cref="Description"/>.
    /// <br />
    /// A badge showing a count its <see cref="Max"/> has capped spells that count out on hover on its own, so
    /// this is only needed when there is something better to say than the figure itself.
    /// </remarks>
    [Parameter] public string? Title { get; set; }

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
        _isContentCapped = false;

        if (Content is null)
        {
            _content = null;
        }
        else if (Content is string stringContent)
        {
            _content = stringContent;
        }
        else if (TryGetNumber(Content, out var number))
        {
            _isZeroContent = number == 0;
            _isContentCapped = Max.HasValue && number > Max.Value;

            // A capped count is reported as the max it went past; anything else is printed the way the value
            // itself prints, so a fraction keeps its separator and a localized digit set keeps its digits.
            _content = _isContentCapped ? $"{Max!.Value}+" : Content.ToString();
        }
        else
        {
            // Anything the badge cannot count is still something it can print, so it is rendered
            // through its own ToString() instead of silently dropping out of the badge.
            _content = Content.ToString();
        }
    }

    protected override void OnParametersSet()
    {
        // The bump is decided here rather than as the content is set, because what the badge ends up showing
        // is not settled until the whole batch of parameters has landed: ShowZero, Hidden, Dot and a template
        // each take the counter off the badge without ever going through the content setter.
        var isVisible = _isBadgeVisible;
        var shownContent = _shownContent;

        if (isVisible is false)
        {
            // A badge that is off the page comes back to it with the entry animation, so the bump is cleared
            // out of that animation's way rather than being left to win the cascade over it for good.
            _bump = null;
        }
        else if (IsRendered && _wasBadgeVisible && shownContent != _shownText)
        {
            // A keyframe animation only restarts when the class carrying it changes, so the two classes
            // alternate on every change of the text a badge that was already there is showing.
            _bump = _bump is not true;
        }

        _shownText = shownContent;
        _wasBadgeVisible = isVisible;

        base.OnParametersSet();
    }

    private void OnSetHrefAndRel()
    {
        if (Href.HasNoValue() || Href!.StartsWith('#'))
        {
            _rel = null;
            return;
        }

        if (Rel.HasValue)
        {
            _rel = BitLinkRelUtils.GetRels(Rel.Value);
            return;
        }

        // protects against reverse-tabnabbing when opening the link in a new browsing context
        _rel = Target == "_blank" ? "noopener" : null;
    }

    /// <summary>
    /// Reads any of the numeric types as a decimal, which is what <see cref="Max"/> and the zero check
    /// compare against. Anything else - and a floating-point value that is not a finite number a decimal can
    /// hold - is left to be rendered as plain text instead of being counted.
    /// </summary>
    private static bool TryGetNumber(object value, out decimal result)
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
            case ulong ulongValue: result = ulongValue; return true;
            case nint nintValue: result = nintValue; return true;
            case nuint nuintValue: result = nuintValue; return true;
            case decimal decimalValue: result = decimalValue; return true;
            case float floatValue: return TryGetFiniteNumber(floatValue, out result);
            case double doubleValue: return TryGetFiniteNumber(doubleValue, out result);
            default: result = 0; return false;
        }
    }

    /// <summary>
    /// Reads a floating-point value as a decimal while it is a finite number inside the range of one. A NaN,
    /// an infinity and a value beyond that range are not counts, so they are left to be printed as they are.
    /// </summary>
    private static bool TryGetFiniteNumber(double value, out decimal result)
    {
        if (double.IsFinite(value) && value > (double)decimal.MinValue && value < (double)decimal.MaxValue)
        {
            result = (decimal)value;
            return true;
        }

        result = 0;
        return false;
    }
}
