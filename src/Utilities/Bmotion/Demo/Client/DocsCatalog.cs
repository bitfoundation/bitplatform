using Bit.Bmotion.Demo.Client.Pages;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Demo.Client;

/// <summary>
/// One documentation page: <paramref name="Url"/> is its address, <paramref name="Keywords"/> feeds
/// the header's search palette, and <paramref name="PageType"/> is the component the address renders -
/// which is what the "Edit this page on GitHub" link under every page is built from.
/// </summary>
public record DocsPageInfo(string Url, string Title, string Description, string Keywords, Type PageType);

public record DocsSection(string Title, DocsPageInfo[] Pages);

/// <summary>
/// A link in the header. <paramref name="MatchesDocs"/> marks the entry that stands for the docs as a
/// whole: it is the current section on every page no other entry claims.
/// </summary>
public record SiteNavItem(string Href, string Label, bool MatchesDocs = false);

/// <summary>
/// What the shared site chrome in Layout/ - the header, the docs shell, the palette and the footer -
/// says about this particular demo. Those components are the same file in every Utilities demo; this
/// class and <see cref="DocsCatalog"/> are what each demo fills in for itself.
/// </summary>
public static class SiteInfo
{
    /// <summary>The library's name, as the header's wordmark and the breadcrumb's root show it.</summary>
    public const string Name = "Bmotion";

    public const string PackageId = "Bit.Bmotion";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/Bmotion";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "Bmotion is a Blazor-native animation library inspired by Framer Motion: springs, gestures, variants, " +
        "drag, layout and scroll animations, with all of the maths in C# and no JavaScript wiring of your own. " +
        "Every moving thing on this site is Bmotion animating itself.";

