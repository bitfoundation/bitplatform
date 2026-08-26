using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A Card provides a container to wrap around a specific content. Keeping a card to a single subject keeps the design clean.
/// </summary>
public partial class BitCard : BitComponentBase
{
    private string? _rel;
    private ElementReference _linkRef;

    // Whether the card was sectioned and whether it was a control the last time its classes were built. Neither
    // is a parameter the generated setter can watch for: they are read off templates and event callbacks, and a
    // lambda or a template written in markup is a new delegate on every render, so watching them there would
    // rebuild the class string on every render of every card. They are compared here instead.
    private (bool Sectioned, bool Interactive)? _classState;

    // A card that leads somewhere gets a stretched anchor laid over it rather than becoming one: an anchor
    // that wraps the whole card would swallow every link and button inside it, which is neither valid HTML
    // nor reachable by a screen reader, and it would read the entire card out as the name of one link.
    private bool _IsLink => Href.HasValue();

    // Binding Selected turns the card into a toggle, the same way a click handler turns it into a button.
    private bool _IsToggle => SelectedChanged.HasDelegate;

    private bool _IsClickable => OnClick.HasDelegate || _IsToggle;

    // The root only becomes a control where there is no stretched link to be one instead - two controls over
    // the same surface would be two tab stops on something the reader sees as a single thing.
    private bool _IsButton => _IsClickable && _IsLink is false;

    // A card that carries any of the parts below is laid out as a stack of them, with the padding moved from
    // the root onto each part so that the cover can run edge to edge. A card that carries none of them keeps
    // the plain padded box it has always been.
    private bool _IsSectioned => _HasCover || _HasHeader || Footer is not null || Loading;

    private bool _HasCover => Cover is not null || ImageUrl.HasValue();

    private bool _HasMain => _HasHeader || Footer is not null || Loading || ChildContent is not null;

    private bool _HasHeader => HeaderTemplate is not null
                            || Title.HasValue()
                            || Subtitle.HasValue()
                            || Actions is not null
                            || Icon is not null
                            || IconName.HasValue();



    /// <summary>
    /// Gets or sets the cascading parameters for the card component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple card components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitCardParams.ParamName)]
    public BitCardParams? CascadingParameters { get; set; }



    /// <summary>
    /// The content rendered at the trailing edge of the header of the card.
    /// </summary>
    /// <remarks>
    /// This is the slot for whatever acts on the card as a whole - an overflow menu, a dismiss button, a
    /// status tag. It sits beside the title rather than inside it, so it can hold real controls, and it is
    /// raised above the stretched link of a card that has an <see cref="Href"/> so those controls stay clickable.
    /// </remarks>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>
    /// The color kind of the background of the card.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the border of the card.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// The content of the card.
    /// </summary>
    /// <remarks>
    /// It renders on its own inside the padding of the card, unless the card also has a cover, a header or a
    /// footer - then it renders as the body between them.
    /// </remarks>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the card.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitCardClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the card.
    /// </summary>
    /// <remarks>
    /// Setting it paints the card in one of the roles of the theme instead of in the neutral surface colors,
    /// in the way the <see cref="Variant"/> asks for. Leaving it unset keeps the card a plain surface, which
    /// is what <see cref="Background"/> and <see cref="Border"/> then paint.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The full-bleed media at the head of the card, rendered outside the padding and clipped to the corner of the card.
    /// </summary>
    /// <remarks>
    /// It takes precedence over <see cref="ImageUrl"/>, so a card that needs more than a single picture there
    /// - a carousel, a chart, a video - can render whatever it likes into the same place.
    /// </remarks>
    [Parameter] public RenderFragment? Cover { get; set; }

    /// <summary>
    /// Lays the cover of the card behind its content instead of above it, filling the whole surface.
    /// </summary>
    /// <remarks>
    /// This is the hero card: a picture the size of the card with the header, the body and the footer written
    /// over it. The picture carries no scrim of its own, so give the card a foreground it can be read against
    /// through <see cref="Color"/>, <see cref="BitComponentBase.Style"/> or <see cref="Styles"/>, and give it a
    /// <see cref="Height"/> or a <see cref="MinHeight"/> - an overlaid cover is taken out of the flow and no
    /// longer makes the card as tall as the picture. It wins over <see cref="Horizontal"/>, which lays the same
    /// cover beside the content rather than behind it.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool CoverOverlay { get; set; }

