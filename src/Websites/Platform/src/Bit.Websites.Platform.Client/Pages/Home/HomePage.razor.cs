using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Home;

public partial class HomePage
{
    private const string OUR_PRODUCTS_SECTION_ID = "our-products";
    private const string LIBRARIES_SECTION_ID = "libraries";
    private const string BLAZORUI_GETTING_STARTED_URL = $"{Urls.BlazorUI}getting-started";
    private const string HERO_INTRO_STATE_KEY = "home-hero-intro";

    // The hero pill announces the release the site is built with. The site takes the bit packages of the
    // current release (see the csproj), and BlazorUI's Info.Version is a ReleaseVersionLiteral that its build
    // checks against Bit.Build.props, so the pill follows every version bump - preview to stable, and on to
    // the next major - with nothing here to update. Each bit major targets the .NET of the same number.
    private static readonly string releaseMajor = Bit.BlazorUI.Info.Version.Split('.')[0];
    private static readonly bool isPreviewRelease = Bit.BlazorUI.Info.Version.Contains("-pre-");

    private bool animateHeroIntro = true;

    private ElementReference ourProductsRef = default!;
    private ElementReference librariesRef = default!;

    /// <summary>
    /// What the libraries build, in the order the page lists them; the first three lead. Each links to its
    /// libraries' own sites, taken from ProductCatalog so a renamed or moved library is one edit there.
    /// </summary>
    private static readonly Capability[] capabilities =
    [
        new("Offline sync", "Data that lives on the device and keeps working offline: an embedded database, or SQLite with EF Core in the browser.", ["BlazeDb", "Bsqlite"], Featured: true),
        new("Live collaboration", "Shared editing with presence and cursors, on a C# CRDT engine that syncs again after long offline sessions.", ["Bcrdt"], Featured: true),
        new("Image editor", "Crop, resize, adjust, annotate and export images on the user's device, with a full editor component.", ["Bmage"], Featured: true),
        new("Ink", "Signatures, sketches, whiteboards and annotations, with pressure-aware strokes and undo.", ["Bink"]),
        new("Command palette", "A keyboard-first Ctrl+K palette with fuzzy search, nested pages and shortcuts.", ["BlazorUI"]),
        new("Barcode scanner", "QR codes and barcodes from the camera or from images, decoded in C# with no SDK.", ["BbScanner"]),
        new("Motion", "Springs, gestures, drag, layout and exit animations, declared in Razor.", ["Bmotion"]),
        new("Drag and drop", "Typed drag and drop between any sources and targets.", ["Bdnd"]),
        new("Forms", "Describe a form once, from a C# type, a fluent builder or JSON Schema, and it renders and validates itself.", ["Borm"]),
        new("Telemetry", "Logs, traces, metrics and web vitals from the browser and from .NET, over OpenTelemetry.", ["Belemetry"]),
        new("Node editors", "Flow charts, diagrams and node-based editors on a pannable, zoomable canvas.", ["Blow"]),
        new("State management", "Reactive stores with persistence, undo and redo, and Redux DevTools time travel.", ["Blex"]),
    ];

    protected override async Task OnInitAsync()
    {
        // The prerendered markup plays the hero intro the moment it paints, and then the wasm app comes up,
        // throws that DOM away and rebuilds it - which replayed the animation from the start.
        // Prerender state is handed back to the client exactly once, on that boot render, so getting a value
        // out of it is the signal that the intro has already been seen and this render has to skip it.
        // Later client-side navigations back to the home page find nothing stored and animate as usual.
        var restoredFromPrerenderState = true;
        await PrerenderStateService.GetValue(HERO_INTRO_STATE_KEY, () =>
        {
            restoredFromPrerenderState = false;
            return Task.FromResult(true);
        });

        animateHeroIntro = restoredFromPrerenderState is false;
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        if (NavigationManager.Uri.Contains(OUR_PRODUCTS_SECTION_ID))
        {
            await ourProductsRef.ScrollIntoView();
        }
        else if (NavigationManager.Uri.Contains($"#{LIBRARIES_SECTION_ID}"))
        {
            await librariesRef.ScrollIntoView();
        }
    }

    private sealed record Capability(string Title, string Description, string[] LibraryNames, bool Featured = false)
    {
        public ProductLink[] Libraries { get; } = [.. LibraryNames.Select(name => ProductCatalog.All.Single(product => product.Title == name))];
    }
}