    public static readonly string[] Badges = ["MIT", "net8.0", "net9.0", "net10.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["spring", "stagger", "drag", "exit", "scroll"];

    /// <summary>
    /// Whether Ctrl+K / Cmd+K opens the site's search palette. "/" always does. A demo whose own
    /// library answers to Ctrl+K turns this off so the two do not fight over the key.
    /// </summary>
    public static readonly bool PaletteCtrlK = true;

    public static readonly SiteNavItem[] Primary =
    [
        new("/", "Home"),
        new("/getting-started", "Docs", MatchesDocs: true),
        new("/mcp-server", "MCP server"),
    ];

    public const string RepositoryUrl = "https://github.com/bitfoundation/bitplatform";

    public static string ProjectUrl => $"{RepositoryUrl}/tree/develop/{ProjectPath}";

    public static string NuGetUrl => $"https://www.nuget.org/packages/{PackageId}";

    /// <summary>
    /// The page's source file on GitHub. Razor gives a component the namespace of its folder, so the
    /// path below Demo/Client is the type's namespace past this assembly's root, one folder a segment.
    /// </summary>
    public static string SourceUrlOf(Type page)
    {
        var root = typeof(SiteInfo).Namespace!;
        var relative = page.FullName!.StartsWith(root + ".", StringComparison.Ordinal)
            ? page.FullName[(root.Length + 1)..]
            : page.Name;

        return $"{RepositoryUrl}/blob/develop/{ProjectPath}/Demo/Client/{relative.Replace('.', '/')}.razor";
    }
}

/// <summary>
/// The single source of truth for the site's information architecture. The docs nav panel, the
/// header's search palette, the previous/next pager, the breadcrumb and the footer's link columns are
/// all driven from here, so adding a page means adding exactly one entry. The demo's MCP server reads
/// the same list (Server/Services/BmotionSourceCatalog.cs and BmotionSearchIndex.cs), so the
/// description and keywords an agent is handed for a demo page are the words the site shows.
/// </summary>
public static class DocsCatalog
{
    public static readonly DocsSection[] Sections =
    [
        new("Getting started",
        [
            new("/getting-started", "Getting started",
                "Install the package, register the services, link the prerender stylesheet and animate your first element.",
                "install setup nuget package AddBitBmotionServices quick start program.cs imports stylesheet prerender render mode",
                typeof(GettingStarted)),
            new("/basic", "Basics",
                "Initial and Animate on a single element, the shape of every Bmotion animation, and what runs on mount.",
                "initial animate mount enter first render bmotion component transition basics hello world",
                typeof(BasicAnimations)),
        ]),
        new("Motion",
        [
            new("/springs", "Springs",
                "Physics springs: stiffness and damping, the intuitive bounce and duration form, overshoot on mount, and repeating springs.",
                "spring stiffness damping mass bounce duration overshoot physics velocity repeat mirror",
                typeof(Springs)),
            new("/easing", "Easing",
                "Every BmEase preset side by side, custom cubic beziers, stepped easing, and per-segment easing across keyframes.",
                "ease easing bezier cubic steps linear in out inout back elastic bounce circ expo quad quart quint sine",
                typeof(Easing)),
            new("/keyframes", "Keyframes",
                "Multi-step animations from arrays of values, with Times for uneven spacing and Bm.Current as the wildcard first frame.",
                "keyframes array multi step times wildcard current sequence repeat mirror loop",
                typeof(Keyframes)),
        ]),
        new("Interaction",
        [
            new("/gestures", "Gestures",
                "WhileHover, WhileTap, WhileFocus and WhileInView - animation states the element enters while something is true.",
                "hover tap press focus inview viewport gesture whilehover whiletap whilefocus whileinview intersection",
                typeof(Gestures)),
            new("/drag", "Drag",
                "Pointer dragging with axis locks, constraints, elastic edges, momentum on release and drag-driven values.",
                "drag pointer pan constraints elastic momentum inertia snap axis lock draggable",
                typeof(DragPage)),
            new("/reorder", "Reorder",
                "BmotionReorderGroup: a drag-to-reorder list where the other items animate out of the way as one is moved.",
                "reorder sortable list drag drop rearrange items order group",
                typeof(ReorderPage)),
        ]),
        new("Orchestration",
        [
            new("/variants", "Variants",
                "Named animation states shared down a subtree, with orchestration: staggered children, before/after children, and propagation.",
                "variants named states orchestration stagger staggerchildren delaychildren when beforechildren afterchildren propagate parent child",
                typeof(Variants)),
            new("/presets", "Presets",
                "BmPresets: packaged enter/exit, overlay, gesture, attention, scroll and loading animations, and composing them with +.",
                "presets preset fade slide scale pop blur flip modal drawer popover tooltip toast hoverlift press pulse shake wiggle jump heartbeat reveal spin shimmer compose reusable",
                typeof(Presets)),
            new("/presence", "AnimatePresence",
                "Exit animations: how an element animates out before Blazor removes it, plus presence modes and switching between views.",
                "exit animate presence unmount remove leave mode wait popLayout sync switch group conditional rendering",
                typeof(AnimatePresencePage)),
            new("/split-text", "Split Text",
                "BmotionSplitText breaking a headline into characters, words or lines so each piece can animate on its own stagger.",
                "split text characters words lines stagger headline typography reveal per letter",
                typeof(SplitText)),
        ]),
        new("Layout and scroll",
        [
            new("/layout", "Layout",
                "FLIP layout animations: an element animating between two positions or sizes it was merely re-rendered into, and shared-element transitions.",
                "layout flip shared element magic move layoutid position size reposition transition morph",
                typeof(LayoutPage)),
            new("/scroll", "Scroll",
                "Scroll-driven animation: progress-linked timelines that the browser scrubs, and enter-on-scroll reveals.",
                "scroll parallax progress timeline scrolltimeline viewtimeline reveal sticky offset viewport",
                typeof(ScrollAnimations)),
            new("/view-transitions", "View Transitions",
                "The browser View Transitions API driven from C#: cross-fading a whole DOM update rather than one element.",
                "view transition startviewtransition cross fade page navigation document update",
                typeof(ViewTransitions)),
        ]),
        new("Advanced",
        [
            new("/programmatic", "Programmatic",
                "The imperative API: animating by selector or ElementReference, motion values, sequences and playback controls.",
                "programmatic imperative animate service selector elementreference motion value bmvalue sequence timeline controls pause play stop speed",
                typeof(Programmatic)),
            new("/color", "Color",
                "Colour interpolation and why the space matters: sRGB against OKLab on the same crossfade, per transition or for a subtree.",
                "color colour interpolation srgb oklab colorspace BmColorSpace perceptual space mixing gradient crossfade",
                typeof(ColorSpaces)),
            new("/motion-path", "Motion Path",
                "Moving an element along an SVG path with offsetPath and offsetDistance, and bending a straight move into an arc.",
                "motion path offsetpath offsetdistance svg curve arc trajectory follow",
                typeof(MotionPath)),
            new("/css-props", "CSS Props",
                "The style surface beyond transforms - typography, spacing, positioning, clipping and backgrounds - plus any other property through css and custom properties through cssVars.",
                "css custom properties variables cssvars arbitrary property letterspacing fontsize lineheight padding margin gap top left clippath backgroundposition backgroundsize escape hatch",
                typeof(CssProps)),
        ]),
        new("Guides",
        [
            new("/accessibility", "Accessibility",
                "prefers-reduced-motion policies, what reduced motion keeps animating, global speed control, and the live engine inspector.",
                "accessibility a11y reduced motion prefers-reduced-motion diagnostics inspector speed reducemotion policy",
                typeof(Accessibility)),
            new("/mcp-server", "MCP Server",
                "The demo's Model Context Protocol server: the tools, prompts and resources that let an AI agent write correct Bmotion code.",
                "mcp model context protocol ai agent llm tools prompts resources claude cursor copilot server integration",
                typeof(McpServerPage)),
        ]),
    ];

