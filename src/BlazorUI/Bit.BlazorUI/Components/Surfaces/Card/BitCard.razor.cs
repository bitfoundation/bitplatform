using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A Card provides a container to wrap around a specific content. Keeping a card to a single subject keeps the design clean.
/// </summary>
public partial class BitCard : BitComponentBase
{
    private string? _rel;
    private bool _buttonKeysRegistered;
    private ElementReference _stretchedRef;

    // Whether the card was sectioned, whether it was a control and whether that control was a button the last time
    // its classes were built. None of them is a parameter the generated setter can watch for: they are read off
    // templates and event callbacks, and a lambda or a template written in markup is a new delegate on every
    // render, so watching them there would rebuild the class string on every render of every card. They are
    // compared here instead.
    private (bool Sectioned, bool Interactive, bool Button)? _classState;

    // A card that leads somewhere gets a stretched anchor laid over it rather than becoming one: an anchor
    // that wraps the whole card would swallow every link and button inside it, which is neither valid HTML
    // nor reachable by a screen reader, and it would read the entire card out as the name of one link.
    private bool _IsLink => Href.HasValue();

    // Binding Selected turns the card into a toggle, the same way a click handler turns it into a button.
    private bool _IsToggle => SelectedChanged.HasDelegate;

    // Whether the card has a selected state to report at all. A bound Selected always has one, and so does a card
    // that was handed the state ready-made rather than letting the card flip it - a card drawing the selection ring
    // while saying nothing about it would be a control whose state only the sighted reader can see.
    private bool _IsSelectable => _IsToggle || Selected;

    private bool _IsClickable => OnClick.HasDelegate || _IsToggle;

    // A clickable card is a button only where there is no stretched link to be the control instead - two controls
    // over the same surface would be two tab stops on something the reader sees as a single thing.
    private bool _IsButton => _IsClickable && _IsLink is false;

    // The role the app splatted onto the root, read in one place so the markup and the checks below never disagree
    // about whether there is one. A blank role says nothing, and is treated as no role at all - which also puts the
    // role the card works out for itself in its place.
    private string? _SplattedRole => GetSplattedAttribute("role") is { } value && value.HasValue() ? value.Trim() : null;

    // Where that button lives. A card the app splatted a widget role onto - an option of a listbox, a tab of a tab
    // strip, a row of a grid - is that control itself, so the root takes the focus and the keys. Any other clickable
    // card lays a native button over its surface the way a linked card lays its anchor, rather than turning the root
    // into one: the children of a button are presentational, so the root as a button would bury the controls of its
    // Actions, Footer and FloatingActions inside another control and could never keep its title a heading. That
    // includes a card splatted with a structural role - a listitem, an article, a region, a group - which says what
    // the card is on the page rather than what it does, and has to keep the button to be a control at all.
    private bool _HasControlRole => _SplattedRole is "button" or "checkbox" or "columnheader" or "gridcell" or "link"
                                                  or "menuitem" or "menuitemcheckbox" or "menuitemradio" or "option"
                                                  or "radio" or "row" or "rowheader" or "switch" or "tab" or "treeitem";

    private bool _IsRootControl => _IsButton && _HasControlRole;

    private bool _IsStretchedButton => _IsButton && _HasControlRole is false;

    private bool _HasStretchedControl => _IsLink || _IsStretchedButton;

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
                            || IconTemplate is not null
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



    [Inject] private IJSRuntime _js { get; set; } = default!;



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
    /// <remarks>
    /// Leaving it unset paints the card in the <c>--bit-Card-background</c> variable, or in the secondary background
    /// of the theme where that is not set either; setting it wins over the variable.
    /// </remarks>
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
    /// over it. The picture carries no scrim unless it is given one through the <c>--bit-Card-scrim</c> variable, so
    /// give the card one - and a foreground to go with it, through <c>--bit-Card-color</c> - and give it a
    /// <see cref="Height"/> or a <see cref="MinHeight"/> - an overlaid cover is taken out of the flow and no
    /// longer makes the card as tall as the picture. It wins over <see cref="Horizontal"/>, which lays the same
    /// cover beside the content rather than behind it.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool CoverOverlay { get; set; }