    /// <summary>
    /// The width of the cover of a horizontal card.
    /// </summary>
    /// <remarks>
    /// It only means anything while <see cref="Horizontal"/> is set, since a cover above the content is as wide
    /// as the card is. The default is a third of the width of the card.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? CoverWidth { get; set; }

    /// <summary>
    /// The download attribute of the stretched link of the card.
    /// </summary>
    /// <remarks>
    /// It only reaches the anchor a card lays over itself while <see cref="Href"/> is set.
    /// </remarks>
    [Parameter] public string? Download { get; set; }

    /// <summary>
    /// Sets the shadow elevation level of the card (0-24). Maps to theme shadow variables (--bit-shd-1 to --bit-shd-24).
    /// </summary>
    /// <remarks>
    /// 0 is a card with no shadow at all, which is the same thing <see cref="NoShadow"/> asks for; leaving it
    /// unset keeps the shadow the theme gives every card surface.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public int? Elevation { get; set; }

    /// <summary>
    /// The content rendered under the body of the card, outside its padding block.
    /// </summary>
    /// <remarks>
    /// This is the slot for the actions of the card - the buttons and links a reader is meant to act on. It is
    /// raised above the stretched link of a card that has an <see cref="Href"/>, so those controls stay
    /// clickable rather than being covered by it.
    /// </remarks>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// Makes the card height 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullHeight { get; set; }

    /// <summary>
    /// Makes the card width and height 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullSize { get; set; }

    /// <summary>
    /// Makes the card width 100% of its parent container.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The custom template rendered as the header of the card, in place of the icon, the title and the subtitle.
    /// </summary>
    /// <remarks>
    /// <see cref="Actions"/> still renders beside it, so the trailing controls of the header survive a custom
    /// header. What does not survive is the <see cref="Title"/> and the <see cref="Subtitle"/> - a card with an
    /// <see cref="Href"/> and a header of its own therefore has nothing left to name its stretched link with,
    /// and wants a <see cref="BitComponentBase.AriaLabel"/>.
    /// </remarks>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// The heading level the title of the card reports itself as (1-6).
    /// </summary>
    /// <remarks>
    /// A card in a list of cards is usually a section of the page, and the title of such a section is a
    /// heading - which is what lets a screen reader user jump between the cards instead of reading through
    /// them. Leaving it unset keeps the title plain text, which is the right choice for a card whose title is
    /// only a label. Values outside 1-6 are ignored.
    /// </remarks>
    [Parameter] public int? HeadingLevel { get; set; }

    /// <summary>
    /// Sets the height of the card explicitly.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Height { get; set; }

    /// <summary>
    /// The URL the whole card leads to, rendered as an anchor stretched over the surface of the card.
    /// </summary>
    /// <remarks>
    /// Everything in <see cref="Actions"/> and <see cref="Footer"/> stays above that anchor and keeps working;
    /// an interactive element anywhere else in the card is covered by it, so put the controls of a linked card
    /// in one of those two slots. The anchor is named by the <see cref="Title"/> of the card, or by
    /// <see cref="BitComponentBase.AriaLabel"/> where there is no title to name it.
    /// </remarks>
    [Parameter, CallOnSet(nameof(OnSetHrefAndRel)), ResetClassBuilder]
    public string? Href { get; set; }

    /// <summary>
    /// Lays the cover of the card beside its content instead of above it.
    /// </summary>
    /// <remarks>
    /// It only changes anything on a card that has a cover, a header or a footer - a card that is only a
    /// padded box has nothing to lay out.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Horizontal { get; set; }

    /// <summary>
    /// Lifts the card while the pointer is over it.
    /// </summary>
    /// <remarks>
    /// A clickable card or a linked one lifts on its own; this is for a card that reacts to the pointer
    /// without being a control itself.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Hoverable { get; set; }

