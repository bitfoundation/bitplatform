using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bit.Bdnd.Demo.Server.Dtos;
using Bit.Bdnd.Demo.Server.Services;

namespace Bit.Bdnd.Demo.Server.Controllers;

/// <summary>
/// The Bdnd MCP server: the tools an AI agent calls to write drag-and-drop code with Bit.Bdnd
/// without guessing at its API - or borrowing React DnD's.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the XML
/// documentation compiled into Bit.Bdnd, the README, the components themselves and the demo's source
/// files - so an agent gets what the current version actually does rather than a snapshot someone
/// wrote down. The same methods are exposed as plain HTTP GET endpoints under /api/mcp/..., which
/// makes each of them inspectable from a browser.
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
/// call, a drop report - set <c>UseStructuredContent</c> and publish an output schema. The rest
/// answer in Markdown: a structured answer goes over the wire twice, once as the object and once as
/// the JSON text the spec asks for on its behalf, and documentation read as Markdown is smaller than
/// either half. There is no theme tool, because Bit.Bdnd ships no stylesheet: the look of a drag is
/// the app's own markup, rendered from the collected state.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public class McpController : ControllerBase
{
    // The longest real answers are the README's longest sections and the drop target's reference, all
    // well under 20,000 characters. The cap sits far above them so each arrives whole, and is there so
    // a pathological answer cannot dominate a client's context window.
    private const int MaxDocumentLength = 60_000;

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBdnd), Title = "Search everything about Bit.Bdnd", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches everything known about Bit.Bdnd at once - the reference guide, every public type and member, the setup guides and the demo's pages and source files - and returns the best matches, each with the exact follow-up call that returns its full text. Call it first whenever you do not already know which section or type holds the answer, then call the hit's tool verbatim. Example queries: 'sortable list', 'drop files from desktop', 'custom drag preview', 'drag handle', 'nested drop targets', 'drop result', 'Blazor Server'.")]
    public BdndSearchResultDto SearchBdnd(
        [Description("What you are looking for, in the words you would use for it - a feature, a symptom, or a member name. A few words rank better than a whole sentence.")] string query,
        [Description("How many hits to return. Clamped to 1..50.")] int limit = 12)
    {
        return BdndSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBdndSetupGuide), Title = "Setup guide for one render mode", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the complete wiring needed to add Bit.Bdnd to a Blazor app in one render mode: 'server' (InteractiveServer), 'wasm' (InteractiveWebAssembly, which is how this demo is built and comes with its real files), 'auto' (InteractiveAuto), 'standalone-wasm' or 'hybrid' (MAUI, WPF, Windows Forms). Call this before writing setup code - builder.Services.AddBitBdndServices() is required (in a Blazor Web App in both Program.cs files), there is no stylesheet to link, every component must sit inside a BdndProvider, and the page must render interactively.")]
    public string GetBdndSetupGuide(
        // Declared rather than merely described: the values then sit in the JSON schema itself,
        // where a model cannot pass another one past them, and a client offers them as completions.
        [AllowedValues("server", "wasm", "auto", "standalone-wasm", "hybrid")]
        [Description("The app's Blazor render mode.")] string renderMode)
    {
        return Truncate(BdndSetupGuide.Get(renderMode)
            ?? $"'{renderMode}' is not a known render mode. Use one of: {string.Join(", ", BdndSetupGuide.RenderModes)}.");
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBdndGuideSection), Title = "Read a reference-guide section", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one section of the Bit.Bdnd reference guide (the library's README) as Markdown, with its code samples - e.g. 'Getting started', 'The provider', 'Draggables', 'Drop targets', 'Drop results', 'Nesting', 'Custom drag layers', 'Drag handles', 'Sortable lists', 'Native drags', 'The monitor', 'Hosting models', 'Troubleshooting', 'Coming from React DnD'. Sub-sections are included, and heading matching ignores case and punctuation. Omit the heading for the index of sections with their sizes.")]
    public string GetBdndGuideSection(
        [Description("The section's heading, e.g. 'Drop results'. Case and punctuation are ignored; a heading the guide uses twice is told apart as 'Parent > Heading', as the index shows. Omit it for the index of sections.")] string? heading = null)
    {
        if (string.IsNullOrWhiteSpace(heading)) return BdndSourceCatalog.RenderGuideIndex();

        var section = BdndSourceCatalog.GetGuideSection(heading);

        if (section is not null) return Truncate(section);

        var candidates = BdndSourceCatalog.GuideSections
            .Where(s => s.Heading.Contains(heading, StringComparison.OrdinalIgnoreCase) || heading.Contains(s.Heading, StringComparison.OrdinalIgnoreCase))
            .Select(s => $"'{s.Key}'")
            .Distinct(StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The guide has no section called '{heading}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The guide has no section called '{heading}'. Call GetBdndGuideSection with no heading for the index of sections, or SearchBdnd to find the topic.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBdndApi), Title = "Public API reference", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the public API of Bit.Bdnd as Markdown, read out of the shipped assembly: one type with every Blazor parameter, property, method or constant it has - each with its C# type, its real default value and its documentation. Omit the type name for the index of every public type. Call it before using a member you are unsure about, e.g. 'BdndDraggable', 'BdndDropTarget', 'BdndDragLayer', 'BdndDragHandle', 'BdndProvider', 'BdndDropState', 'BdndDragEndContext', 'BdndDropEventArgs', 'IBdndMonitor', 'BdndNativeTypes', 'BdndInterop'.")]
    public string GetBdndApi(
        [Description("The type's name without its namespace or generic arguments, e.g. 'BdndDropTarget' or 'IBdndMonitor'. Omit it for the index of types.")] string? typeName = null)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return BdndApiCatalog.RenderIndex();

        var rendered = BdndApiCatalog.RenderType(typeName);

        if (rendered is not null) return Truncate(rendered);

        var candidates = BdndApiCatalog.Types
            .Where(t => t.Name.Contains(typeName.Trim(), StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"Bit.Bdnd has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"Bit.Bdnd has no public type called '{typeName}'. Every public type is prefixed 'Bdnd' (e.g. 'BdndDraggable' rather than 'useDrag', 'BdndDropTarget' rather than 'useDrop', 'IBdndMonitor' rather than 'DropTargetMonitor'); call GetBdndApi with no type name for the index.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBdndDrop), Title = "Inspect a Bdnd drag", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Runs one drag through the real BdndProvider, BdndDraggable and BdndDropTarget components and reports what the library does with it: for every drop target its CanDrop, IsOver and IsOverShallow while the item hovers, the order OnDrop runs in across nested targets, the Result each one published and the DidDrop and DropResult the draggable's OnEnd receives - with notes on what to fix (a type the target does not accept, a drop that falls through to an ancestor, an outer result overriding an inner one, near-miss type names). The scenario is JSON: a draggable (type, canDrag, dropEffect), a tree of targets (id, accept, canDrop, result, children) and the id of the hovered target. A native type (BdndNativeTypes.File...) as the draggable's type runs a drag from outside the app through OnNativeDrop. Call it with no json for a sample scenario.")]
    public Task<BdndDropReportDto> InspectBdndDrop(
        [Description("The scenario as JSON (up to 256 KB): { \"draggable\": { \"type\": \"card\" }, \"targets\": [ { \"id\": \"list\", \"accept\": [\"card\"], \"result\": \"list\", \"children\": [ ... ] } ], \"hover\": \"list\" }. 'canDrop': false emulates a CanDropPredicate answering false; 'hover' is the innermost target under the pointer (null for a drop outside every target). Omit it to get a sample scenario instead.")] string? json = null)
    {
        return BdndDropLab.InspectAsync(json);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBdndSourceFile), Title = "Read a source file", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one working Bit.Bdnd source file verbatim - e.g. 'Demo/Client/Pages/Sortable/SortableCard.razor' (a sortable card that is both a draggable and a drop target), 'Demo/Client/Pages/Nesting/DropTargets.razor' (deep and shallow hover), 'Demo/Client/Pages/Tutorial.razor' (a chessboard with CanDropPredicate), 'Demo/Client/Pages/Other/NativeFiles.razor' (files from the desktop), 'Library/Components/BdndDropTarget.cs' (the component itself) or 'Library/Scripts/bit-bdnd.ts' (the browser layer). Omit the path for the index of files: the demo pages, each a working example, the hosting files of a Blazor Web App and the library's components. These are worked examples - for what a parameter means, GetBdndApi answers in a fraction of the characters.")]
    public string GetBdndSourceFile(
        [Description("The file's path as the index lists it, e.g. 'Demo/Client/Pages/Sortable/Simple.razor'. Omit it for the index of files.")] string? path = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return Truncate(BdndSourceCatalog.RenderSourceIndex());

        var content = BdndSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BdndSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBdndSourceFile with no path for the index of files.";
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
