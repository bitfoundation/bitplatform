using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bit.BbScanner.Demo.Server.Dtos;
using Bit.BbScanner.Demo.Server.Services;

namespace Bit.BbScanner.Demo.Server.Controllers;

/// <summary>
/// The BbScanner MCP server: the tools an AI agent calls to write barcode scanning code with Bit.BbScanner
/// without guessing at its API.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the XML documentation
/// compiled into Bit.BbScanner, the README, the symbology flags and the option defaults, the decoder itself,
/// the shipped stylesheet and the demo's source files - so an agent gets what the current version actually
/// does rather than a snapshot someone wrote down. The same methods are exposed as plain HTTP GET endpoints
/// under /api/mcp/..., which makes each of them inspectable from a browser.
/// </para>
/// <para>
/// One rule holds across the reference tools: a tool takes the key of the thing you want and answers with
/// that thing; leave the key out and it answers with the index of what there is. That is what a listing tool
/// used to be - and a listing tool costs a name, a description and a schema in every request an agent makes,
/// forever, to say what an optional argument says for nothing.
/// </para>
/// <para>
/// Each tool carries the two annotations that mean something for a tool which only reads: <c>ReadOnly</c>,
/// which is what lets a client run it without stopping to ask a person, and <c>OpenWorld = false</c>, which
/// says the answers come from a closed, known body of material rather than from the internet.
/// </para>
/// <para>
/// Only the two tools whose answer is machine-actionable - a ranked hit carrying its own follow-up call, a
/// decode report - set <c>UseStructuredContent</c> and publish an output schema. The rest answer in Markdown:
/// a structured answer goes over the wire twice, once as the object and once as the JSON text the spec asks
/// for on its behalf, and documentation read as Markdown is smaller than either half.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public class McpController : ControllerBase
{
    // The README's longest sections and the demo's longest pages land far below the cap; it is
    // there so a pathological answer cannot dominate a client's context window.
    private const int MaxDocumentLength = 40_000;

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBbScanner), Title = "Search everything about Bit.BbScanner", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches everything known about Bit.BbScanner at once - the reference guide, every public type and member, the supported symbologies, the setup guides, the stylesheet's classes and custom properties, and the demo's source files - and returns the best matches, each with the exact follow-up call that returns its full text. Call it first whenever you do not already know which section or type holds the answer, then call the hit's tool verbatim. Example queries: 'scan qr code', 'torch', 'scan image file', 'duplicate', 'ean13 upc', 'scan region', 'permission denied'.")]
    public BbScannerSearchResultDto SearchBbScanner(
        [Description("What you are looking for, in the words you would use for it - a feature, a symptom, or a member name. A few words rank better than a whole sentence.")] string query,
        [Description("How many hits to return. Clamped to 1..50.")] int limit = 12)
    {
        return BbScannerSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBbScannerSetupGuide), Title = "Setup guide for one render mode", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the complete wiring needed to add Bit.BbScanner to a Blazor app in one render mode: 'wasm' (InteractiveWebAssembly, which is how this demo is built and comes with its real files), 'standalone-wasm', 'auto' (InteractiveAuto), 'server' (InteractiveServer) or 'hybrid' (MAUI, WPF, Windows Forms). Call this before writing setup code - there are no services to register and the stylesheet injects itself, but camera scanning runs on WebAssembly only, so where the component may live, and what to do instead under Server or Hybrid rendering, differ per render mode.")]
    public string GetBbScannerSetupGuide(
        // Declared rather than merely described: the values then sit in the JSON schema itself,
        // where a model cannot pass another one past them, and a client offers them as completions.
        [AllowedValues("wasm", "standalone-wasm", "auto", "server", "hybrid")]
        [Description("The app's Blazor render mode.")] string renderMode)
    {
        return BbScannerSetupGuide.Get(renderMode)
            ?? $"'{renderMode}' is not a known render mode. Use one of: {string.Join(", ", BbScannerSetupGuide.RenderModes)}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBbScannerGuideSection), Title = "Read a reference-guide section", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one section of the Bit.BbScanner reference guide (the library's README) as Markdown, with its code samples - e.g. 'Getting started', 'Symbologies', 'Using the component', 'Using the decoder without Blazor', 'Hosting models', 'Styling', 'Errors and permissions', 'What it reads, and what it does not', 'Performance', 'PDF417 status'. Sub-sections are included, and heading matching ignores case and punctuation. Omit the heading for the index of sections with their sizes.")]
    public string GetBbScannerGuideSection(
        [Description("The section's heading, e.g. 'Using the decoder without Blazor'. Case and punctuation are ignored; a heading the guide uses twice is told apart as 'Parent > Heading', as the index shows. Omit it for the index of sections.")] string? heading = null)
    {
        if (string.IsNullOrWhiteSpace(heading)) return BbScannerSourceCatalog.RenderGuideIndex();

        var section = BbScannerSourceCatalog.GetGuideSection(heading);

        if (section is not null) return Truncate(section);

        var candidates = BbScannerSourceCatalog.GuideSections
            .Where(s => s.Heading.Contains(heading, StringComparison.OrdinalIgnoreCase) || heading.Contains(s.Heading, StringComparison.OrdinalIgnoreCase))
            .Select(s => $"'{s.Key}'")
            .Distinct(StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The guide has no section called '{heading}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The guide has no section called '{heading}'. Call GetBbScannerGuideSection with no heading for the index of sections, or SearchBbScanner to find the topic.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBbScannerApi), Title = "Public API reference", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the public API of Bit.BbScanner as Markdown, read out of the shipped assembly: one type with every Blazor parameter, property, method, event or enum value it has - each with its C# type, its real default value and its documentation. Omit the type name for the index of every public type. Call it before using a member you are unsure about, e.g. 'BbScanner', 'BbScannerOptions', 'BbBarcodeFormat', 'BbBarcodeResult', 'BbBarcodeResultMetadata', 'BbBarcodeDecoder', 'BbScanRegion', 'BbScannerException', 'BbCameraInfo'.")]
    public string GetBbScannerApi(
        [Description("The type's name without its namespace, e.g. 'BbScanner' or 'BbScannerOptions'. Omit it for the index of types.")] string? typeName = null)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return BbScannerApiCatalog.RenderIndex();

        var rendered = BbScannerApiCatalog.RenderType(typeName);

        if (rendered is not null) return Truncate(rendered);

        var candidates = BbScannerApiCatalog.Types
            .Where(t => t.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"Bit.BbScanner has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"Bit.BbScanner has no public type called '{typeName}'. The component is 'BbScanner' and every other public type is prefixed 'Bb' (e.g. 'BbBarcodeResult' rather than 'BarcodeResult', 'BbScannerOptions' rather than 'ScannerOptions'); call GetBbScannerApi with no type name for the index.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBbScannerFormats), Title = "Supported symbologies and their options", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the symbologies Bit.BbScanner reads - QR Code, Data Matrix, PDF417, Code 128, Code 39, EAN-13, EAN-8, UPC-A, UPC-E, ITF - read out of the library: each one's BbBarcodeFormat flag and its value, what of it is read, whether it is enabled by default and whether the pipeline actually creates its reader, and the BbScannerOptions members that steer it with their real defaults. Omit the format for the table of all of them. Use it to pick the Formats to pass and the options worth changing.")]
    public string GetBbScannerFormats(
        [Description("The symbology, as its BbBarcodeFormat member ('QrCode', 'Ean13') or its usual name ('QR Code', 'EAN-13'). Omit it for the table of every symbology.")] string? format = null)
    {
        if (string.IsNullOrWhiteSpace(format)) return BbScannerFormatCatalog.RenderIndex();

        return BbScannerFormatCatalog.Render(format)
            ?? $"Bit.BbScanner reads no symbology called '{format}'. The formats are: {string.Join(", ", BbScannerFormatCatalog.Formats.Select(f => f.Name))}; combine them with '|', or use the composites AllLinear, AllMatrix and All.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBbScannerSymbol), Title = "Decode a rendered symbol", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Renders a symbol of one symbology with the given payload, optionally rotated and blurred the way a camera frame is, and runs Bit.BbScanner's own BbBarcodeDecoder over it twice - with the live camera defaults and with BbScannerOptions.ForStillImages() - reporting the BbBarcodeResult each produced: its text, format, raw bytes, corners, bounding box, metadata (error correction level, version, GS1, ECI, structured append) and timings, with notes on what to change when one of them does not read it. Call it with only a format for a sample payload, the quickest way to see what a result looks like.")]
    public BbScannerDecodeReportDto InspectBbScannerSymbol(
        [Description("The symbology, e.g. 'QrCode', 'DataMatrix', 'Code128', 'Ean13' or 'Itf'.")] string format = "QrCode",
        [Description("The payload to encode. Omit it for a sample payload. An EAN-13, EAN-8 or UPC-A payload without its check digit gets one.")] string? text = null,
        [Description("Pixels per module of the rendered symbol, 1..12. Below about 2 is too small to read.")] int moduleSize = 4,
        [Description("Degrees to rotate the frame by, -360..360.")] double rotation = 0,
        [Description("Gaussian blur, as a sigma in pixels, 0..5.")] double blur = 0)
    {
        return BbScannerDecodeLab.Inspect(format, text, moduleSize, rotation, blur);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBbScannerSourceFile), Title = "Read a source file", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one working Bit.BbScanner source file verbatim - e.g. 'Demo/Client/Pages/Scan.razor' (every parameter of the component in one playground), 'Demo/Client/Pages/Camera.razor' (camera selection, torch and zoom), 'Demo/Client/Pages/Image.razor' (scanning a file or a buffer), 'Demo/Client/Pages/Customize.razor' (overlays, message templates and theming), 'Demo/Client/Pages/Errors.razor', or 'Library/Styles/bit-bbscanner.css' (the shipped stylesheet). Omit the path for the index of files: the demo pages, each a working scanner, their shared components, and the hosting files of a Blazor Web App. These are worked examples - for what a parameter means, GetBbScannerApi answers in a fraction of the characters.")]
    public string GetBbScannerSourceFile(
        [Description("The file's path as the index lists it, e.g. 'Demo/Client/Pages/Scan.razor'. Omit it for the index of files.")] string? path = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return BbScannerSourceCatalog.RenderSourceIndex();

        var content = BbScannerSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BbScannerSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBbScannerSourceFile with no path for the index of files.";
        }

        return Truncate(content);
    }

    internal static string Truncate(string text)
    {
        if (text.Length <= MaxDocumentLength) return text;

        // Never between the two halves of a surrogate pair: half a pair is not text, and a client
        // that re-encodes the answer turns it into a replacement character or rejects it outright.
        var cut = char.IsHighSurrogate(text[MaxDocumentLength - 1]) ? MaxDocumentLength - 1 : MaxDocumentLength;

        return $"{text[..cut]}\n\n[truncated - the full text is longer than {MaxDocumentLength} characters]";
    }
}