    /// <summary>
    /// The leading icon of the header of the card.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the leading icon of the header of the card.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? IconName { get; set; }

    /// <summary>
    /// The alternate text of the cover image of the card.
    /// </summary>
    /// <remarks>
    /// A cover image is decorative next to the title beside it, so with no alt of its own it renders an empty
    /// one and is skipped by assistive technologies. Give it a value only where the picture says something the
    /// rest of the card does not.
    /// </remarks>
    [Parameter] public string? ImageAlt { get; set; }

    /// <summary>
    /// The height of the cover image of the card.
    /// </summary>
    /// <remarks>
    /// The image is cropped to fill it rather than stretched, so a row of cards keeps the same picture height
    /// whatever the aspect ratios of the pictures are.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? ImageHeight { get; set; }

    /// <summary>
    /// The loading behavior of the cover image of the card, eager or lazy.
    /// </summary>
    /// <remarks>
    /// A page of cards is the case lazy loading exists for: it holds the request for every picture that is not
    /// near the viewport yet. Leave it unset - or set it to eager - for the cards above the fold, whose pictures
    /// are what the reader is waiting for.
    /// </remarks>
    [Parameter] public BitImageLoading? ImageLoading { get; set; }

    /// <summary>
    /// The URL of the cover image at the head of the card.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Stands the body of the card in with a placeholder while its content is being fetched.
    /// </summary>
    /// <remarks>
    /// The header keeps rendering - the title of a card is known before its content is - and the root reports
    /// itself as busy so a screen reader knows the card is not finished. Use <see cref="LoadingTemplate"/> for
    /// a placeholder of your own.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Loading { get; set; }

    /// <summary>
    /// The custom placeholder rendered in the body of the card while <see cref="Loading"/> is set.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Sets the maximum height of the card.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? MaxHeight { get; set; }

    /// <summary>
    /// Sets the maximum width of the card.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? MaxWidth { get; set; }

    /// <summary>
    /// Sets the minimum height of the card.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? MinHeight { get; set; }

    /// <summary>
    /// Sets the minimum width of the card.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? MinWidth { get; set; }

    /// <summary>
    /// Removes the default padding of the card.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoPadding { get; set; }

    /// <summary>
    /// Removes the default shadow around the card.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoShadow { get; set; }

    /// <summary>
    /// The callback for when the card is clicked.
    /// </summary>
    /// <remarks>
    /// Setting it turns the card into a button: it takes focus, it answers Enter and Space, and it reports
    /// itself as a control to assistive technologies. A card that is a button should not hold controls of its
    /// own - use <see cref="Href"/> with <see cref="Actions"/> or <see cref="Footer"/> for a card that leads
    /// somewhere and still carries buttons.
    /// </remarks>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Renders the card with no shadow and a primary border.
    /// </summary>
    /// <remarks>
    /// It is a shorthand: an explicit <see cref="Border"/> still wins over the border color it asks for.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Outlined { get; set; }

    /// <summary>
    /// The rel attribute of the stretched link of the card.
    /// </summary>
    /// <remarks>
    /// With no value of its own, a card whose <see cref="Target"/> is <c>_blank</c> gets <c>noopener</c>,
    /// which is what protects the page from reverse tabnabbing.
    /// </remarks>
    [Parameter, CallOnSet(nameof(OnSetHrefAndRel))]
    public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// Whether the card is currently selected.
    /// </summary>
    /// <remarks>
    /// Binding it turns the card into a toggle the same way <see cref="OnClick"/> turns it into a button:
    /// clicking it flips the value, and the card reports its state through <c>aria-pressed</c>.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool Selected { get; set; }

    /// <summary>
    /// The size of the card, which sets its padding, the gap between its parts and the type of its header.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Removes the border-radius from the card, rendering it with sharp corners.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Square { get; set; }

    /// <summary>
    /// Stops the propagation of the click event of the card.
    /// </summary>
    [Parameter] public bool StopPropagation { get; set; }

