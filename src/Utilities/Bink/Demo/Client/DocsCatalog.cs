using Bit.Bink.Demo.Client.Pages;
using Microsoft.AspNetCore.Components;

namespace Bit.Bink.Demo.Client;

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
    public const string Name = "Bink";

    public const string PackageId = "Bit.Bink";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/Bink";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "Bink is a Blazor-native drawing and ink engine: pressure-aware strokes, brushes, erasers, selection, " +
        "layers, annotations, undo, collaboration and SVG/PNG export, with the engine written in C#. Every page " +
        "of this site is a working canvas.";

    public static readonly string[] Badges = ["MIT", "net8.0", "net9.0", "net10.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["signature", "pressure", "layers", "export", "collaboration"];

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
/// all driven from here, so adding a page means adding exactly one entry.
/// </summary>
public static class DocsCatalog
{
    public static readonly DocsSection[] Sections =
    [
        new("Introduction",
        [
            new("/getting-started", "Getting started",
                "Install the package and put a canvas on a page - no services to register and no stylesheet to link.",
                "install setup nuget package quick start BinkCanvas _Imports render mode InteractiveWebAssembly stylesheet initializer brush tool undo keyboard save export headless Unavailable AttachFailed CSP lazy loading eyedropper",
                typeof(GettingStarted)),
        ]),
        new("Use cases",
        [
            new("/signature", "Signature",
                "A signature field with validation, a read-only accepted state, and SVG and PNG export cropped to the ink.",
                "signature pad sign field validate accept read only ReadOnly StrokeCompleted MeasureInk metadata export svg png crop BinkExportRegion form touch scroll",
                typeof(Signature)),
            new("/handwriting", "Handwriting",
                "A note-taking surface tuned for writing: smoothing, pressure curves, prediction, palm rejection, tilt and ruled paper.",
                "handwriting notes pen stylus pressure curve PressureCurve SimulatePressure smoothing spacing PredictInk prediction tilt palm rejection RejectTouchWhilePen pencil calligraphy NibAngle marker highlighter eraser end",
                typeof(Handwriting)),
            new("/whiteboard", "Whiteboard",
                "The full editor: every tool and shape, pictures and SVG, grouping, alignment, layers, the clipboard, autosave and export.",
                "whiteboard editor tools layers opacity rename reorder blend selection transform move resize rotate text shapes polygon star triangle line arrow polyline curve dashed eraser zoom pan save open export duplicate group lock align distribute flip clipboard cut paste drop insert image import svg eyedropper autosave IndexedDB unsaved IsDirty",
                typeof(Whiteboard)),
            new("/diagram", "Diagram surface",
                "Nodes and connectors on the document model: move a node and its connectors follow, in the same undo step.",
                "diagram nodes connectors graph flowchart custom tool metadata Changed listener batch undo labels snapping grid BinkSnapper graph paper",
                typeof(Diagram)),
            new("/image-annotation", "Image annotation",
                "Open a screenshot or photo, mark it up on a layer above it, crop it, and export PNG, JPEG, WebP or SVG.",
                "image annotation screenshot photo markup arrow box highlight pixel eraser locked layer hide crop LoadImageAsync BinkImageElement export png jpeg webp svg ExportImageAsync",
                typeof(ImageAnnotation)),
            new("/document-annotation", "Document review",
                "Highlight text, attach callout comments and write in the margin, with every annotation listed beside the page.",
                "document review annotation highlight callout comment BinkHighlightElement BinkCalloutElement pdf contract margin notes description accessibility",
                typeof(DocumentAnnotation)),
            new("/collaboration", "Collaboration",
                "Two canvases kept in sync by exchanging operations, with live cursors, latency, an offline queue and joining by snapshot.",
                "collaboration realtime sync multiplayer presence cursors operations transport signalr websockets offline queue BinkCollaborationSession snapshot join RequestSnapshotAsync SendSnapshotAsync threading",
                typeof(Collaboration)),
        ]),
        new("Engine",
        [
            new("/input-and-accessibility", "Input and accessibility",
                "Touch against scrolling, the wheel and zoom limits, every keyboard shortcut, and what a screen reader is told.",
                "accessibility a11y keyboard shortcuts navigation tab screen reader aria WCAG touch gestures EnableTouchGestures wheel trackpad WheelMode zoom MinZoom MaxZoom DisabledShortcuts EnableKeyboardShortcuts Announce Description capabilities",
                typeof(InputAndAccessibility)),
            new("/serialization", "Serialization",
                "Save, inspect, edit and reload the vector document, and see what the compact and readable formats cost.",
                "serialization save load json format compact readable BinkSerializer TryDeserialize round trip bytes per point file version asset table limits untrusted BinkDeserializerOptions IsDirty MarkSaved metadata",
                typeof(SerializationPage)),
            new("/performance", "Performance",
                "Generate thousands of strokes and time geometry, redraw, hit testing, dragging, undo, save, load and export.",
                "performance benchmark speed strokes redraw culling hit test drag move undo save load export stream allocation zero-copy render path",
                typeof(Performance)),
            new("/extending", "Extending Bink",
                "A brush, an element type and a render target of the page's own, saved, reloaded and read by an app that lacks them.",
                "extend extension custom brush element type converter registry BinkBrushRegistry BinkElementRegistry BinkElementConverter IBinkRenderContext render target IBinkTextRasterizer unknown element plugin",
                typeof(ExtendingBink)),
        ]),
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
