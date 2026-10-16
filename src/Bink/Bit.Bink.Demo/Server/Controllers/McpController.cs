using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bit.Bink.Demo.Server.Dtos;
using Bit.Bink.Demo.Server.Services;

namespace Bit.Bink.Demo.Server.Controllers;

/// <summary>
/// The Bink MCP server: the tools an AI agent calls to write drawing code with Bit.Bink without
/// guessing at its API.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the XML
/// documentation compiled into Bit.Bink, the README, the brush registry, the serializer and the
/// exporters, the shipped stylesheet and the demo's source files - so an agent gets what the current
/// version actually does rather than a snapshot someone wrote down. The same methods are exposed as
/// plain HTTP GET endpoints under /api/mcp/..., which makes each of them inspectable from a browser.
/// </para>
/// <para>
/// One rule holds across the reference tools: a tool takes the key of the thing you want and
/// answers with that thing; leave the key out and it answers with the index of what there is. That
/// is what a listing tool used to be - and a listing tool costs a name, a description and a schema
/// in every request an agent makes, forever, to say what an optional argument says for nothing.
/// </para>
/// <para>
/// Each tool carries the two annotations that mean something for a tool which only reads:
/// <c>ReadOnly</c>, which is what lets a client run it without stopping to ask a person, and
/// <c>OpenWorld = false</c>, which says the answers come from a closed, known body of material
/// rather than from the internet.
/// </para>
/// <para>
/// Only the two tools whose answer is machine-actionable - a ranked hit carrying its own follow-up
/// call, a document report - set <c>UseStructuredContent</c> and publish an output schema. The rest
/// answer in Markdown: a structured answer goes over the wire twice, once as the object and once as
/// the JSON text the spec asks for on its behalf, and documentation read as Markdown is smaller than
/// either half.
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
    [McpServerTool(Name = nameof(SearchBink), Title = "Search everything about Bit.Bink", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches everything known about Bit.Bink at once - the reference guide, every public type and member, the built-in brushes, the setup guides, the stylesheet's classes and the demo's source files - and returns the best matches, each with the exact follow-up call that returns its full text. Call it first whenever you do not already know which section or type holds the answer, then call the hit's tool verbatim. Example queries: 'signature pad export png', 'pressure', 'undo', 'save and load json', 'eraser', 'touch scrolling'.")]
    public BinkSearchResultDto SearchBink(
        [Description("What you are looking for, in the words you would use for it - a feature, a symptom, or a member name. A few words rank better than a whole sentence.")] string query,
        [Description("How many hits to return. Clamped to 1..50.")] int limit = 12)
    {
        return BinkSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBinkSetupGuide), Title = "Setup guide for one render mode", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the complete wiring needed to add Bit.Bink to a Blazor app in one render mode: 'server' (InteractiveServer), 'wasm' (InteractiveWebAssembly, which is how this demo is built and comes with its real files), 'auto' (InteractiveAuto), 'standalone-wasm' or 'hybrid' (MAUI, WPF, Windows Forms). Call this before writing setup code - there are no services to register and the stylesheet injects itself, but which project the canvas belongs in and what Blazor Server's latency changes differ per render mode.")]
    public string GetBinkSetupGuide(
        // Declared rather than merely described: the values then sit in the JSON schema itself,
        // where a model cannot pass another one past them, and a client offers them as completions.
        [AllowedValues("server", "wasm", "auto", "standalone-wasm", "hybrid")]
        [Description("The app's Blazor render mode.")] string renderMode)
    {
        return BinkSetupGuide.Get(renderMode)
            ?? $"'{renderMode}' is not a known render mode. Use one of: {string.Join(", ", BinkSetupGuide.RenderModes)}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBinkGuideSection), Title = "Read a reference-guide section", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one section of the Bit.Bink reference guide (the library's README) as Markdown, with its code samples - e.g. 'Getting started', 'A signature field', 'Drawing, brushes and pressure', 'Erasing', 'Serialization', 'SVG and PNG export', 'Hosting models', 'Mobile, touch and stylus', 'Collaboration', 'Extending Bink'. Sub-sections are included, and heading matching ignores case and punctuation. Omit the heading for the index of sections with their sizes.")]
    public string GetBinkGuideSection(
        [Description("The section's heading, e.g. 'Saving and loading'. Case and punctuation are ignored; a heading the guide uses twice is told apart as 'Parent > Heading', as the index shows. Omit it for the index of sections.")] string? heading = null)
    {
        if (string.IsNullOrWhiteSpace(heading)) return BinkSourceCatalog.RenderGuideIndex();

        var section = BinkSourceCatalog.GetGuideSection(heading);

        if (section is not null) return Truncate(section);

        var candidates = BinkSourceCatalog.GuideSections
            .Where(s => s.Heading.Contains(heading, StringComparison.OrdinalIgnoreCase) || heading.Contains(s.Heading, StringComparison.OrdinalIgnoreCase))
            .Select(s => $"'{s.Key}'")
            .Distinct(StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The guide has no section called '{heading}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The guide has no section called '{heading}'. Call GetBinkGuideSection with no heading for the index of sections, or SearchBink to find the topic.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBinkApi), Title = "Public API reference", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the public API of Bit.Bink as Markdown, read out of the shipped assembly: one type with every Blazor parameter, property, method, event or enum value it has - each with its C# type, its real default value and its documentation. Omit the type name for the index of every public type. Call it before using a member you are unsure about, e.g. 'BinkCanvas', 'BinkDocument', 'BinkStroke', 'BinkBrush', 'BinkSerializer', 'BinkSvgExporter', 'BinkPngExportOptions', 'BinkEraserTool', 'BinkCollaborationSession'.")]
    public string GetBinkApi(
        [Description("The type's name without its namespace, e.g. 'BinkCanvas' or 'BinkSvgExportOptions'. Omit it for the index of types.")] string? typeName = null)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return BinkApiCatalog.RenderIndex();

        var rendered = BinkApiCatalog.RenderType(typeName);

        if (rendered is not null) return Truncate(rendered);

        var candidates = BinkApiCatalog.Types
            .Where(t => t.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"Bit.Bink has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"Bit.Bink has no public type called '{typeName}'. Every public type is prefixed 'Bink' (e.g. 'BinkDocument' rather than 'InkDocument', 'BinkPenBrush' rather than 'PenBrush'); call GetBinkApi with no type name for the index.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBinkBrushes), Title = "Built-in brushes and their defaults", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the brushes Bit.Bink registers by default - pen, pencil, marker, highlighter, pixel-eraser - read out of the library: each brush's real default colour, width, opacity, blend mode, pressure curve, smoothing, spacing and taper, whether it draws a pressure-following outline or a constant-width centerline, and the JSON a stroke stores it as. Omit the kind for the table of all of them. Use it to pick a brush and the `with { ... }` values to change.")]
    public string GetBinkBrushes(
        [Description("The brush's serialized kind, e.g. 'pen' or 'highlighter' (its type name, e.g. 'BinkMarkerBrush', also works). Omit it for the table of every built-in brush.")] string? kind = null)
    {
        if (string.IsNullOrWhiteSpace(kind)) return BinkBrushCatalog.RenderIndex();

        return BinkBrushCatalog.Render(kind)
            ?? $"Bit.Bink registers no brush called '{kind}'. The built-in kinds are: {string.Join(", ", BinkBrushCatalog.Brushes.Select(brush => brush.Kind))}. A custom brush derives from BinkBrush and is registered with BinkBrushRegistry - see GetBinkGuideSection(heading: \"A custom brush\").";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBinkDocument), Title = "Inspect a Bink document", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Runs Bit.Bink's own serializer and exporters on a saved Bink JSON document and reports what the library makes of it: whether BinkSerializer.TryDeserialize accepts it under the untrusted-input limits (and its error when not), its page, layers, element types, brush kinds and point encodings, content bounds, compact and readable sizes, whether a save round trip is stable, what the server-side PNG exporter has to skip, and optionally its SVG - with notes on what to change. Call it with no document for a sample document written by the library, the quickest way to see the format.")]
    public BinkDocumentReportDto InspectBinkDocument(
        [Description("The document's JSON, as BinkSerializer.Serialize writes it (up to 2 MB). Omit it to get a sample document instead.")] string? json = null,
        [Description("True to also return the document exported by BinkSvgExporter (truncated past 30,000 characters).")] bool includeSvg = false,
        [Description("The region the SVG export covers: 'Page' (the default, the document's page) or 'Content' (the ink plus padding).")] string region = "Page")
    {
        return BinkDocumentLab.Inspect(json, includeSvg, region);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBinkSourceFile), Title = "Read a source file", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one working Bit.Bink source file verbatim - e.g. 'Demo/Client/Pages/Signature.razor' (a signature field with validation and export), 'Demo/Client/Pages/Whiteboard.razor' (every tool, layers and undo), 'Demo/Client/Pages/SerializationPage.razor' (saving and loading), 'Demo/Client/Pages/Collaboration.razor', or 'Library/Styles/bit-bink.css' (the shipped stylesheet). Omit the path for the index of files: the demo pages, each a working drawing surface, their shared toolbars, and the hosting files of a Blazor Web App. These are worked examples - for what a parameter means, GetBinkApi answers in a fraction of the characters.")]
    public string GetBinkSourceFile(
        [Description("The file's path as the index lists it, e.g. 'Demo/Client/Pages/Signature.razor'. Omit it for the index of files.")] string? path = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return BinkSourceCatalog.RenderSourceIndex();

        var content = BinkSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BinkSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBinkSourceFile with no path for the index of files.";
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
