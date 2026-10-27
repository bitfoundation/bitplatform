using Bit.BbScanner.Demo.Client.Pages;
using Microsoft.AspNetCore.Components;

namespace Bit.BbScanner.Demo.Client;

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
    public const string Name = "BbScanner";

    public const string PackageId = "Bit.BbScanner";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/BbScanner";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "BbScanner is a barcode scanner for Blazor with the decoding written from scratch in C#: QR Code, " +
        "Micro QR, Data Matrix, PDF417, Aztec, MaxiCode, Code 128, Code 39, Code 93, Codabar, EAN, UPC, ITF and " +
        "GS1 DataBar, from the camera or from still images, with no barcode SDK and no JavaScript scanner " +
        "library. This site is itself a Blazor WebAssembly app built on it.";

    public static readonly string[] Badges = ["MIT", "net8.0", "net9.0", "net10.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["qr", "torch", "region", "image", "options"];

    /// <summary>
    /// Whether Ctrl+K / Cmd+K opens the site's search palette. "/" always does. A demo whose own
    /// library answers to Ctrl+K turns this off so the two do not fight over the key.
    /// </summary>
    public static readonly bool PaletteCtrlK = true;

    public static readonly SiteNavItem[] Primary =
    [
        new("/", "Home"),
        new("/getting-started", "Docs", MatchesDocs: true),
        new("/scan", "Playground"),
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
                "Install the package, add the namespace and put a working scanner on any interactive page - WebAssembly, Server, Auto or Hybrid; there is nothing to register.",
                "install setup nuget package quick start using namespace rendermode InteractiveWebAssembly InteractiveServer InteractiveAuto blazor server hybrid maui webview android ios windows camera permission first scanner pipeline how it works",
                typeof(GettingStarted)),
            new("/samples", "Sample barcodes",
                "One scannable example of every supported symbology, drawn as SVG and decoded in place without a camera.",
                "samples qr micro qr data matrix pdf417 aztec maxicode code 128 code 39 code 93 codabar ean-13 ean-8 upc-a upc-e itf gs1 databar limited expanded symbology generated svg self test",
                typeof(Samples)),
        ]),
        new("Scanning",
        [
            new("/scan", "Live playground",
                "The scanner's parameters in one place - formats, capture, region, duplicates, symbols per frame, after-scan behaviour, options, feedback and chrome - applied to the running camera.",
                "playground parameters formats targetframerate maxprocessingsize requestedwidth requestedheight region options bbscanneroptions maxsymbolsperframe afterscan feedback showcontrols controlscontent childcontent fill codabarstartstop requireeanextension",
                typeof(Scan)),
            new("/lifecycle", "Lifecycle & duplicates",
                "StartAsync, StopAsync, Pause and Resume driven from the page, the status each one leads to, DuplicateSuppressionWindow, AfterScan, AcceptResult and background behaviour.",
                "lifecycle status start stop pause resume autostart duplicate suppression window once per session InfiniteTimeSpan ResetDuplicateFilter AfterScan AcceptResult filter ReleaseCameraWhenHidden background feedback vibrate beep dispose camera release",
                typeof(Lifecycle)),
            new("/camera", "Camera, torch & zoom",
                "Enumerate the cameras, switch between them, turn the torch and the zoom on where the hardware offers them, and capture a full resolution frame.",
                "camera device DeviceId switch GetCamerasAsync SwitchCameraAsync OnCameraChanged BbCameraInfo torch flashlight SetTorchAsync zoom SetZoomAsync facing front rear mirrored ScanCurrentFrameAsync capture full frame",
                typeof(Camera)),
            new("/region", "Scan region",
                "Aim the decoder at part of the frame with a normalized BbScanRegion; corner points still come back in frame pixels.",
                "region BbScanRegion CenteredSquare KeepSquare ForFrame crop aim mask corners bounding box normalized",
                typeof(Region)),
            new("/image", "Image & file scanning",
                "Scan a picked or dropped photo, an InputFile upload with ScanImageAsync, or pixels the application already has with DecodeImage and DecodeAllInImage.",
                "image file picker photo still drag drop OpenFilePickerAsync ScanImageAsync InputFile DecodeImage DecodeAllInImage MaxImageFileSize MaxSymbolsPerFrame ForStillImages rgba upload InvalidImage",
                typeof(Image)),
        ]),
        new("Advanced",
        [
            new("/performance", "Performance",
                "An in-browser decoder benchmark and the live cost of a camera frame, stage by stage.",
                "performance benchmark speed fps frame time milliseconds diagnostics MaxScanLines TryHarder DownsampleFactor",
                typeof(Performance)),
            new("/customize", "Customisation",
                "Restyle the scanner through its CSS custom properties, replace the reticle and the messages with render fragments, and translate every text it shows.",
                "customise customize theme css custom properties CssClass overlay MessageTemplate ControlsContent ChildContent reticle accent radius style Texts BbScannerTexts language translation localization accessibility rtl",
                typeof(Customize)),
            new("/errors", "Errors & permissions",
                "Permission denied, no camera, camera in use, an image refused: every failure classified as a BbScannerErrorKind, with how to handle it, and the permission checked before asking.",
                "errors permissions denied NotSupported NoCamera CameraUnavailable ConstraintsUnsatisfied InvalidImage BbScannerException OnError GetCameraPermissionAsync BbCameraPermission https secure context iframe webview",
                typeof(Errors)),
            new("/api", "API reference",
                "Every parameter, event, method and property of BbScanner, its result, option, region, camera and diagnostics types, its enums and the headless decoder.",
                "api reference parameters events methods properties BbBarcodeResult BbBarcodeResultMetadata BbScannerOptions BbScanRegion BbCameraInfo BbScanDiagnostics BbScannerTexts BbBarcodeDecoder enums IBbRowDecoder IBbMatrixDecoder headless",
                typeof(Api)),
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
