namespace Bit.Bmotion.Demo.Client.Shared;

/// <summary>
/// One entry of the docs navigation. The list itself is <see cref="All"/>, and it is the site's
/// single index: the nav panel groups it, the command palette searches it, the landing page prints
/// it, the breadcrumb reads a page's category out of it and the pager walks it in order. A page
/// therefore appears everywhere by having an entry here, and the five of them can never disagree
/// about what the site contains.
/// <para>
/// It is also where the demo's MCP server gets the description, keywords and route it reports for
/// a demo page in <c>GetBmotionSourceFiles</c>, which is why each entry carries them alongside the
/// file it is implemented in. A page therefore describes itself once, and the site's search, the
/// nav panel and an AI agent looking for a worked example all read the same words.
/// </para>
/// </summary>
/// <param name="Title">The label shown in the nav panel.</param>
/// <param name="Href">The route, relative to the app base. Empty for the landing page.</param>
/// <param name="Category">The group this page is filed under; see <see cref="Groups"/>.</param>
/// <param name="Description">What the page demonstrates, in one sentence.</param>
/// <param name="Keywords">Space-separated terms the page covers, for search and the MCP index.</param>
/// <param name="Source">The page's file name under Client/Pages, for GetBmotionSourceFile.</param>
public sealed record NavItem(
    string Title,
    string Href,
    string Category,
    string Description,
    string Keywords,
    string Source)
{
    /// <summary>The path the MCP server hands this page's source out under.</summary>
    public string SourcePath => $"Demo/Client/Pages/{Source}";

    /// <summary>The route as a link target, which for the landing page is the app root.</summary>
    public string Url => Href.Length == 0 ? "/" : $"/{Href}";

    /// <summary>The icon the palette and the landing page show beside this page's group.</summary>
    public string Icon => CategoryIcon(Category);

    // The group names, written out once so a typo in an entry below cannot silently invent a
    // twenty-first group, and in the order the docs are meant to be read.
    private const string GettingStarted = "Getting started";
    private const string Motion = "Motion";
    private const string Interaction = "Interaction";
    private const string Orchestration = "Orchestration";
    private const string LayoutAndScroll = "Layout and scroll";
    private const string Advanced = "Advanced";
    private const string Guides = "Guides";

    /// <summary>Every demo page, in the order they are meant to be read.</summary>
    public static readonly NavItem[] All =
    [
        new("Home", "", GettingStarted,
            "The landing page: what Bmotion is, and the feature tiles that link into the rest of the demos.",
            "home landing overview introduction start",
            "Home.razor"),
        new("Basics", "basic", GettingStarted,
            "Initial and Animate on a single element, the shape of every Bmotion animation, and what runs on mount.",
            "initial animate mount enter first render bmotion component transition basics hello world",
            "BasicAnimations.razor"),
        new("Springs", "springs", Motion,
            "Physics springs: stiffness and damping, the intuitive bounce and duration form, overshoot on mount, and repeating springs.",
            "spring stiffness damping mass bounce duration overshoot physics velocity repeat mirror",
            "Springs.razor"),
        new("Easing", "easing", Motion,
            "Every BmEase preset side by side, custom cubic beziers, stepped easing, and per-segment easing across keyframes.",
            "ease easing bezier cubic steps linear in out inout back elastic bounce circ expo quad quart quint sine",
            "Easing.razor"),
        new("Keyframes", "keyframes", Motion,
            "Multi-step animations from arrays of values, with Times for uneven spacing and Bm.Current as the wildcard first frame.",
            "keyframes array multi step times wildcard current sequence repeat mirror loop",
            "Keyframes.razor"),
        new("Gestures", "gestures", Interaction,
            "WhileHover, WhileTap, WhileFocus and WhileInView - animation states the element enters while something is true.",
            "hover tap press focus inview viewport gesture whilehover whiletap whilefocus whileinview intersection",
            "Gestures.razor"),
        new("Drag", "drag", Interaction,
            "Pointer dragging with axis locks, constraints, elastic edges, momentum on release and drag-driven values.",
            "drag pointer pan constraints elastic momentum inertia snap axis lock draggable",
            "DragPage.razor"),
        new("Reorder", "reorder", Interaction,
            "BmotionReorderGroup: a drag-to-reorder list where the other items animate out of the way as one is moved.",
            "reorder sortable list drag drop rearrange items order group",
            "ReorderPage.razor"),
        new("Variants", "variants", Orchestration,
            "Named animation states shared down a subtree, with orchestration: staggered children, before/after children, and propagation.",
            "variants named states orchestration stagger staggerchildren delaychildren when beforechildren afterchildren propagate parent child",
            "Variants.razor"),
        new("AnimatePresence", "presence", Orchestration,
            "Exit animations: how an element animates out before Blazor removes it, plus presence modes and switching between views.",
            "exit animate presence unmount remove leave mode wait popLayout sync switch group conditional rendering",
            "AnimatePresencePage.razor"),
        new("Split Text", "split-text", Orchestration,
            "BmotionSplitText breaking a headline into characters, words or lines so each piece can animate on its own stagger.",
            "split text characters words lines stagger headline typography reveal per letter",
            "SplitText.razor"),
        new("Layout", "layout", LayoutAndScroll,
            "FLIP layout animations: an element animating between two positions or sizes it was merely re-rendered into, and shared-element transitions.",
            "layout flip shared element magic move layoutid position size reposition transition morph",
            "LayoutPage.razor"),
        new("Scroll", "scroll", LayoutAndScroll,
            "Scroll-driven animation: progress-linked timelines that the browser scrubs, and enter-on-scroll reveals.",
            "scroll parallax progress timeline scrolltimeline viewtimeline reveal sticky offset viewport",
            "ScrollAnimations.razor"),
        new("View Transitions", "view-transitions", LayoutAndScroll,
            "The browser View Transitions API driven from C#: cross-fading a whole DOM update rather than one element.",
            "view transition startviewtransition cross fade page navigation document update",
            "ViewTransitions.razor"),
        new("Programmatic", "programmatic", Advanced,
            "The imperative API: animating by selector or ElementReference, motion values, sequences and playback controls.",
            "programmatic imperative animate service selector elementreference motion value bmvalue sequence timeline controls pause play stop speed",
            "Programmatic.razor"),
        new("Color", "color", Advanced,
            "Colour interpolation and why the space matters: sRGB against Oklab, LCH and HSL on the same crossfade.",
            "color colour interpolation srgb oklab lch hsl space mixing gradient crossfade",
            "ColorSpaces.razor"),
        new("Motion Path", "motion-path", Advanced,
            "Moving an element along an SVG path with offsetPath and offsetDistance, and bending a straight move into an arc.",
            "motion path offsetpath offsetdistance svg curve arc trajectory follow",
            "MotionPath.razor"),
        new("CSS Props", "css-props", Advanced,
            "Animating properties with no dedicated argument: arbitrary CSS through Css, and CSS custom properties through CssVars.",
            "css custom properties variables cssvars arbitrary property backdrop filter escape hatch",
            "CssProps.razor"),
        new("Accessibility", "accessibility", Guides,
            "prefers-reduced-motion policies, what reduced motion keeps animating, global speed control, and the live engine inspector.",
            "accessibility a11y reduced motion prefers-reduced-motion diagnostics inspector speed reducemotion policy",
            "Accessibility.razor"),
        new("MCP Server", "mcp-server", Guides,
            "The demo's Model Context Protocol server: the tools, prompts and resources that let an AI agent write correct Bmotion code.",
            "mcp model context protocol ai agent llm tools prompts resources claude cursor copilot server integration",
            "McpServerPage.razor"),
    ];

    /// <summary>
    /// The pages a reader navigates between: everything in <see cref="All"/> except the landing
    /// page, which is not a document and is reached from the header instead.
    /// </summary>
    public static readonly NavItem[] Docs = [.. All.Where(item => item.Href.Length > 0)];

    /// <summary>
    /// <see cref="Docs"/> in groups, in reading order. Both the order of the groups and the order
    /// within them come from the order of <see cref="All"/>, so re-filing a page is a one-line
    /// edit above and nothing here has to be kept in step with it.
    /// </summary>
    public static readonly (string Category, string Icon, NavItem[] Items)[] Groups =
    [
        .. Docs.GroupBy(item => item.Category)
               .Select(group => (group.Key, CategoryIcon(group.Key), group.ToArray()))
    ];

    /// <summary>The entry for a route, or null when the route is not a demo page.</summary>
    public static NavItem? ByHref(string href)
        => Array.Find(All, item => string.Equals(item.Href, href, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The pages either side of <paramref name="href"/> in reading order, for the pager at the
    /// foot of every page. Both are null for a route that is not a demo page - the error and
    /// not-found pages, which are not part of the sequence and have nowhere to go next.
    /// </summary>
    public static (NavItem? Previous, NavItem? Next) Neighbours(string href)
    {
        var index = Array.FindIndex(Docs, item => string.Equals(item.Href, href, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return (null, null);

        return (index > 0 ? Docs[index - 1] : null,
                index < Docs.Length - 1 ? Docs[index + 1] : null);
    }

    /// <summary>
    /// Whether this page answers to <paramref name="term"/>. Keywords are matched as well as the
    /// title and the category: a reader searching for "hover" or "stagger" is naming the feature,
    /// not the page it happens to live on, and every entry carries the terms it covers.
    /// </summary>
    public bool Matches(string term)
        => Title.Contains(term, StringComparison.OrdinalIgnoreCase)
        || Category.Contains(term, StringComparison.OrdinalIgnoreCase)
        || Keywords.Contains(term, StringComparison.OrdinalIgnoreCase)
        || Description.Contains(term, StringComparison.OrdinalIgnoreCase);

    // The one place a group is tied to a picture. Kept as a switch rather than as a field on the
    // record so the entries above stay a list of prose, and so two pages in the same group cannot
    // be given two different icons.
    private static string CategoryIcon(string category) => category switch
    {
        GettingStarted => Icons.Play,
        Motion => Icons.Pulse,
        Interaction => Icons.Pointer,
        Orchestration => Icons.Layers,
        LayoutAndScroll => Icons.Layout,
        Advanced => Icons.Controls,
        Guides => Icons.Book,
        _ => Icons.Book,
    };
}
