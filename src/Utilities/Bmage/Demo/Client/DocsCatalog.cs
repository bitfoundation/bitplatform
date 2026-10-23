using Microsoft.AspNetCore.Components;

namespace Bit.Bmage.Demo.Client;

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
    public const string Name = "Bmage";

    public const string PackageId = "Bit.Bmage";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/Bmage";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "Bmage is browser-side image processing for Blazor: load, crop, resize, adjust, annotate, redact and " +
        "export images on the user's device from strongly typed C#, with a full editor component and a " +
        "headless engine. Every sample on this site runs in your browser.";

    public static readonly string[] Badges = ["MIT", "net8.0", "net9.0", "net10.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["crop", "webp", "exif", "editor", "batch"];

    /// <summary>
    /// Whether Ctrl+K / Cmd+K opens the site's search palette. "/" always does. A demo whose own
    /// library answers to Ctrl+K turns this off so the two do not fight over the key.
    /// </summary>
    public static readonly bool PaletteCtrlK = true;

    public static readonly SiteNavItem[] Primary =
    [
        new("/", "Home"),
        new("/getting-started", "Docs", MatchesDocs: true),
        new("/editor", "Editor"),
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
/// all driven from here, so adding a page means adding exactly one entry. The MCP server reads the same
/// list (Server/Services/BmageSourceCatalog.cs and BmageSearchIndex.cs): a page's description and
/// keywords here are also what an AI agent is told about it when it looks for a worked example.
/// </summary>
public static class DocsCatalog
{
    // The page types are named through Pages. on purpose: Editor, Metadata and Capabilities are also
    // namespaces of the library (Bit.Bmage.Editor, ...), and a bare name would bind to those first.
    public static readonly DocsSection[] Sections =
    [
        new("Getting started",
        [
            new("/getting-started", "Getting started",
                "Install the package, register the services, add the editor stylesheet and process your first image.",
                "install package nuget dotnet add register services addbitbmageservices stylesheet first image dispose object url",
                typeof(Pages.GettingStarted)),
            new("/hosting", "Hosting models",
                "Setting Bmage up in a Blazor Web App and in standalone WebAssembly, prerendering, Interactive Server, AOT and server-side use.",
                "hosting blazor web app standalone webassembly wasm server auto prerender prerendering aot runaotcompilation render mode program.cs index.html app.razor",
                typeof(Pages.Hosting)),
        ]),
        new("Fundamentals",
        [
            new("/pipelines", "Pipelines",
                "Immutable, composable lists of operations: geometry, resize modes, filters, smart cropping, the optimiser and execution options.",
                "pipeline imagepipeline resize crop cover maxsize fit contain rotate flip smartcrop sharpen blur adjust optimiser optimize execute progress cancellation forscale",
                typeof(Pages.Pipelines)),
            new("/layers-and-masks", "Layers and masks",
                "Selective edits through masks, image, fill and adjustment layers with blend modes, and edits saved as JSON to replay on a server.",
                "layers masks mask masked selective adjustment gradient radial graduated filter luminance colour range brush lasso blend mode multiply screen overlay color luminosity fill adjustment layer watermark cut out alpha json serialize save replay document",
                typeof(Pages.LayersAndMasks)),
            new("/formats", "Formats",
                "What decodes and encodes where, probing instead of assuming, fallbacks, size budgets, transparency and format detection.",
                "format webp avif jpeg png bmp gif svg encode decode fallback quality maxfilesize budget transparency background detect identify",
                typeof(Pages.Formats)),
            new("/capabilities", "Capabilities",
                "What this browser can actually decode, encode and render, probed live rather than guessed from a user agent.",
                "capabilities probe browser canvas limit offscreencanvas webcodecs clipboard camera simd threads device pixel ratio environment",
                typeof(Pages.Capabilities)),
            new("/metadata", "Metadata and privacy",
                "EXIF, GPS and the three metadata policies, reading and writing tags, and orientation - with a live inspector for your own photos.",
                "metadata privacy exif gps location strip remove preserve stripsensitive orientation autoorient xmp icc camera",
                typeof(Pages.Metadata)),
        ]),
        new("Workflows",
        [
            new("/avatar", "Avatar editor",
                "Crop to a square or circle around the subject, resize, and compress to a byte budget with a WebP to JPEG fallback.",
                "avatar profile picture circle square smart crop subject size budget webp fallback dominant colours",
                typeof(Pages.Avatar)),
            new("/annotate", "Screenshot annotation",
                "Paste a screenshot, add arrows, numbered steps and text, then redact the parts that must not be shared.",
                "screenshot annotate paste clipboard arrow step highlight redact redaction blur pixelate bug report",
                typeof(Pages.Annotate)),
            new("/optimize", "Upload optimisation",
                "Cap dimensions, re-encode to a modern format, strip metadata, and see exactly how many bytes that saved.",
                "optimise optimize upload compress shrink reencode webp quality bytes saved maxsize",
                typeof(Pages.Optimize)),
            new("/product", "Product images",
                "Every size a catalogue needs from one upload, on a consistent background, without ever cropping the product.",
                "product ecommerce catalogue sizes variants background padding contain thumbnails",
                typeof(Pages.Product)),
            new("/social", "Social images",
                "Compose to a platform's exact canvas, apply a look, and burn in a caption with the browser's text engine.",
                "social instagram story open graph caption text filter look vignette warmth canvas compose",
                typeof(Pages.Social)),
            new("/retouch", "Retouching and selections",
                "Straighten, white balance, remove a plain backdrop, recolour with the magic wand, paint by hand, and save it all as a project that reopens editable.",
                "retouch straighten level white balance colour cast background removal remove backdrop transparent magic wand flood select selection expand contract border paint brush eraser stroke pressure hardness project save open bmage zip",
                typeof(Pages.Retouch)),
            new("/batch", "Batch processing",
                "One pipeline over many files with bounded concurrency, live progress, cancellation and a per-file outcome.",
                "batch bulk concurrency progress cancellation errors processbatchasync many files",
                typeof(Pages.Batch)),
        ]),
        new("Components and APIs",
        [
            new("/editor", "The editor",
                "The complete BmageEditor component: crop, adjust, filter, draw, annotate, undo and export - themed, localised and right-to-left.",
                "editor bmageeditor component crop adjust filter draw annotate layers history undo redo theme localisation rtl strings toolbar panel keyboard",
                typeof(Pages.Editor)),
            new("/headless", "Headless API",
                "The same engine with no components at all: thumbnails, crops, grading, drawing with ImageCanvas, and server-side use.",
                "headless api no ui server console imagecanvas drawing thumbnail exif placeholder statistics addbitbmageheadlessservices",
                typeof(Pages.Headless)),
        ]),
        new("Guides",
        [
            new("/performance", "Performance",
                "Measured numbers, why WebAssembly needs AOT for pixel work, and how to keep pipelines fast - with a benchmark for this browser.",
                "performance speed aot interpreter benchmark fast slow memory pooling fused resize box prepass",
                typeof(Pages.Performance)),
            new("/mcp-server", "MCP server",
                "The demo's Model Context Protocol server: the tools, prompts and resources that let an AI agent write correct Bmage code - tried live.",
                "mcp model context protocol ai agent llm tools prompts resources claude cursor copilot vs code server integration review measure",
                typeof(Pages.McpServerPage)),
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

    /// <summary>Finds the catalog entry for a base-relative path, or null when the page is not in the catalog.</summary>
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
