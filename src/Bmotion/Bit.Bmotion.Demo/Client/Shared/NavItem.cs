namespace Bit.Bmotion.Demo.Client.Shared;

/// <summary>
/// One entry of the docs navigation. The list itself is <see cref="All"/>: the nav panel renders
/// it and the search box filters it, so both the drawer and the sticky rail always show the same
/// set of pages in the same order.
/// <para>
/// It is also what the demo's MCP server answers <c>GetBmotionDemoPages</c> with, which is why
/// each entry carries a description, search keywords and the file it is implemented in. A page
/// therefore describes itself once, and the site's search box, the nav panel and an AI agent
/// looking for a worked example all read the same words.
/// </para>
/// </summary>
/// <param name="Title">The label shown in the nav panel.</param>
/// <param name="Href">The route, relative to the app base.</param>
/// <param name="Description">What the page demonstrates, in one sentence.</param>
/// <param name="Keywords">Space-separated terms the page covers, for the search box and the MCP index.</param>
/// <param name="Source">The page's file name under Client/Pages, for GetBmotionSourceFile.</param>
public sealed record NavItem(string Title, string Href, string Description, string Keywords, string Source)
{
    /// <summary>The path the MCP server hands this page's source out under.</summary>
    public string SourcePath => $"Demo/Client/Pages/{Source}";

    /// <summary>Every demo page, in the order they are meant to be read.</summary>
    public static readonly NavItem[] All =
    [
        new("Home", "",
            "The landing page: what Bmotion is, and the feature tiles that link into the rest of the demos.",
            "home landing overview introduction start",
            "Home.razor"),
        new("Basics", "basic",
            "Initial and Animate on a single element, the shape of every Bmotion animation, and what runs on mount.",
            "initial animate mount enter first render bmotion component transition basics hello world",
            "BasicAnimations.razor"),
        new("Springs", "springs",
            "Physics springs: stiffness and damping, the intuitive bounce and duration form, overshoot on mount, and repeating springs.",
            "spring stiffness damping mass bounce duration overshoot physics velocity repeat mirror",
            "Springs.razor"),
        new("Easing", "easing",
            "Every BmEase preset side by side, custom cubic beziers, stepped easing, and per-segment easing across keyframes.",
            "ease easing bezier cubic steps linear in out inout back elastic bounce circ expo quad quart quint sine",
            "Easing.razor"),
        new("Gestures", "gestures",
            "WhileHover, WhileTap, WhileFocus and WhileInView - animation states the element enters while something is true.",
            "hover tap press focus inview viewport gesture whilehover whiletap whilefocus whileinview intersection",
            "Gestures.razor"),
        new("Variants", "variants",
            "Named animation states shared down a subtree, with orchestration: staggered children, before/after children, and propagation.",
            "variants named states orchestration stagger staggerchildren delaychildren when beforechildren afterchildren propagate parent child",
            "Variants.razor"),
        new("Keyframes", "keyframes",
            "Multi-step animations from arrays of values, with Times for uneven spacing and Bm.Current as the wildcard first frame.",
            "keyframes array multi step times wildcard current sequence repeat mirror loop",
            "Keyframes.razor"),
        new("Split Text", "split-text",
            "BmotionSplitText breaking a headline into characters, words or lines so each piece can animate on its own stagger.",
            "split text characters words lines stagger headline typography reveal per letter",
            "SplitText.razor"),
        new("AnimatePresence", "presence",
            "Exit animations: how an element animates out before Blazor removes it, plus presence modes and switching between views.",
            "exit animate presence unmount remove leave mode wait popLayout sync switch group conditional rendering",
            "AnimatePresencePage.razor"),
        new("Drag", "drag",
            "Pointer dragging with axis locks, constraints, elastic edges, momentum on release and drag-driven values.",
            "drag pointer pan constraints elastic momentum inertia snap axis lock draggable",
            "DragPage.razor"),
        new("Reorder", "reorder",
            "BmotionReorderGroup: a drag-to-reorder list where the other items animate out of the way as one is moved.",
            "reorder sortable list drag drop rearrange items order group",
            "ReorderPage.razor"),
        new("Scroll", "scroll",
            "Scroll-driven animation: progress-linked timelines that the browser scrubs, and enter-on-scroll reveals.",
            "scroll parallax progress timeline scrolltimeline viewtimeline reveal sticky offset viewport",
            "ScrollAnimations.razor"),
        new("Layout", "layout",
            "FLIP layout animations: an element animating between two positions or sizes it was merely re-rendered into, and shared-element transitions.",
            "layout flip shared element magic move layoutid position size reposition transition morph",
            "LayoutPage.razor"),
        new("Programmatic", "programmatic",
            "The imperative API: animating by selector or ElementReference, motion values, sequences and playback controls.",
            "programmatic imperative animate service selector elementreference motion value bmvalue sequence timeline controls pause play stop speed",
            "Programmatic.razor"),
        new("Color", "color",
            "Colour interpolation and why the space matters: sRGB against Oklab, LCH and HSL on the same crossfade.",
            "color colour interpolation srgb oklab lch hsl space mixing gradient crossfade",
            "ColorSpaces.razor"),
        new("Motion Path", "motion-path",
            "Moving an element along an SVG path with offsetPath and offsetDistance, and bending a straight move into an arc.",
            "motion path offsetpath offsetdistance svg curve arc trajectory follow",
            "MotionPath.razor"),
        new("View Transitions", "view-transitions",
            "The browser View Transitions API driven from C#: cross-fading a whole DOM update rather than one element.",
            "view transition startviewtransition cross fade page navigation document update",
            "ViewTransitions.razor"),
        new("CSS Props", "css-props",
            "Animating properties with no dedicated argument: arbitrary CSS through Css, and CSS custom properties through CssVars.",
            "css custom properties variables cssvars arbitrary property backdrop filter escape hatch",
            "CssProps.razor"),
        new("Accessibility", "accessibility",
            "prefers-reduced-motion policies, what reduced motion keeps animating, global speed control, and the live engine inspector.",
            "accessibility a11y reduced motion prefers-reduced-motion diagnostics inspector speed reducemotion policy",
            "Accessibility.razor"),
        new("MCP Server", "mcp",
            "The demo's Model Context Protocol server: the tools, prompts and resources that let an AI agent write correct Bmotion code.",
            "mcp model context protocol ai agent llm tools prompts resources claude cursor copilot server integration",
            "McpServerPage.razor"),
    ];
}
