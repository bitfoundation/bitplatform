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

    public static readonly string[] Badges = ["MIT", "net10.0", "net11.0"];

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
                "Install the package, register the services and pick the global options, link the prerender stylesheet and animate your first element.",
                "install setup nuget package AddBitBmotionServices quick start program.cs imports stylesheet prerender render mode options cssafemode inertwhileexiting lazy load lazyassemblyloader blazorwebassemblylazyload GetBmotionService ConfigureBmotionServices aot",
                typeof(GettingStarted)),
            new("/basic", "Basics",
                "Initial and Animate on a single element: every transform, colours, shadows and 3-D, skipping the entrance, timing per property, composing targets and the start/complete events.",
                "initial animate mount enter first render bmotion component transition basics hello world transforms skew scalex scaley rotatez initial false properties per-property delay withdelay compose with onanimationstart onanimationcomplete",
                typeof(BasicAnimations)),
        ]),
        new("Motion",
        [
            new("/springs", "Springs",
                "Physics springs: stiffness, damping and mass, the bounce and duration form, a starting velocity, interrupting mid-flight, overshoot on mount, and repeating springs.",
                "spring stiffness damping mass bounce duration overshoot physics velocity interrupt retarget restspeed restdelta repeat mirror loop",
                typeof(Springs)),
            new("/easing", "Easing",
                "All 32 BmEase presets side by side, in each direction, the default curve, custom cubic-béziers and stepped easing.",
                "ease easing bezier cubic steps stepjump linear in out inout back anticipate elastic bounce circ expo quad quart quint sine default",
                typeof(Easing)),
            new("/keyframes", "Keyframes",
                "Multi-step animations from arrays of values, with Times for uneven spacing, an ease per segment, the three repeat types, and Bm.Current as the wildcard first frame.",
                "keyframes array multi step times eases per-segment wildcard current sequence repeat mirror loop reverse repeat delay",
                typeof(Keyframes)),
        ]),
        new("Interaction",
        [
            new("/gestures", "Gestures",
                "WhileHover, WhileTap, WhileFocus and WhileInView - animation states the element enters while something is true - with their events, pan, nesting and TransformTemplate.",
                "hover tap press focus inview viewport root amount margin gesture whilehover whiletap whilefocus whileinview intersection onfocusstart onviewportenter onviewportleave pan propagation transformtemplate",
                typeof(Gestures)),
            new("/drag", "Drag",
                "Pointer dragging with axis locks, pixel and element constraints, elastic edges, momentum and snapping on release, handles, auto-scroll and drag events.",
                "drag pointer pan constraints within parent box vertical horizontal elastic momentum inertia snap snapto axis lock draggable handle controls autoscroll dragautoscroll ondrag",
                typeof(DragPage)),
            new("/reorder", "Reorder",
                "BmotionReorderGroup: drag-to-reorder lists, rows and grids, with auto-scroll, a custom lift and settle, and keyboard reordering with screen-reader announcements.",
                "reorder sortable list drag drop rearrange items order group grid horizontal autoscroll whiledrag keyboard announce screen reader",
                typeof(ReorderPage)),
        ]),
        new("Orchestration",
        [
            new("/variants", "Variants",
                "Named animation states shared down a subtree, with orchestration, opting out, dynamic variants, variant names on gesture targets and grid stagger.",
                "variants named states orchestration stagger staggerchildren delaychildren childstagger when beforechildren afterchildren propagate parent child inherit custom dynamic gesture create grid",
                typeof(Variants)),
            new("/presets", "Presets",
                "BmPresets: packaged enter/exit, overlay, gesture, attention, scroll and loading animations, composing them with +, and writing your own.",
                "presets preset fade slide scale pop blur flip modal drawer popover tooltip toast hoverlift press pulse shake wiggle jump heartbeat reveal spin shimmer compose reusable custom",
                typeof(Presets)),
            new("/presence", "AnimatePresence",
                "Exit animations: how an element animates out before Blazor removes it, with OnExitComplete, presence modes, keyed lists, pop layout and switching between views.",
                "exit animate presence unmount remove leave mode wait popLayout sync switch group conditional rendering onexitcomplete inert inertwhileexiting",
                typeof(AnimatePresencePage)),
            new("/split-text", "Split Text",
                "BmotionSplitText breaking a headline into characters, words or lines so each piece can animate on its own stagger - with gestures, exits, per-unit styling and a completion callback.",
                "split text characters words lines stagger headline typography reveal per letter unitclass oncomplete exit accessible",
                typeof(SplitText)),
        ]),
        new("Layout and scroll",
        [
            new("/layout", "Layout",
                "FLIP layout animations: an element animating between positions or sizes it was merely re-rendered into, the three layout modes, shared elements with groups and crossfade, and anchoring.",
                "layout flip shared element magic move layoutid position size reposition transition morph layoutgroup crossfade layoutanchor layoutdependency layoutscroll layoutroot",
                typeof(LayoutPage)),
            new("/scroll", "Scroll",
                "Scroll-driven animation: browser-scrubbed timelines for the page, a container or an element's journey, motion values and velocity from the scroll position, and enter-on-scroll reveals.",
                "scroll parallax progress timeline scrolltimeline viewtimeline container viewof range horizontal reveal sticky offset viewport velocity tracker",
                typeof(ScrollAnimations)),
            new("/view-transitions", "View Transitions",
                "The browser View Transitions API driven from C#: cross-fading a whole DOM update rather than one element, with typed transitions and a reduced-motion override.",
                "view transition startviewtransition cross fade page navigation document update types forward backward reducemotion",
                typeof(ViewTransitions)),
        ]),
        new("Advanced",
        [
            new("/programmatic", "Programmatic",
                "The imperative API: animating by selector, id or ElementReference, numbers, sequences, motion values and their helpers, element refs, and BmotionConfig defaults.",
                "programmatic imperative animate service selector id elementreference motion value bmvalue subscribe jump wrap clamp mix maprange sequence timeline controls pause play stop speed set config",
                typeof(Programmatic)),
            new("/color", "Color",
                "Colour interpolation and why the space matters: sRGB against OKLab on the same crossfade, per transition or for a subtree.",
                "color colour interpolation srgb oklab colorspace BmColorSpace perceptual space mixing gradient crossfade",
                typeof(ColorSpaces)),
            new("/motion-path", "Motion Path",
                "Moving an element along an SVG path with offsetPath and offsetDistance, bending a straight move into an arc, drawing and dashing strokes, and morphing shapes.",
                "motion path offsetpath offsetdistance svg curve arc peak direction trajectory follow pathlength pathoffset pathspacing draw morph d",
                typeof(MotionPath)),
            new("/css-props", "CSS Props",
                "The style surface beyond transforms - typography, spacing, sizing, positioning, colours, clipping and backgrounds - plus any other property through css and custom properties through cssVars.",
                "css custom properties variables cssvars arbitrary property letterspacing fontsize lineheight padding margin gap width height top left right bottom color bordercolor outlinecolor fill stroke clippath backgroundposition backgroundsize escape hatch",
                typeof(CssProps)),
        ]),
        new("Guides",
        [
            new("/accessibility", "Accessibility",
                "prefers-reduced-motion policies, what reduced motion keeps animating, global speed control, and the live engine inspector.",
                "accessibility a11y reduced motion prefers-reduced-motion diagnostics inspector speed reducemotion policy capabilities isprerendering supportsframeloop onupdate",
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