    public static readonly DocsPageInfo[] AllPages = [.. Sections.SelectMany(s => s.Pages)];

    /// <summary>The current address relative to the base, without its query string or fragment.</summary>
    public static string PathOf(NavigationManager nav)
    {
        var path = nav.ToBaseRelativePath(nav.Uri);
        var cut = path.IndexOfAny(['?', '#']);
        return (cut >= 0 ? path[..cut] : path).Trim('/');
    }

    /// <summary>Whether a link's address and a base-relative path name the same page, ignoring case as the router does.</summary>
    public static bool IsSamePath(string href, string path)
        => string.Equals(href.Trim('/'), path.Trim('/'), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Finds the catalog entry for a base-relative path, or null when the page is not in the catalog. A
    /// page whose route takes a parameter is listed by its bare address, so a deeper path falls back to
    /// the entry it starts with.
    /// </summary>
    public static DocsPageInfo? FindByPath(string path)
        => AllPages.FirstOrDefault(p => IsSamePath(p.Url, path))
           ?? AllPages.FirstOrDefault(p => path.Trim('/').StartsWith(p.Url.Trim('/') + "/", StringComparison.OrdinalIgnoreCase));

    /// <summary>Previous/next pages in reading order for the bottom-of-page pager.</summary>
    public static (DocsPageInfo? Previous, DocsPageInfo? Next) NeighborsOf(DocsPageInfo page)
    {
        var index = Array.IndexOf(AllPages, page);
        if (index < 0) return (null, null);
        return (index > 0 ? AllPages[index - 1] : null,
                index < AllPages.Length - 1 ? AllPages[index + 1] : null);
    }

    /// <summary>Section a page belongs to (the breadcrumb's middle step and the palette's group label).</summary>
    public static string SectionOf(DocsPageInfo page)
        => Sections.First(s => s.Pages.Contains(page)).Title;
}
