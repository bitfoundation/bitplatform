using Bit.Blow.Demo.Client.Examples;
using Bit.Blow.Demo.Client.Pages;
using Microsoft.AspNetCore.Components;

namespace Bit.Blow.Demo.Client;

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
    public const string Name = "Blow";

    public const string PackageId = "Bit.Blow";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/Blow";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "Blow is a Blazor-native library for node-based UIs - editors, flow charts, pipelines and diagrams - and " +
        "the Blazor counterpart to React Flow, with interaction, layout and edge geometry written in C#. This " +
        "site is itself a Blazor app built on it.";

    public static readonly string[] Badges = ["MIT", "net10.0", "net11.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["custom node", "minimap", "layout", "undo", "subflow"];

    /// <summary>
    /// Whether Ctrl+K / Cmd+K opens the site's search palette. "/" always does. A demo whose own
    /// library answers to Ctrl+K turns this off so the two do not fight over the key.
    /// </summary>
    public static readonly bool PaletteCtrlK = true;

    public static readonly SiteNavItem[] Primary =
    [
        new("/", "Home"),
        new("/getting-started", "Docs", MatchesDocs: true),
        new("/examples", "Examples"),
        new("/mcp-server", "MCP server"),
    ];

    public const string RepositoryUrl = "https://github.com/bitfoundation/bitplatform";

    public static string ProjectUrl => $"{RepositoryUrl}/tree/develop/{ProjectPath}";

    public static string NuGetUrl => $"https://www.nuget.org/packages/{PackageId}";

    /// <summary>
    /// The page's source file on GitHub. Razor gives a component the namespace of its folder, so the
    /// path below Demo/Client is the type's namespace past this assembly's root, one folder a segment -
    /// Pages.Examples.BasicExample is Pages/Examples/BasicExample.razor.
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
/// all driven from here, so adding a page means adding exactly one entry.
/// </summary>
public static class DocsCatalog
{
    // The hand-written tours come first, in the order the old navigation listed them. The examples
    // gallery follows as one section per group of ExampleCatalog - its own source of truth, which the
    // gallery's index page and the MCP server's search read too - so an example added there shows up
    // here, in the palette and in the pager without a second entry.
    public static readonly DocsSection[] Sections =
    [
        new("Introduction",
        [
            new("/getting-started", "Getting Started",
                "Install the package, link the stylesheet, give the canvas a height and put a first flow on a page.",
                "install setup nuget package quick start stylesheet bit-blow.css _Imports render mode first flow container height lazy loading lazyassemblyloader blazorwebassemblylazyload aot",
                typeof(GettingStarted)),
            new("/basic-flow", "Basic Flow",
                "Nodes, edges and the default gestures: drag, connect, select, box-select, delete, pan and zoom.",
                "basic flow first BlowCanvas nodes edges drag connect select delete pan zoom minimap controls add node BlowPanel OnConnect keyboard",
                typeof(BasicFlow)),
            new("/examples", "All Examples",
                "The gallery of React Flow examples ported to Blow, grouped by topic, each one a working page.",
                "examples gallery react flow ports index catalog all",
                typeof(Pages.Examples.ExamplesIndex)),
        ]),
        new("Feature Tours",
        [
            new("/custom-nodes", "Custom Nodes",
                "A NodeTemplate with a working colour picker and two labelled source handles, handing every other node back to BlowDefaultNode.",
                "custom node NodeTemplate template BlowHandle multiple handles SourceHandle Ports BlowPort Label BlowDefaultNode nodrag color picker swatch",
                typeof(CustomNodes)),
            new("/edge-types", "Edge Types",
                "All six path types - bezier, simple bezier, straight, step, smooth step and routed - each with its own arrowhead, and the type of new edges.",
                "edge types bezier simplebezier straight step smoothstep routed markers arrow MarkerType MarkerStart MarkerEnd MarkerColor MarkerWidth DefaultEdgeType ConnectionLineType",
                typeof(EdgeTypes)),
            new("/custom-edges", "Custom Edges",
                "An EdgeTemplate on BlowBaseEdge and BlowEdgeText, coloured from edge data and passing on its arrowheads, with a BlowEdgeToolbar on the selected edge.",
                "custom edge EdgeTemplate BlowBaseEdge BlowEdgeText BlowEdgeToolbar MarkerStartUrl MarkerEndUrl InteractionWidth arrowheads label plate recolor delete",
                typeof(CustomEdges)),
            new("/interactions", "Interactions",
                "A playground of interaction options - snap to grid, connect on click, selection on drag - with an event log.",
                "interactions playground SnapToGrid ConnectOnClick SelectionOnDrag PanOnScroll ZoomOnDoubleClick OnlyRenderVisibleElements reconnect OnDelete event log imperative api",
                typeof(Interactions)),
            new("/labels-handles", "Labels & Handles",
                "HTML edge labels with buttons in them, and nodes that only drag by their grip.",
                "html edge labels EdgeLabelTemplate drag handle BlowDragHandle UseCustomDragHandle",
                typeof(LabelsAndHandles)),
            new("/extras", "Theming & Extras",
                "Colour modes, loose connections, canvas and per-node origin, the attribution badge, a viewport portal, a single resize control with size limits, and BlowGraphUtils helpers.",
                "theming color mode dark loose connections ConnectionMode NodeOrigin Origin BlowViewportPortal BlowNodeResizeControl KeepAspectRatio MaxWidth MaxHeight BlowGraphUtils GetIncomers GetOutgoers GetConnectedEdges GetNodesBounds GetViewportForBounds AddEdge ReconnectEdge BuildEdgeId neighbors attribution HideAttribution AttributionPosition",
                typeof(Extras)),
            new("/advanced", "Groups & Resizing",
                "Group nodes that carry their children - free, pinned inside or growing the group - a resizable node with a toolbar, and connection validation.",
                "groups group parent ParentId ExtentParent ExpandParent subflow resize BlowNodeResizer BlowNodeToolbar DeleteElementsAsync validation IsValidConnection",
                typeof(Advanced)),
            new("/connection-line", "Connection Lines",
                "The connection line built in, restyled or drawn by a ConnectionLineTemplate, the live state and refusal reason from OnConnectionChange, the drag threshold, and Escape to cancel.",
                "connection line ConnectionLineTemplate ConnectionLineStyle ConnectionLineClass ConnectionLineType ConnectionDragThreshold OnConnectionChange BlowConnectionLineContext Rejection BlowConnectionRejection cancel escape valid target",
                typeof(ConnectionLineDemo)),
            new("/add-node-on-drop", "Add Node on Drop",
                "OnConnectEnd's BlowConnectEndInfo turns a connection dropped on empty space into a new, connected node.",
                "add node drop OnConnectEnd BlowConnectEndInfo IsDropped IsReconnect Rejection FlowPosition BlowConnectionStartInfo spawn upstream downstream",
                typeof(AddNodeOnDropDemo)),
            new("/floating-edges", "Floating Edges",
                "Edges that meet each node's border where it faces the other node, from BlowGraphUtils.GetFloatingEdgeParams, with its anchor points and sides shown.",
                "floating edges GetFloatingEdgeParams GetNodeIntersection GetEdgePosition loose connection EdgeTemplate BlowBaseEdge border anchor arrowhead",
                typeof(FloatingEdgesDemo)),
            new("/minimap", "MiniMap",
                "Minimap nodes by colour, outline, class or SVG template, a tinted mask and background, and a minimap you pan and zoom.",
                "minimap BlowMiniMap NodeColorFunc NodeStrokeColorFunc NodeClassFunc NodeColor NodeStrokeColor NodeStrokeWidth BorderRadius NodeTemplate MaskColor MaskStrokeColor MaskStrokeWidth BgColor Pannable Zoomable",
                typeof(MiniMapDemo)),
            new("/drag-threshold", "Drag Threshold",
                "NodeDragThreshold: how far the pointer must travel before a press on a node becomes a drag, and how it plays with NodeClickDistance.",
                "drag threshold NodeDragThreshold NodeClickDistance OnNodeClick OnNodeDragStart OnNodeDragStop click drag accidental nudge",
                typeof(DragThresholdDemo)),
        ]),
        .. ExampleCatalog.Grouped().Select(group => new DocsSection(group.Key,
        [
            .. group.Select(example => new DocsPageInfo($"/examples/{example.Route}", example.Title,
                example.Summary, example.Keywords, example.Page)),
        ])),
        new("Tooling",
        [
            new("/mcp-server", "MCP server",
                "The Model Context Protocol server this site hosts, so an AI agent reads the library instead of guessing.",
                "mcp model context protocol ai agent tools resources prompts claude copilot server",
                typeof(McpServer)),
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