    /// <summary>
    /// The second line of the header of the card, under the title.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? Subtitle { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the card.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitCardClassStyles? Styles { get; set; }

    /// <summary>
    /// The target attribute of the stretched link of the card.
    /// </summary>
    [Parameter, CallOnSet(nameof(OnSetHrefAndRel))]
    public string? Target { get; set; }

    /// <summary>
    /// The title of the card, rendered as the first line of its header.
    /// </summary>
    /// <remarks>
    /// It also names the stretched link of a card that has an <see cref="Href"/>, so a page full of linked
    /// cards reads as a list of the things they lead to rather than a list of identical links.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? Title { get; set; }

    /// <summary>
    /// The visual variant of the card, which only takes effect while a <see cref="Color"/> is set.
    /// </summary>
    /// <remarks>
    /// <strong>Fill</strong> paints the whole card in the role color, <strong>Outline</strong> keeps only the
    /// rule and the text in it, and <strong>Text</strong> drops the rule and the shadow too. The default is
    /// <strong>Fill</strong>.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }

    /// <summary>
    /// Sets the width of the card explicitly.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Width { get; set; }



    /// <summary>
    /// Gives focus to the card.
    /// </summary>
    /// <remarks>
    /// What takes the focus is whatever the card made focusable: the anchor stretched over a linked card, and
    /// the root of a card that is a button or that was given a tab index of its own. A card that is none of
    /// those has nothing to focus.
    /// </remarks>
    public ValueTask FocusAsync() => _IsLink ? _linkRef.FocusAsync() : RootElement.FocusAsync();



    protected override string RootElementClass => "bit-crd";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        // The background and the border classes carry a leading "b" so that the whole per-role vocabulary of
        // the theme (pbg, sbg, tbg, pbr, sbr, tbr, ...) stays free for the Color parameter below, the same way
        // BitDropMenu names the two apart.
        ClassBuilder.Register(() => Background switch
        {
            BitColorKind.Primary => "bit-crd-bpg",
            BitColorKind.Secondary => "bit-crd-bsg",
            BitColorKind.Tertiary => "bit-crd-btg",
            BitColorKind.Transparent => "bit-crd-brg",
            _ => "bit-crd-bsg"
        });

        ClassBuilder.Register(() => Border switch
        {
            BitColorKind.Primary => "bit-crd-brd bit-crd-bpr",
            BitColorKind.Secondary => "bit-crd-brd bit-crd-bsr",
            BitColorKind.Tertiary => "bit-crd-brd bit-crd-btr",
            BitColorKind.Transparent => "bit-crd-brd bit-crd-brr",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-crd-pri",
            BitColor.Secondary => "bit-crd-sec",
            BitColor.Tertiary => "bit-crd-ter",
            BitColor.Info => "bit-crd-inf",
            BitColor.Success => "bit-crd-suc",
            BitColor.Warning => "bit-crd-wrn",
            BitColor.SevereWarning => "bit-crd-swr",
            BitColor.Error => "bit-crd-err",
            BitColor.PrimaryBackground => "bit-crd-pbg",
            BitColor.SecondaryBackground => "bit-crd-sbg",
            BitColor.TertiaryBackground => "bit-crd-tbg",
            BitColor.PrimaryForeground => "bit-crd-pfg",
            BitColor.SecondaryForeground => "bit-crd-sfg",
            BitColor.TertiaryForeground => "bit-crd-tfg",
            BitColor.PrimaryBorder => "bit-crd-pbr",
            BitColor.SecondaryBorder => "bit-crd-sbr",
            BitColor.TertiaryBorder => "bit-crd-tbr",
            _ => string.Empty
        });

        // The variant classes are named apart from the Outlined shorthand, which already holds bit-crd-otl.
        // A variant paints in a role color, so it only means anything once there is a role to paint in.
        ClassBuilder.Register(() => Color.HasValue is false ? string.Empty : Variant switch
        {
            BitVariant.Fill => "bit-crd-vfl",
            BitVariant.Outline => "bit-crd-vot",
            BitVariant.Text => "bit-crd-vtx",
            _ => "bit-crd-vfl"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-crd-sm",
            BitSize.Medium => "bit-crd-md",
            BitSize.Large => "bit-crd-lg",
            _ => "bit-crd-md"
        });

