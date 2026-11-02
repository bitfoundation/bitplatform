using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bit.Bortable.Demo.Server.Dtos;
using Bit.Bortable.Demo.Server.Services;

namespace Bit.Bortable.Demo.Server.Controllers;

/// <summary>
/// The Bortable MCP server: the tools an AI agent calls to write sortable-list, kanban and
/// drag-and-drop code with Bit.Bortable without guessing at its API.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the XML
/// documentation compiled into Bit.Bortable, the README, the shipped stylesheet, the component
/// itself and the demo's source files - so an agent gets what the current version actually does
/// rather than a snapshot someone wrote down. The same methods are exposed as plain HTTP GET
/// endpoints under /api/mcp/..., which makes each of them inspectable from a browser.
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
/// call, a drag report - set <c>UseStructuredContent</c> and publish an output schema. The rest
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
    // The longest real answer is the component's own code - the sorting engine, every rule a drag is
    // put through, the keyboard and the moves made by code - at under 100,000 characters; the
    // component's reference, the README's longest sections and the demo's pages land far below it. The
    // cap sits above all of them so each arrives whole, and is there so a pathological answer cannot
    // dominate a client's context window.
    private const int MaxDocumentLength = 120_000;

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBortable), Title = "Search everything about Bit.Bortable", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches everything known about Bit.Bortable at once - the reference guide, every public type and member, the setup guides, the stylesheet's custom properties and drag-state classes, and the demo's source files - and returns the best matches, each with the exact follow-up call that returns its full text. Call it first whenever you do not already know which section or type holds the answer, then call the hit's tool verbatim. Example queries: 'drag between two lists', 'clone', 'drag handle', 'lock some items', 'swap', 'multi drag', 'event order', 'nested lists'.")]
    public BortableSearchResultDto SearchBortable(
        [Description("What you are looking for, in the words you would use for it - a feature, a symptom, or a member name. A few words rank better than a whole sentence.")] string query,
        [Description("How many hits to return. Clamped to 1..50.")] int limit = 12)
    {
        return BortableSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBortableSetupGuide), Title = "Setup guide for one render mode", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the complete wiring needed to add Bit.Bortable to a Blazor app in one render mode: 'server' (InteractiveServer), 'wasm' (InteractiveWebAssembly, which is how this demo is built and comes with its real files), 'auto' (InteractiveAuto), 'standalone-wasm' or 'hybrid' (MAUI, WPF, Windows Forms). Call this before writing setup code - AddBitBortableServices() is required (in both projects of a Blazor Web App), the stylesheet link is required, and the lists must render interactively.")]
    public string GetBortableSetupGuide(
        // Declared rather than merely described: the values then sit in the JSON schema itself,
        // where a model cannot pass another one past them, and a client offers them as completions.
        [AllowedValues("server", "wasm", "auto", "standalone-wasm", "hybrid")]
        [Description("The app's Blazor render mode.")] string renderMode)
    {
        return Truncate(BortableSetupGuide.Get(renderMode)
            ?? $"'{renderMode}' is not a known render mode. Use one of: {string.Join(", ", BortableSetupGuide.RenderModes)}.");
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBortableGuideSection), Title = "Read a reference-guide section", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one section of the Bit.Bortable reference guide (the library's README) as Markdown, with its code samples - e.g. 'Getting started', 'Sorting a list', 'Shared lists', 'Cloning', 'Nested lists', 'Drag handles', 'Filtering items', 'Swap mode', 'Multi-drag', 'Events', 'Styling', 'Hosting models', 'Coming from SortableJS', 'Troubleshooting'. Sub-sections are included, and heading matching ignores case and punctuation. Omit the heading for the index of sections with their sizes.")]
    public string GetBortableGuideSection(
        [Description("The section's heading, e.g. 'Shared lists'. Case and punctuation are ignored; a heading the guide uses twice is told apart as 'Parent > Heading', as the index shows. Omit it for the index of sections.")] string? heading = null)
    {
        if (string.IsNullOrWhiteSpace(heading)) return BortableSourceCatalog.RenderGuideIndex();

        var section = BortableSourceCatalog.GetGuideSection(heading);

        if (section is not null) return Truncate(section);

        var candidates = BortableSourceCatalog.GuideSections
            .Where(s => s.Heading.Contains(heading, StringComparison.OrdinalIgnoreCase) || heading.Contains(s.Heading, StringComparison.OrdinalIgnoreCase))
            .Select(s => $"'{s.Key}'")
            .Distinct(StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The guide has no section called '{heading}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The guide has no section called '{heading}'. Call GetBortableGuideSection with no heading for the index of sections, or SearchBortable to find the topic.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBortableApi), Title = "Public API reference", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the public API of Bit.Bortable as Markdown, read out of the shipped assembly: one type with every Blazor parameter, property, method or enum value it has - each with its C# type, its real default value and its documentation. Omit the type name for the index of every public type. Call it before using a member you are unsure about: 'Bortable' (the component, Bortable<TItem>), 'BortableEventArgs', 'BortablePull', 'BortableDirection' or 'BitBortable' (AddBitBortableServices).")]
    public string GetBortableApi(
        [Description("The type's name without its namespace, e.g. 'Bortable' or 'BortablePull'; a generic one may be written with or without its type argument ('Bortable<TItem>'). Omit it for the index of types.")] string? typeName = null)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return BortableApiCatalog.RenderIndex();

        var rendered = BortableApiCatalog.RenderType(typeName);

        if (rendered is not null) return Truncate(rendered);

        var candidates = BortableApiCatalog.Types
            .Where(t => t.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"Bit.Bortable has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"Bit.Bortable has no public type called '{typeName}'. Every public type is prefixed 'Bortable' - the component is 'Bortable<TItem>', not 'Sortable' - except the registration, 'BitBortable'; call GetBortableApi with no type name for the index.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBortableTheme), Title = "Theme custom properties and drag-state classes", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the CSS custom properties Bit.Bortable's drag states are painted with, read out of the shipped stylesheet - the inputs declared on :root (the accent and the opacities) and the optional overrides the state rules read with a fallback (the chosen, swap and selection colours) - each with its default and what it paints, plus the drag-state classes (bit-bortable-ghost, -chosen, -hidden, -swap-highlight, -selected, -filtered...) with the parameter that replaces each. Pass one property or class for it in full: every rule that paints with it, the parameters involved, and the override to write.")]
    public string GetBortableTheme(
        [Description("A custom property, e.g. '--bit-bortable-accent-rgb' (or just 'accent-rgb'), or a class, e.g. 'bit-bortable-ghost' (or just 'ghost'). Omit it for the tables of every property and class.")] string? name = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return Truncate(BortableThemeCatalog.RenderIndex());

        var rendered = BortableThemeCatalog.Render(name);

        if (rendered is not null) return Truncate(rendered);

        var word = name.Trim().TrimStart('-', '.').Replace("bit-bortable-", string.Empty, StringComparison.OrdinalIgnoreCase);

        var candidates = BortableThemeCatalog.Properties.Select(p => p.Name)
            .Concat(BortableStylesheetClasses.Classes.Select(c => c.Name))
            .Where(candidate => word.Length > 0 && candidate.Contains(word, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The stylesheet has no custom property or class called '{name}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The stylesheet has no custom property or class called '{name}'. Every property is named --bit-bortable-* and every class bit-bortable-*; call GetBortableTheme with no name for the tables of them.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBortableDrag), Title = "Run a drag through a real Bortable", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Renders a real Bortable<string> for each list you describe and performs one drag through the component's own handlers - a reorder within a list, a move or clone across lists, a swap or a multi-drag. Reports each step (click, dragstart, dragover, drop, dragend) with whether it was accepted and why not (Group, Pull, Put, Sort, Disabled, Filter, read-only, duplicate item), the drag-state classes it left on the markup, the lists before and after, ItemsChanged, and every event - OnStart, OnChange, OnUpdate, OnRemove, OnAdd, OnSort, OnEnd - in the order raised with all its arguments, plus the Razor that declares the lists. The browser's hit testing is answered with the position you ask for. Call it with no json to run a sample.")]
    public Task<BortableDragReportDto> InspectBortableDrag(
        [Description("The lists and the drag, as JSON (up to 64 KB): {\"lists\": [{\"id\": \"todo\", \"group\": \"board\", \"items\": [\"A\", \"B\"]}, {\"id\": \"done\", \"group\": \"board\", \"items\": []}], \"drag\": {\"from\": \"todo\", \"item\": \"A\", \"to\": \"done\", \"index\": 0}}. A list also takes pull ('move', 'none', 'clone'), put, sort, disabled, swap, multiDrag, readOnly (an array), filter (the items the Filter locks) and cloneSuffix (Clone = item + suffix). The drag also takes selected (multi-drag items clicked first), swapWith (the item of a swap list to trade places with) and release ('drop', 'outside' or 'cancel'). index is where the item lands in 'to'; 'to' defaults to 'from'. Up to 8 lists of 50 unique items. Omit json to run a sample.")] string? json = null)
    {
        return BortableDragLab.InspectAsync(json);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBortableSourceFile), Title = "Read a source file", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one working Bit.Bortable source file verbatim - e.g. 'Demo/Client/Samples/GroupedLists.razor' (two lists of a group), 'Demo/Client/Samples/CloneSource.razor' (a clone factory), 'Demo/Client/Samples/SwapAcross.razor', 'Demo/Client/Samples/MultiSelectList.razor', 'Demo/Client/Samples/NestedList.razor' (a recursive tree), 'Library/Components/Bortable.razor.cs' (the sorting engine) or 'Library/Styles/bit-bortable.css' (the shipped stylesheet); a demo page's route, such as '/swap', gives the page, which names the samples it renders. Omit the path for the index of files: the demo pages and their samples, each sample a working list, the library's component and stylesheet, and the hosting files of a Blazor Web App. These are worked examples - for what a parameter means, GetBortableApi answers in a fraction of the characters.")]
    public string GetBortableSourceFile(
        [Description("The file's path as the index lists it, e.g. 'Demo/Client/Samples/GroupedLists.razor', or a demo page's route, e.g. '/shared'. Omit it for the index of files.")] string? path = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return Truncate(BortableSourceCatalog.RenderSourceIndex());

        var content = BortableSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BortableSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBortableSourceFile with no path for the index of files.";
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