    /// <summary>
    /// The aspect ratio the cover of the card is drawn at, as a CSS ratio such as <c>16 / 9</c>.
    /// </summary>
    /// <remarks>
    /// The cover is then sized by the ratio rather than by the picture inside it, and the picture fills it - which is
    /// what keeps a row of cards level whatever the pictures turn out to be, and keeps it level while the column
    /// narrows, which the fixed <see cref="ImageHeight"/> cannot do. It has nothing to size on a cover laid behind the
    /// content by <see cref="CoverOverlay"/>, which is as tall as the card is.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? CoverRatio { get; set; }

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
    /// Draws a hairline between the header and the body of the card and between its body and its footer.
    /// </summary>
    /// <remarks>
    /// It only means anything on a card that is laid out as a stack of parts, and a part on the outer edge of
    /// that stack - a header with nothing under it, a footer with nothing over it - draws no rule of its own.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Divider { get; set; }

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
    /// The content floated over the leading corner of the card, above everything else on its surface.
    /// </summary>
    /// <remarks>
    /// This is the slot for the control a picture card keeps in its corner - a favourite toggle, a selection box, a
    /// dismiss button - which <see cref="Actions"/> cannot hold on such a card, since the header it renders into sits
    /// under the cover rather than over it. It is raised above the stretched link of a card that has an
    /// <see cref="Href"/>, so the control in it stays clickable, and it floats over the card whether the card is laid
    /// out as a stack of parts or is still a plain padded box.
    /// </remarks>
    [Parameter] public RenderFragment? FloatingActions { get; set; }

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
    /// header. What does not survive is the <see cref="Title"/> and the <see cref="Subtitle"/> - a linked or
    /// clickable card with a header of its own therefore names its stretched link or button with its body, or with
    /// everything it says where it has no body, and wants a <see cref="BitComponentBase.AriaLabel"/> to say it
    /// shorter.
    /// </remarks>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// The heading level the title of the card reports itself as (1-6).
    /// </summary>
    /// <remarks>
    /// A card in a list of cards is usually a section of the page, and the title of such a section is a
    /// heading - which is what lets a screen reader user jump between the cards instead of reading through
    /// them. Leaving it unset keeps the title plain text, which is the right choice for a card whose title is
    /// only a label. Values outside 1-6 are ignored, and so is any value at all on a card splatted with a role
    /// that presents its children, such as an <c>option</c> or a <c>tab</c>: what such a role holds is read as the
    /// name of the control rather than as structure, so a heading in one is never reached and is left off the card
    /// entirely. A card made clickable through <see cref="OnClick"/> keeps it, since its button is stretched over
    /// the content rather than wrapped around it.
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
    /// in one of those two slots, and so is the text of the body, which a reader can no longer select with the
    /// pointer, nor scroll with the wheel on a card with <see cref="ScrollableBody"/> - the price the block-link
    /// pattern pays everywhere it is used. The anchor is named by <see cref="BitComponentBase.AriaLabel"/>, by an
    /// <c>aria-labelledby</c> splatted onto the card, by its <see cref="Title"/>, by its <see cref="Subtitle"/> or by
    /// its body, in that order, and failing all of those by everything the card says; the name is moved onto the
    /// anchor rather than left on the card as a second copy of itself.
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
    /// The custom template rendered in the leading slot of the header of the card, in place of the icon.
    /// </summary>
    /// <remarks>
    /// This is the slot for an avatar, a logo or a thumbnail that leads the header. Unlike
    /// <see cref="HeaderTemplate"/> it keeps the <see cref="Title"/> and the <see cref="Subtitle"/> beside it -
    /// and with them the name a linked card gives its stretched anchor.
    /// </remarks>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

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
    /// The part of the cover image of the card that is kept in frame, as a CSS object-position such as <c>top</c> or <c>50% 20%</c>.
    /// </summary>
    /// <remarks>
    /// A cover given an <see cref="ImageHeight"/> or a <see cref="CoverRatio"/> is a crop of the picture rather
    /// than the whole of it, and the crop is taken from the middle unless this says otherwise - which is what a
    /// portrait whose face sits at the top of the frame, or a landscape whose horizon sits at the bottom, needs.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? ImagePosition { get; set; }

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
    /// Setting it makes the card a button the way <see cref="Href"/> makes it a link: a native button is stretched
    /// over the surface, which takes the focus, answers Enter and Space, and is named by the <see cref="Title"/> and
    /// described by the <see cref="Subtitle"/> (or named by <see cref="BitComponentBase.AriaLabel"/>, or by the
    /// body, or failing all of those by everything the card says). The root stays a plain box, so its title can
    /// still be a heading and the controls in <see cref="Actions"/>, <see cref="Footer"/> and
    /// <see cref="FloatingActions"/> stay controls of their own; their clicks and key presses never fire this a
    /// second time, nor reach whatever holds the card. Unlike the anchor of a linked card, the button lets the
    /// pointer through: a click anywhere else on the card reaches the root and fires this, and the body still
    /// scrolls and its controls still answer the pointer - a click on one of them fires this as well, the same way
    /// Enter on one does, unless it stops the click itself. A card splatted with a widget <c>role</c> of its own - an
    /// <c>option</c>, a <c>tab</c>, a <c>row</c> - is that control itself instead: its root takes the focus and
    /// the keys. A structural role - a <c>listitem</c>, an <c>article</c> - keeps the button.
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
    /// A card whose <see cref="Target"/> is <c>_blank</c> gets <c>noopener</c> added to whatever this says,
    /// which is what protects the page from reverse tabnabbing - unless this already says what the opener
    /// relationship should be: <see cref="BitLinkRels.NoOpener"/> and <see cref="BitLinkRels.NoReferrer"/>
    /// close it already, and <see cref="BitLinkRels.Opener"/> is the one way to ask for the opener back, so it
    /// leaves the card's new tab able to reach this page.
    /// </remarks>
    [Parameter, CallOnSet(nameof(OnSetHrefAndRel))]
    public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// Lays the cover of the card after its content instead of before it.
    /// </summary>
    /// <remarks>
    /// A vertical card then wears its cover under the body rather than over the header, and a
    /// <see cref="Horizontal"/> one puts it on the trailing edge rather than the leading one. Only the order of the
    /// two changes; the cover is still full-bleed and still clipped to the corner of the card. It has nothing to
    /// reorder on a cover laid behind the content by <see cref="CoverOverlay"/>, which is not in the flow at all.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// Whether the card is currently selected.
    /// </summary>
    /// <remarks>
    /// Binding it turns the card into a toggle the same way <see cref="OnClick"/> turns it into a button:
    /// clicking it flips the value, and the button stretched over the card reports it through <c>aria-pressed</c>.
    /// A card that is a button through <see cref="OnClick"/> reports the state through <c>aria-pressed</c> as well
    /// while it carries it, even where the app flips the value itself rather than binding it. A card splatted with a role that carries
    /// a selection of its own - <c>option</c>, <c>row</c>, <c>gridcell</c>, <c>tab</c>, <c>treeitem</c>,
    /// <c>columnheader</c>, <c>rowheader</c> - reports it through <c>aria-selected</c> instead, which is the state
    /// those roles answer to, and one splatted with <c>checkbox</c>, <c>switch</c>, <c>radio</c>,
    /// <c>menuitemcheckbox</c> or <c>menuitemradio</c> through <c>aria-checked</c>. A card with none of those roles
    /// has only the ring to show for it.
    /// </remarks>
    [Parameter, ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool Selected { get; set; }

    /// <summary>
    /// Lets the content of the card scroll inside it instead of growing past the height it was given.
    /// </summary>
    /// <remarks>
    /// It only means anything on a card whose height is bounded - by <see cref="Height"/>, by
    /// <see cref="MaxHeight"/> or by <see cref="FullHeight"/> in a container of a known height - and it is what
    /// keeps the header and the footer of such a card in place while only the body between them moves. On a card
    /// that is still a plain padded box it is the box itself that scrolls, since there is no body to scroll instead.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool ScrollableBody { get; set; }

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
    /// <remarks>
    /// A <c>_blank</c> one adds <c>noopener</c> to the rel - see <see cref="Rel"/> for when it does not.
    /// </remarks>
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
    /// What takes the focus is whatever the card made focusable: the link or the button stretched over a linked
    /// or a clickable card, and the root of a card splatted with a role of its own or given a tab index of its
    /// own. A card that is none of those has nothing to focus.
    /// </remarks>
    public ValueTask FocusAsync() => _HasStretchedControl ? _stretchedRef.FocusAsync() : RootElement.FocusAsync();



    protected override string RootElementClass => "bit-crd";

    // The root of a clickable card splatted with a role of its own is the control, so it answers the keys a
    // button answers: Enter as the key goes down, Space as it comes back up, and neither one scrolling the page.
    // That is wired up in the browser, where a key pressed on the card itself can be told apart from one pressed
    // on a control it holds - which a Blazor keydown handler cannot see, nor prevent the default of per key. The
    // root is the same element for the lifetime of the card, so it is registered once, the first time the root is
    // a control; once registered it only ever turns a key pressed on the root into a click, and a root that
    // stopped being a control is no longer focusable for one to be pressed on. The stretched button of every other
    // clickable card is a native one and needs none of this.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_buttonKeysRegistered || _IsRootControl is false) return;

        _buttonKeysRegistered = true;

        try
        {
            await _js.BitUtilsRegisterButtonKeys(RootElement);
        }
        catch (JSDisconnectedException) { } // the circuit is gone, there is nothing left to answer a key
    }

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
            _ => string.Empty
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