        ClassBuilder.Register(() => FullSize || FullHeight ? "bit-crd-fhe" : string.Empty);
        ClassBuilder.Register(() => FullSize || FullWidth ? "bit-crd-fwi" : string.Empty);

        ClassBuilder.Register(() => Elevation is >= 0 and <= 24 ? $"bit-crd-e{Elevation}" : string.Empty);

        ClassBuilder.Register(() => _IsSectioned ? "bit-crd-sct" : string.Empty);

        ClassBuilder.Register(() => Horizontal ? "bit-crd-hrz" : string.Empty);

        ClassBuilder.Register(() => CoverOverlay ? "bit-crd-ovl" : string.Empty);

        ClassBuilder.Register(() => _IsLink || _IsButton ? "bit-crd-int" : string.Empty);

        ClassBuilder.Register(() => Hoverable ? "bit-crd-hov" : string.Empty);

        ClassBuilder.Register(() => Loading ? "bit-crd-ldg" : string.Empty);

        ClassBuilder.Register(() => Selected ? $"bit-crd-sel {Classes?.Selected}" : string.Empty);

        ClassBuilder.Register(() => NoPadding ? "bit-crd-npd" : string.Empty);

        ClassBuilder.Register(() => NoShadow ? "bit-crd-nsd" : string.Empty);

        ClassBuilder.Register(() => Outlined ? "bit-crd-otl" : string.Empty);

        ClassBuilder.Register(() => Square ? "bit-crd-sqr" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Selected ? Styles?.Selected : string.Empty);

        StyleBuilder.Register(() => Height.HasNoValue() ? null : $"height:{Height}");

        StyleBuilder.Register(() => Width.HasNoValue() ? null : $"width:{Width}");

        StyleBuilder.Register(() => MinHeight.HasNoValue() ? null : $"min-height:{MinHeight}");

        StyleBuilder.Register(() => MinWidth.HasNoValue() ? null : $"min-width:{MinWidth}");

        StyleBuilder.Register(() => MaxHeight.HasNoValue() ? null : $"max-height:{MaxHeight}");

        StyleBuilder.Register(() => MaxWidth.HasNoValue() ? null : $"max-width:{MaxWidth}");

        StyleBuilder.Register(() => ImageHeight.HasNoValue() ? null : $"--bit-crd-img-height:{ImageHeight}");

        StyleBuilder.Register(() => CoverWidth.HasNoValue() ? null : $"--bit-crd-cvr-width:{CoverWidth}");
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitCardParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        var classState = (_IsSectioned, _IsLink || _IsButton);
        if (_classState != classState)
        {
            _classState = classState;

            ClassBuilder.Reset();
        }

        base.OnParametersSet();
    }



    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        // Only a card that is a button toggles. A linked card navigates away on the same click, and it is the
        // only kind whose pressed state is never reported - aria-pressed belongs to a button - so flipping
        // Selected there would change the card silently on its way off the page.
        if (_IsButton && _IsToggle)
        {
            await AssignSelected(Selected is false);
        }

        await OnClick.InvokeAsync(e);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        // Enter and Space are what a button answers, and the root of a clickable card is one in everything but
        // its tag name. Neither key is prevented: the root is a div rather than a button, so preventing the
        // default here would also prevent it for whatever the card holds.
        if (e.Key is not ("Enter" or " " or "Spacebar")) return;

        await HandleOnClick(new MouseEventArgs());
    }

    internal void OnSetHrefAndRel()
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

        // protects against reverse-tabnabbing when opening the link in a new browsing context. The target
        // attribute is matched case-insensitively by the browser, so a "_BLANK" opens the same new context
        // and has to be recognized here as one.
        _rel = string.Equals(Target, "_blank", StringComparison.OrdinalIgnoreCase) ? "noopener" : null;
    }
}
