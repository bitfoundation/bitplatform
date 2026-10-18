namespace Bit.Bmage.Demo.Client.Shared;

/// <summary>
/// One entry of the docs navigation. The list itself is <see cref="All"/>, and it is the site's
/// single index: the nav panel groups it, the command palette searches it, the landing page prints
/// it, the breadcrumb reads a page's category out of it and the pager walks it in order. A page
/// therefore appears everywhere by having an entry here, and the five of them can never disagree
/// about what the site contains.
/// <para>
/// It is also where the demo's MCP server gets the description, keywords and route it reports for a
/// demo page in <c>GetBmageSourceFiles</c>, which is why each entry carries them alongside the file
/// it is implemented in. A page therefore describes itself once, and the site's search, the nav
/// panel and an AI agent looking for a worked example all read the same words.
/// </para>
/// </summary>
/// <param name="Title">The label shown in the nav panel.</param>
/// <param name="Href">The route, relative to the app base. Empty for the landing page.</param>
/// <param name="Category">The group this page is filed under; see <see cref="Groups"/>.</param>
/// <param name="Description">What the page demonstrates, in one sentence.</param>
/// <param name="Keywords">Space-separated terms the page covers, for search and the MCP index.</param>
/// <param name="Source">The page's file name under Client/Pages, for GetBmageSourceFile.</param>
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

    // The group names, written out once so a typo in an entry below cannot silently invent a new
    // group, and in the order the docs are meant to be read.
    private const string GettingStarted = "Getting started";
    private const string Fundamentals = "Fundamentals";
    private const string Workflows = "Workflows";
    private const string Components = "Components and APIs";
    private const string Guides = "Guides";

    /// <summary>Every demo page, in the order they are meant to be read.</summary>
    public static readonly NavItem[] All =
    [
        new("Home", "", GettingStarted,
            "The landing page: what Bmage is, and the workflow tiles that link into the rest of the site.",
            "home landing overview introduction start",
            "Home.razor"),
        new("Getting started", "getting-started", GettingStarted,
            "Pick a package, register the services, add the editor stylesheet and process your first image.",
            "install package nuget dotnet add register services addbitbmageservices stylesheet first image dispose object url",
            "GettingStarted.razor"),
        new("Hosting models", "hosting", GettingStarted,
            "Setting Bmage up in a Blazor Web App and in standalone WebAssembly, prerendering, Interactive Server, AOT and server-side use.",
            "hosting blazor web app standalone webassembly wasm server auto prerender prerendering aot runaotcompilation render mode program.cs index.html app.razor",
            "Hosting.razor"),
        new("Pipelines", "pipelines", Fundamentals,
            "Immutable, composable lists of operations: geometry, resize modes, filters, smart cropping, the optimiser and execution options.",
            "pipeline imagepipeline resize crop cover maxsize fit contain rotate flip smartcrop sharpen blur adjust optimiser optimize execute progress cancellation forscale",
            "Pipelines.razor"),
        new("Formats", "formats", Fundamentals,
            "What decodes and encodes where, probing instead of assuming, fallbacks, size budgets, transparency and format detection.",
            "format webp avif jpeg png bmp gif svg encode decode fallback quality maxfilesize budget transparency background detect identify",
            "Formats.razor"),
        new("Capabilities", "capabilities", Fundamentals,
            "What this browser can actually decode, encode and render, probed live rather than guessed from a user agent.",
            "capabilities probe browser canvas limit offscreencanvas webcodecs clipboard camera simd threads device pixel ratio environment",
            "Capabilities.razor"),
        new("Metadata and privacy", "metadata", Fundamentals,
            "EXIF, GPS and the three metadata policies, reading and writing tags, and orientation - with a live inspector for your own photos.",
            "metadata privacy exif gps location strip remove preserve stripsensitive orientation autoorient xmp icc camera",
            "Metadata.razor"),
        new("Avatar editor", "avatar", Workflows,
            "Crop to a square or circle around the subject, resize, and compress to a byte budget with a WebP to JPEG fallback.",
            "avatar profile picture circle square smart crop subject size budget webp fallback dominant colours",
            "Avatar.razor"),
        new("Screenshot annotation", "annotate", Workflows,
            "Paste a screenshot, add arrows, numbered steps and text, then redact the parts that must not be shared.",
            "screenshot annotate paste clipboard arrow step highlight redact redaction blur pixelate bug report",
            "Annotate.razor"),
        new("Upload optimisation", "optimize", Workflows,
            "Cap dimensions, re-encode to a modern format, strip metadata, and see exactly how many bytes that saved.",
            "optimise optimize upload compress shrink reencode webp quality bytes saved maxsize",
            "Optimize.razor"),
        new("Product images", "product", Workflows,
            "Every size a catalogue needs from one upload, on a consistent background, without ever cropping the product.",
            "product ecommerce catalogue sizes variants background padding contain thumbnails",
            "Product.razor"),
        new("Social images", "social", Workflows,
            "Compose to a platform's exact canvas, apply a look, and burn in a caption with the browser's text engine.",
            "social instagram story open graph caption text filter look vignette warmth canvas compose",
            "Social.razor"),
        new("Batch processing", "batch", Workflows,
            "One pipeline over many files with bounded concurrency, live progress, cancellation and a per-file outcome.",
            "batch bulk concurrency progress cancellation errors processbatchasync many files",
            "Batch.razor"),
        new("The editor", "editor", Components,
            "The complete BmageEditor component: crop, adjust, filter, draw, annotate, undo and export - themed, localised and right-to-left.",
            "editor bmageeditor component crop adjust filter draw annotate layers history undo redo theme localisation rtl strings toolbar panel keyboard",
            "Editor.razor"),
        new("Headless API", "headless", Components,
            "The same engine with no components at all: thumbnails, crops, grading, drawing with ImageCanvas, and server-side use.",
            "headless api no ui server console imagecanvas drawing thumbnail exif placeholder statistics addbitbmageheadlessservices",
            "Headless.razor"),
        new("Performance", "performance", Guides,
            "Measured numbers, why WebAssembly needs AOT for pixel work, and how to keep pipelines fast - with a benchmark for this browser.",
            "performance speed aot interpreter benchmark fast slow memory pooling fused resize box prepass",
            "Performance.razor"),
        new("MCP server", "mcp-server", Guides,
            "The demo's Model Context Protocol server: the tools, prompts and resources that let an AI agent write correct Bmage code - tried live.",
            "mcp model context protocol ai agent llm tools prompts resources claude cursor copilot vs code server integration review measure",
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
    /// title and the category: a reader searching for "exif" or "webp" is naming the feature, not
    /// the page it happens to live on, and every entry carries the terms it covers.
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
        Fundamentals => Icons.Pipeline,
        Workflows => Icons.Image,
        Components => Icons.Code,
        Guides => Icons.Book,
        _ => Icons.Book,
    };
}
