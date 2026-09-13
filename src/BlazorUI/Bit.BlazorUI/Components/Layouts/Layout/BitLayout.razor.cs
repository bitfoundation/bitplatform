using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The BitLayout component builds the base UI structure of an application: a header, a footer and a middle row
/// that holds a nav panel, the main content and an aside.
/// </summary>
/// <remarks>
/// Every section renders the HTML element that carries its landmark role - <c>header</c>, <c>nav</c>, <c>main</c>,
/// <c>aside</c> and <c>footer</c> - so a screen reader can jump straight to any of them, and an optional skip link
/// (<see cref="SkipLink"/>) gives a keyboard user the same shortcut to the content.
/// <br />
/// The layout is only as tall as its content, which lets it be dropped into a box of any size; <see cref="FullHeight"/>
/// makes it fill the viewport instead, so the footer of a short page still sits at the bottom of the screen. The header,
/// the footer, the nav panel and the aside can each be pinned while the rest of the page scrolls
/// (<see cref="StickyHeader"/>, <see cref="StickyFooter"/>, <see cref="StickyNavPanel"/>, <see cref="StickyAside"/>),
/// or the chrome can be kept still and only the content scrolled (<see cref="ScrollableMain"/>), and every section can
/// be hidden without taking the layout apart.
/// <br />
/// The header runs across the top of everything by default, which is the shape of a web page;
/// <see cref="FullHeightPanels"/> gives the shape of an application instead, with the panels running down the full
/// height and the header between them.
/// </remarks>
public partial class BitLayout : BitComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the layout component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple layout components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitLayoutParams.ParamName)]
    public BitLayoutParams? CascadingParameters { get; set; }



    /// <summary>
    /// The content of the aside section, which sits next to the main content on the side opposite the nav panel.
    /// </summary>
    /// <remarks>
    /// It renders a semantic <c>aside</c> element - the complementary landmark - which is where a table of contents,
    /// a filter panel or an inspector belongs, next to the main content but not part of it.
    /// <br />
    /// The section is left out of the markup entirely while this is null or <see cref="HideAside"/> is on.
    /// </remarks>
    [Parameter] public RenderFragment? Aside { get; set; }

    /// <summary>
    /// Gets or sets the accessible label of the aside section, used by assistive technologies.
    /// </summary>
    /// <remarks>
    /// A page can hold more than one complementary landmark, and a label is what tells them apart in the landmark
    /// list of a screen reader. The word "complementary" is announced already, so the label should not repeat it.
    /// </remarks>
    [Parameter] public string? AsideAriaLabel { get; set; }

    /// <summary>
    /// The width of the aside section in pixels.
    /// </summary>
    /// <remarks>
    /// This is the room the main content leaves for the aside, not a width forced onto the aside itself: the aside
    /// keeps sizing itself to its own content, which is what lets it be pinned, collapsed or slid out of the flow
    /// without the layout reserving a gap where it used to be.
    /// <br />
    /// When not set (or set to 0), the main content spans the full width of the row and the two share it as flex items.
    /// <br />
    /// Under <see cref="FullHeightPanels"/> the aside is a column of its own rather than a flex item beside the main
    /// content, so the width is applied to the aside itself.
    /// </remarks>
    [Parameter] public int AsideWidth { get; set; }

    /// <summary>
    /// Draws divider lines between the sections of the BitLayout.
    /// </summary>
    /// <remarks>
    /// The header is separated from the content below it, the footer from the content above it, and the nav panel
    /// and the aside from the main content beside them. The two vertical dividers follow the reading direction and
    /// swap sides along with <see cref="ReverseNavPanel"/>, so they always sit between a panel and the main content.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Bordered { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitLayout.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitLayoutClassStyles? Classes { get; set; }

    /// <summary>
    /// The content of the footer section.
    /// </summary>
    /// <remarks>
    /// It renders a semantic <c>footer</c> element, which is the contentinfo landmark of the page as long as the
    /// layout itself is not nested inside an article, a section, an aside or a nav.
    /// <br />
    /// The section is left out of the markup entirely while this is null or <see cref="HideFooter"/> is on.
    /// </remarks>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// The height of the footer section in pixels.
    /// </summary>
    /// <remarks>
    /// The height includes the paddings and the border of the footer, which is a border-box.
    /// <br />
    /// When not set, the footer is as tall as its own content.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? FooterHeight { get; set; }

    /// <summary>
    /// Makes the BitLayout fill at least the height of the viewport.
    /// </summary>
    /// <remarks>
    /// Without it the layout is as tall as its content and as tall as its parent allows, which is what lets it be
    /// dropped into a card, a dialog or a box of a fixed height. Turning it on is what a page level layout wants:
    /// the middle row grows into the leftover space, so the footer of a page with barely any content still sits at
    /// the bottom of the screen instead of half way up it.
    /// <br />
    /// The height follows the browser chrome of a mobile device as it retracts, so the layout is never taller than
    /// the part of the screen that is actually visible.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool FullHeight { get; set; }

    /// <summary>
    /// Makes the nav panel and the aside span the whole height of the BitLayout, with the header and the footer
    /// between them rather than above and below them.
    /// </summary>
    /// <remarks>
    /// The default layout is the one of a web page: the header runs across the top of everything and the panels
    /// sit under it. This is the one of an application: the panels run down the full height of the window, and
    /// the header, the main content and the footer share the column left between them - which is what a workspace
    /// with a permanent side navigation, an inspector or a tool rail looks like.
    /// <br />
    /// Since the panels are columns of their own here rather than flex items beside the main content,
    /// <see cref="NavPanelWidth"/> and <see cref="AsideWidth"/> are applied to the panels themselves, and
    /// <see cref="Gap"/> (a single CSS length in this mode) also separates the header and the footer from the
    /// main content.
    /// <br />
    /// The middle row is not a box of its own here either - its three sections are placed on the layout itself -
    /// so a background or a border given to it through <c>Styles.Main</c> or <c>Classes.Main</c> has nothing to paint.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool FullHeightPanels { get; set; }

    /// <summary>
    /// Gets or sets the space between the nav panel, the main content and the aside (the CSS gap of the middle row).
    /// </summary>
    /// <remarks>
    /// Takes any CSS length or the two value form of the gap shorthand (for example <c>1rem</c> or <c>0 16px</c>);
    /// under <see cref="FullHeightPanels"/> it is a single length, and it separates the header and the footer from
    /// the main content as well.
    /// <br />
    /// When not set, the three sections sit right next to each other, which is what a layout whose panels bring
    /// their own paddings or dividers wants.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Gap { get; set; }

    /// <summary>
    /// The content of the header section.
    /// </summary>
    /// <remarks>
    /// It renders a semantic <c>header</c> element, which is the banner landmark of the page as long as the layout
    /// itself is not nested inside an article, a section, an aside or a nav.
    /// <br />
    /// The section is left out of the markup entirely while this is null or <see cref="HideHeader"/> is on.
    /// </remarks>
    [Parameter] public RenderFragment? Header { get; set; }

    /// <summary>
    /// The height of the header section in pixels.
    /// </summary>
    /// <remarks>
    /// The height includes the paddings and the border of the header, which is a border-box.
    /// <br />
    /// It is also the offset a <see cref="StickyNavPanel"/> or a <see cref="StickyAside"/> pins itself at, so a panel
    /// pinned under a <see cref="StickyHeader"/> starts right below it instead of sliding underneath it.
    /// <br />
    /// When not set, the header is as tall as its own content and the pinned panels stick to the top of the viewport.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? HeaderHeight { get; set; }

    /// <summary>
    /// Hides the aside section.
    /// </summary>
    /// <remarks>
    /// The section is removed from the markup, so the main content takes the room it used to reserve for it.
    /// </remarks>
    [Parameter] public bool HideAside { get; set; }

    /// <summary>
    /// Hides the footer section.
    /// </summary>
    [Parameter] public bool HideFooter { get; set; }

    /// <summary>
    /// Hides the header section.
    /// </summary>
    [Parameter] public bool HideHeader { get; set; }

    /// <summary>
    /// Hides the nav panel section.
    /// </summary>
    /// <remarks>
    /// The section is removed from the markup, so the main content takes the room it used to reserve for it.
    /// This is what a page that has no navigation of its own - a landing page, a full screen editor - turns on,
    /// without the layout around it having to be built a second time.
    /// </remarks>
    [Parameter] public bool HideNavPanel { get; set; }

    /// <summary>
    /// The content of the main section.
    /// </summary>
    /// <remarks>
    /// It renders a semantic <c>main</c> element, the main landmark of the page and the target of the
    /// <see cref="SkipLink"/>. Unlike the other sections it is always rendered, since a page is expected to have
    /// exactly one of it.
    /// </remarks>
    [Parameter] public RenderFragment? Main { get; set; }

    /// <summary>
    /// Renders the main section as a plain element instead of the main landmark, for a BitLayout nested inside another one.
    /// </summary>
    /// <remarks>
    /// A page may hold only one main landmark, so an inner layout - a workspace inside the content of the page, a
    /// settings area with a nav panel of its own - would otherwise claim a second one. The section keeps its id, its
    /// classes and its place as the target of the <see cref="SkipLink"/>, and only stops being a landmark.
    /// <br />
    /// The other sections need nothing of the sort: a <c>header</c> or a <c>footer</c> stops being a banner or a
    /// contentinfo on its own once it sits inside the content of another one, and a page may hold any number of
    /// navigation and complementary landmarks.
    /// </remarks>
    [Parameter] public bool Nested { get; set; }

    /// <summary>
    /// The content of the nav panel section.
    /// </summary>
    /// <remarks>
    /// It renders a semantic <c>nav</c> element - the navigation landmark - so a screen reader can jump to the
    /// navigation of the application without walking through the content first.
    /// <br />
    /// The section is left out of the markup entirely while this is null or <see cref="HideNavPanel"/> is on.
    /// </remarks>
    [Parameter] public RenderFragment? NavPanel { get; set; }

    /// <summary>
    /// Gets or sets the accessible label of the nav panel section, used by assistive technologies.
    /// </summary>
    /// <remarks>
    /// A page usually holds more than one navigation landmark (the one in the header, the one in the footer, this
    /// one), and a label is what tells them apart in the landmark list of a screen reader. The word "navigation" is
    /// announced already, so the label should not repeat it.
    /// </remarks>
    [Parameter] public string? NavPanelAriaLabel { get; set; }

    /// <summary>
    /// The width of the nav panel section in pixels.
    /// </summary>
    /// <remarks>
    /// This is the room the main content leaves for the nav panel, not a width forced onto the panel itself: the
    /// panel keeps sizing itself to its own content, which is what lets it be pinned, collapsed to a rail or turned
    /// into an overlay drawer on a small screen without the layout reserving a gap where it used to be. A panel that
    /// leaves the flow that way is given a width of 0 here, and the main content spans the whole row again.
    /// <br />
    /// When not set (or set to 0), the main content spans the full width of the row and the two share it as flex items.
    /// <br />
    /// Under <see cref="FullHeightPanels"/> the panel is a column of its own rather than a flex item beside the main
    /// content, so the width is applied to the panel itself.
    /// </remarks>
    [Parameter] public int NavPanelWidth { get; set; }

    /// <summary>
    /// Gets or sets the padding around the content of the main section.
    /// </summary>
    /// <remarks>
    /// Takes any CSS padding value (for example <c>1rem</c> or <c>16px 24px</c>). The main section is a border-box,
    /// so the padding is taken out of the width it already has rather than added to it.
    /// <br />
    /// When not set, the main content spans its section edge to edge, which is what a page that brings its own
    /// container wants.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public string? Padding { get; set; }

    /// <summary>
    /// Reverses the position of the nav panel and the aside inside the middle row.
    /// </summary>
    /// <remarks>
    /// The row is laid out in the reading direction, so the nav panel starts on the left in an LTR layout and on the
    /// right in an RTL one. This flips that order, which puts the nav panel at the end of the row and the aside at
    /// its start.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool ReverseNavPanel { get; set; }

    /// <summary>
    /// Keeps the header and the footer in place and lets only the sections of the middle row scroll, each in a
    /// scrollport of its own.
    /// </summary>
    /// <remarks>
    /// This is the application shell: the chrome of the window never moves and the content under it scrolls, which
    /// is what a sticky section cannot do inside a box that does not scroll with the page. It needs the BitLayout to
    /// have a height to fill - <see cref="FullHeight"/>, or a parent of a definite height - since it is the leftover
    /// of that height that becomes the scrollport.
    /// <br />
    /// The nav panel and the aside scroll on their own too, so a panel with more items than fit stays reachable
    /// without the content beside it moving.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool ScrollableMain { get; set; }

    /// <summary>
    /// Renders a skip link as the first focusable element of the BitLayout, which jumps to the main section.
    /// </summary>
    /// <remarks>
    /// A keyboard or screen reader user otherwise has to walk through the whole header and nav panel on every page
    /// before reaching the content. The link stays out of sight until it is focused, so it costs a mouse user nothing.
    /// <br />
    /// The main section is given a tabindex of -1 while this is on, so it can actually take the focus the link sends it.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool SkipLink { get; set; }

    /// <summary>
    /// The text of the skip link. The default value is <strong>Skip to main content</strong>.
    /// </summary>
    /// <remarks>
    /// Only rendered while <see cref="SkipLink"/> is on. This is visible text, so it is also what a screen reader
    /// announces for the link.
    /// </remarks>
    [Parameter] public string? SkipLinkText { get; set; }

    /// <summary>
    /// Enables sticky positioning of the aside, so it stays in place while the main content scrolls past it.
    /// </summary>
    /// <remarks>
    /// The aside is pinned <see cref="HeaderHeight"/> pixels from the top of the viewport, which is what keeps it
    /// clear of a <see cref="StickyHeader"/>, and it is given the rest of the viewport height with its own scrollbar,
    /// so an aside taller than the screen is still reachable.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool StickyAside { get; set; }

    /// <summary>
    /// Enables sticky positioning of the footer at the bottom of the viewport.
    /// </summary>
    /// <remarks>
    /// Unlike a fixed footer, a sticky one stays in the normal flow, so it never overlaps the content and needs no
    /// extra room reserved for it. It requires a scrolling ancestor (commonly the page itself) and no ancestor with
    /// an overflow other than visible.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool StickyFooter { get; set; }

    /// <summary>
    /// Enables sticky positioning of the header at the top of the viewport.
    /// </summary>
    /// <remarks>
    /// Unlike a fixed header, a sticky one stays in the normal flow, so it never overlaps the content and needs no
    /// extra room reserved for it. It requires a scrolling ancestor (commonly the page itself) and no ancestor with
    /// an overflow other than visible.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool StickyHeader { get; set; }

    /// <summary>
    /// Enables sticky positioning of the nav panel, so it stays in place while the main content scrolls past it.
    /// </summary>
    /// <remarks>
    /// The panel is pinned <see cref="HeaderHeight"/> pixels from the top of the viewport, which is what keeps it
    /// clear of a <see cref="StickyHeader"/>, and it is given the rest of the viewport height with its own scrollbar,
    /// so a nav panel with more items than fit on the screen is still reachable.
    /// <br />
    /// A nav panel that already pins itself (as the BitNavPanel component does) needs none of this.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool StickyNavPanel { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitLayout.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitLayoutClassStyles? Styles { get; set; }

    /// <summary>
    /// Gets or sets the stacking order of the pinned sections of the BitLayout.
    /// </summary>
    /// <remarks>
    /// Only the sections that were pinned have a stacking order to speak of, so this is what decides whether a
    /// sticky header passes over or under the other layers of the application (a callout, a drawer, a dialog).
    /// <br />
    /// When not set, the pinned sections take the base z-index of the theme, which sits below all of them. The
    /// <see cref="SkipLink"/> is always drawn above them, since a focus that lands behind a pinned header is a
    /// focus nobody can see.
    /// </remarks>
    [Parameter, ResetStyleBuilder]
    public int? ZIndex { get; set; }



    /// <summary>
    /// The id of the main section, which is what the skip link points at.
    /// </summary>
    internal string _MainId => $"{_Id}-main";

    /// <summary>
    /// The inline style of the main section: the width it leaves for the panels beside it, followed by the custom
    /// style of the section. Null when there is neither, so no empty style attribute is rendered.
    /// </summary>
    /// <remarks>
    /// The main content is a flex item that grows, so the width is a starting width rather than a final one: a panel
    /// that took itself out of the flow (a drawer, an overlay) simply leaves the room unused and the main content
    /// grows back into it. Nothing is emitted when there is no room to reserve, which leaves the 100% of the stylesheet.
    /// </remarks>
    private string? _MainContentStyle
    {
        get
        {
            // Under FullHeightPanels the panels are columns of their own and carry their width themselves,
            // so there is nothing left for the main content to reserve.
            var reserved = FullHeightPanels
                            ? 0
                            : (_ShowNavPanel ? NavPanelWidth : 0) + (_ShowAside ? AsideWidth : 0);

            var width = reserved > 0 ? FormattableString.Invariant($"width:calc(100% - {reserved}px);") : string.Empty;

            var style = $"{width}{Styles?.MainContent}";

            return style.HasValue() ? style : null;
        }
    }

    /// <summary>
    /// The inline style of the nav panel: the width of its own column under <see cref="FullHeightPanels"/>, followed
    /// by the custom style of the section.
    /// </summary>
    private string? _NavPanelStyle => GetPanelStyle(NavPanelWidth, Styles?.NavPanel);

    /// <summary>
    /// The inline style of the aside: the width of its own column under <see cref="FullHeightPanels"/>, followed by
    /// the custom style of the section.
    /// </summary>
    private string? _AsideStyle => GetPanelStyle(AsideWidth, Styles?.Aside);

    /// <summary>
    /// Builds the inline style of a panel out of the width of its column and the custom style of the section.
    /// Null when there is neither, so no empty style attribute is rendered.
    /// </summary>
    /// <remarks>
    /// The width is only handed to the panel where the panel is a column of its own; everywhere else it is the room
    /// the main content leaves beside itself, and forcing it onto the panel would take away what lets a panel collapse
    /// to a rail or slide out of the flow on its own terms.
    /// </remarks>
    private string? GetPanelStyle(int panelWidth, string? customStyle)
    {
        var width = (FullHeightPanels && panelWidth > 0)
                    ? FormattableString.Invariant($"width:{panelWidth}px;")
                    : string.Empty;

        var style = $"{width}{customStyle}";

        return style.HasValue() ? style : null;
    }

    private bool _ShowHeader => Header is not null && HideHeader is false;
    private bool _ShowFooter => Footer is not null && HideFooter is false;
    private bool _ShowNavPanel => NavPanel is not null && HideNavPanel is false;
    private bool _ShowAside => Aside is not null && HideAside is false;



    protected override string RootElementClass => "bit-lyt";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        // The heights are handed to the sections as custom properties rather than as inline styles of their own,
        // so the header height is also readable by the sticky panels below it, which pin themselves at that offset.
        StyleBuilder.Register(() => HeaderHeight.HasValue
                                    ? FormattableString.Invariant($"--bit-lyt-hdr-h:{HeaderHeight}px")
                                    : string.Empty);

        StyleBuilder.Register(() => FooterHeight.HasValue
                                    ? FormattableString.Invariant($"--bit-lyt-ftr-h:{FooterHeight}px")
                                    : string.Empty);

        StyleBuilder.Register(() => Gap.HasValue() ? $"--bit-lyt-gap:{Gap}" : string.Empty);

        StyleBuilder.Register(() => Padding.HasValue() ? $"--bit-lyt-pad:{Padding}" : string.Empty);

        StyleBuilder.Register(() => ZIndex.HasValue
                                    ? FormattableString.Invariant($"--bit-lyt-zin:{ZIndex}")
                                    : string.Empty);
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FullHeight ? "bit-lyt-flh" : string.Empty);

        ClassBuilder.Register(() => FullHeightPanels ? "bit-lyt-fhp" : string.Empty);

        ClassBuilder.Register(() => ScrollableMain ? "bit-lyt-scm" : string.Empty);

        ClassBuilder.Register(() => StickyHeader ? "bit-lyt-shd" : string.Empty);
        ClassBuilder.Register(() => StickyFooter ? "bit-lyt-sft" : string.Empty);
        ClassBuilder.Register(() => StickyNavPanel ? "bit-lyt-snv" : string.Empty);
        ClassBuilder.Register(() => StickyAside ? "bit-lyt-sas" : string.Empty);

        ClassBuilder.Register(() => ReverseNavPanel ? "bit-lyt-rnv" : string.Empty);

        ClassBuilder.Register(() => Bordered ? "bit-lyt-brd" : string.Empty);
    }



    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitLayoutParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }
}
