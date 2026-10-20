using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bit.Balette.Demo.Server.Dtos;
using Bit.Balette.Demo.Server.Services;

namespace Bit.Balette.Demo.Server.Controllers;

/// <summary>
/// The Balette MCP server: the tools an AI agent calls to write command-palette code with
/// Bit.Balette without guessing at its API.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the XML
/// documentation compiled into Bit.Balette, the README, the shipped stylesheet and component markup,
/// the library's own matcher and ranker, and the demo's source files - so an agent gets what the
/// current version actually does rather than a snapshot someone wrote down. The same methods are
/// exposed as plain HTTP GET endpoints under /api/mcp/..., which makes each of them inspectable from a
/// browser.
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
/// call, a per-command search report - set <c>UseStructuredContent</c> and publish an output schema.
/// The rest answer in Markdown: a structured answer goes over the wire twice, once as the object and
/// once as the JSON text the spec asks for on its behalf, and documentation read as Markdown is
/// smaller than either half.
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
    [McpServerTool(Name = nameof(SearchBalette), Title = "Search everything about Bit.Balette", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches everything known about Bit.Balette at once - the reference guide, every public type and member, the setup guides, the stylesheet's variables, classes and data-balette hooks, and the demo's source files - and returns the best matches, each with the exact follow-up call that returns its full text. Call it first whenever you do not already know which section or type holds the answer, then call the hit's tool verbatim. Example queries: 'nested page prompt', 'toggle keeps open', 'async source debounce', 'G then S shortcut', 'dark theme accent', 'Ctrl+K does nothing'.")]
    public BaletteSearchResultDto SearchBalette(
        [Description("What you are looking for, in the words you would use for it - a feature, a symptom, or a member name. A few words rank better than a whole sentence.")] string query,
        [Description("How many hits to return. Clamped to 1..50.")] int limit = 12)
    {
        return BaletteSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBaletteSetupGuide), Title = "Setup guide for one render mode", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the complete wiring needed to add Bit.Balette to a Blazor app in one render mode: 'server' (InteractiveServer), 'wasm' (InteractiveWebAssembly, which is how this demo is built and comes with its real files), 'auto' (InteractiveAuto), 'standalone-wasm' or 'hybrid' (MAUI, WPF, Windows Forms). Call this before writing setup code - which DI containers register the services, where the stylesheet link goes (it is not injected), where the one <Balette /> goes and what Blazor Server changes differ per render mode.")]
    public string GetBaletteSetupGuide(
        // Declared rather than merely described: the values then sit in the JSON schema itself,
        // where a model cannot pass another one past them, and a client offers them as completions.
        [AllowedValues("server", "wasm", "auto", "standalone-wasm", "hybrid")]
        [Description("The app's Blazor render mode.")] string renderMode)
    {
        return BaletteSetupGuide.Get(renderMode)
            ?? $"'{renderMode}' is not a known render mode. Use one of: {string.Join(", ", BaletteSetupGuide.RenderModes)}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBaletteGuideSection), Title = "Read a reference-guide section", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one section of the Bit.Balette reference guide (the library's README) as Markdown, with its code samples - e.g. 'Getting started', 'Commands', 'Declarative markup', 'Handlers and results', 'Nested pages', 'Search and ranking', 'Modes', 'Async sources', 'Keyboard', 'Theming', 'Hosting models', 'Options', 'Troubleshooting'. Sub-sections are included, and heading matching ignores case and punctuation. Omit the heading for the index of sections with their sizes.")]
    public string GetBaletteGuideSection(
        [Description("The section's heading, e.g. 'Nested pages'. Case and punctuation are ignored; a heading the guide uses twice is told apart as 'Parent > Heading', as the index shows. Omit it for the index of sections.")] string? heading = null)
    {
        if (string.IsNullOrWhiteSpace(heading)) return BaletteSourceCatalog.RenderGuideIndex();

        var section = BaletteSourceCatalog.GetGuideSection(heading);

        if (section is not null) return Truncate(section);

        var candidates = BaletteSourceCatalog.GuideSections
            .Where(s => s.Heading.Contains(heading, StringComparison.OrdinalIgnoreCase) || heading.Contains(s.Heading, StringComparison.OrdinalIgnoreCase))
            .Select(s => $"'{s.Key}'")
            .Distinct(StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The guide has no section called '{heading}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The guide has no section called '{heading}'. Call GetBaletteGuideSection with no heading for the index of sections, or SearchBalette to find the topic.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBaletteApi), Title = "Public API reference", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the public API of Bit.Balette as Markdown, read out of the shipped assembly: one type with every Blazor parameter, property, method, event or enum value it has - each with its C# type, its real default value and its documentation. Omit the type name for the index of every public type. Call it before using a member you are unsure about, e.g. 'Balette' (the palette component), 'BaletteCommand', 'BaletteCommandBuilder', 'BaletteCmd', 'BaletteCommandResult', 'IBaletteService', 'BaletteOptions', 'BaletteCommandMode', 'IBaletteSource', 'BaletteItem'.")]
    public string GetBaletteApi(
        [Description("The type's name without its namespace, e.g. 'BaletteCommand' or 'BaletteOptions'. Omit it for the index of types.")] string? typeName = null)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return BaletteApiCatalog.RenderIndex();

        var rendered = BaletteApiCatalog.RenderType(typeName);

        if (rendered is not null) return Truncate(rendered);

        var candidates = BaletteApiCatalog.Types
            .Where(t => t.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"Bit.Balette has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"Bit.Balette has no public type called '{typeName}'. Every public type is prefixed 'Balette' (e.g. 'BaletteCommand' rather than 'Command', 'Balette' rather than 'CommandPalette'); call GetBaletteApi with no type name for the index.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBaletteThemeVariables), Title = "List the theme variables", ReadOnly = true, OpenWorld = false)]
    [Description("Lists every CSS custom property the default Bit.Balette theme is built from - layout and palette - with its light value and its dark override, read out of the shipped stylesheet, as Markdown tables; where each override reaches (:root, dialog[data-balette=\"dialog\"], the Style and Theme parameters); the values the component sets itself; and the data-balette hooks and state attributes an Unstyled palette is styled by. Restyle the palette by overriding these variables rather than the library's selectors.")]
    public string GetBaletteThemeVariables()
    {
        return BaletteThemeCatalog.Render();
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBaletteSearch), Title = "Explain how a query ranks commands", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Runs Bit.Balette's own matcher and ranker on a query over a list of commands and reports what the palette makes of it: per command whether it matched and, if not, which term failed and why (a !negated term, smart case, an operator), its score, its rank, its place in the ranking and the row the palette lists it on, the field each term matched on, and the highlighted characters - with notes on operators, field weights and the priority and frecency formula. Without commands it searches this demo's registry. Use it to see why a command does not show up, or ranks below another.")]
    public BaletteSearchInspectionDto InspectBaletteSearch(
        [Description("The query, as typed into the palette - several terms are ANDed, and 'exact, ^prefix, suffix$ and !negate are operators. An empty string shows the empty-query order.")] string? query,
        [Description("The commands to search, as a JSON array of { \"title\", \"subtitle\", \"keywords\": [], \"group\", \"description\", \"tags\": [], \"priority\" } objects (or of plain titles), or one title per line. Omit it to search this demo's own registry.")] string? commands = null,
        [AllowedValues("fuzzy", "fuzzy-ignore-case", "substring")]
        [Description("The matcher: 'fuzzy' (BaletteFuzzyMatcher.Default, smart case - the palette's default), 'fuzzy-ignore-case' or 'substring' (BaletteSubstringMatcher).")] string matcher = "fuzzy",
        [Description("The palette's GroupWhenSearching: true (the default) lists matches under their group headings, false as one flat ranked list.")] bool groupWhenSearching = true,
        [Description("How many commands to report on - the listed ones first, then those that did not match. Clamped to 1..200.")] int limit = 20)
    {
        return BaletteSearchLab.Inspect(query, commands, matcher, groupWhenSearching, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBaletteSourceFile), Title = "Read a source file", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one working Bit.Balette source file verbatim - e.g. 'Demo/Client/Services/DemoCommands.cs' (a whole app's registry: navigation, prompts, multi-select, lazy pages, toggles, destructive commands), 'Demo/Client/Pages/Commands.razor' (page-scoped commands and the service API), 'Demo/Client/Pages/PagesAndFlows.razor', 'Demo/Client/Services/FakeData.cs' (async sources), 'Demo/Client/Layout/MainLayout.razor' (one palette with declarative items and a detail pane), 'Library/Styles/bit-balette.css' (the shipped stylesheet) or 'Library/Components/Balette.razor' (the markup it styles). Omit the path for the index of files. These are worked examples - for what a parameter means, GetBaletteApi answers in a fraction of the characters.")]
    public string GetBaletteSourceFile(
        [Description("The file's path as the index lists it, e.g. 'Demo/Client/Pages/Commands.razor'. Omit it for the index of files.")] string? path = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return BaletteSourceCatalog.RenderSourceIndex();

        var content = BaletteSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BaletteSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBaletteSourceFile with no path for the index of files.";
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