        ClassBuilder.Register(() => Reversed ? "bit-crd-rev" : string.Empty);

        ClassBuilder.Register(() => Divider ? "bit-crd-dvd" : string.Empty);

        ClassBuilder.Register(() => CoverOverlay ? "bit-crd-ovl" : string.Empty);

        ClassBuilder.Register(() => CoverRatio.HasValue() ? "bit-crd-cra" : string.Empty);

        ClassBuilder.Register(() => _IsLink || _IsButton ? "bit-crd-int" : string.Empty);

        // A card that is a button - through its root or through the button stretched over it - is pressed through
        // the root itself, since that button lets the pointer through; a linked card only through its anchor.
        ClassBuilder.Register(() => _IsButton ? "bit-crd-btn" : string.Empty);

        ClassBuilder.Register(() => Hoverable ? "bit-crd-hov" : string.Empty);

        ClassBuilder.Register(() => Loading ? "bit-crd-ldg" : string.Empty);

        ClassBuilder.Register(() => ScrollableBody ? "bit-crd-scb" : string.Empty);

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

        StyleBuilder.Register(() => ImagePosition.HasNoValue() ? null : $"--bit-crd-img-position:{ImagePosition}");

        StyleBuilder.Register(() => CoverWidth.HasNoValue() ? null : $"--bit-crd-cvr-width:{CoverWidth}");

        StyleBuilder.Register(() => CoverRatio.HasNoValue() ? null : $"--bit-crd-cvr-ratio:{CoverRatio}");
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitCardParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        var classState = (_IsSectioned, _IsLink || _IsButton, _IsButton);
        if (_classState != classState)
        {
            _classState = classState;

            ClassBuilder.Reset();
        }

        base.OnParametersSet();
    }



    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (Disabled) return;

        // Only a card that is a button toggles. A linked card navigates away on the same click, and it is the
        // only kind whose pressed state is never reported - aria-pressed belongs to a button - so flipping
        // Selected there would change the card silently on its way off the page.
        if (_IsButton && _IsToggle)
        {
            await AssignSelected(Selected is false);
        }

        await OnClick.InvokeAsync(e);
    }

    internal void OnSetHrefAndRel()
    {
        // noopener protects against reverse-tabnabbing when opening the link in a new browsing context, so it
        // is added to whatever rel the card was given rather than left to it - a Rel of NoFollow alone is about
        // crawling and says nothing about the opener.
        _rel = BitNewTabUtils.ResolveRel(Href, Rel, Target);
    }
}
