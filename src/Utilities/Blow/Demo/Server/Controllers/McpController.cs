using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bit.Blow.Demo.Server.Dtos;
using Bit.Blow.Demo.Server.Services;

namespace Bit.Blow.Demo.Server.Controllers;

/// <summary>
/// The Blow MCP server: the tools an AI agent calls to write node-editor and diagram code with
/// Bit.Blow without guessing at its API.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the XML
/// documentation compiled into Bit.Blow, the README, the shipped stylesheet, the canvas itself and
/// the demo's source files - so an agent gets what the current version actually does rather than a
/// snapshot someone wrote down. The same methods are exposed as plain HTTP GET endpoints under
/// /api/mcp/..., which makes each of them inspectable from a browser.
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
/// call, a flow report - set <c>UseStructuredContent</c> and publish an output schema. The rest
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
    // The longest real answer is the canvas's own reference - some hundred and fifty parameters and
    // sixty methods, each with its documentation - at about 40,000 characters; the README's longest
    // sections and the demo's longest pages land far below it. The cap sits above all of them so each
    // arrives whole, and is there so a pathological answer cannot dominate a client's context window.
    private const int MaxDocumentLength = 60_000;

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBlow), Title = "Search everything about Bit.Blow", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches everything known about Bit.Blow at once - the reference guide, every public type and member, the setup guides, the theme's custom properties and the stylesheet's classes, and the demo's source files and examples gallery - and returns the best matches, each with the exact follow-up call that returns its full text. Call it first whenever you do not already know which section or type holds the answer, then call the hit's tool verbatim. Example queries: 'custom node handles', 'sub-flows', 'drag and drop from a sidebar', 'undo redo', 'save and restore', 'auto layout', 'dark mode'.")]
    public BlowSearchResultDto SearchBlow(
        [Description("What you are looking for, in the words you would use for it - a feature, a symptom, or a member name. A few words rank better than a whole sentence.")] string query,
        [Description("How many hits to return. Clamped to 1..50.")] int limit = 12)
    {
        return BlowSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlowSetupGuide), Title = "Setup guide for one render mode", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the complete wiring needed to add Bit.Blow to a Blazor app in one render mode: 'server' (InteractiveServer), 'wasm' (InteractiveWebAssembly, which is how this demo is built and comes with its real files), 'auto' (InteractiveAuto), 'standalone-wasm' or 'hybrid' (MAUI, WPF, Windows Forms). Call this before writing setup code - there are no services to register, but the stylesheet link is required, the canvas needs a container with a height, and which project the pages belong in differs per render mode.")]
    public string GetBlowSetupGuide(
        // Declared rather than merely described: the values then sit in the JSON schema itself,
        // where a model cannot pass another one past them, and a client offers them as completions.
        [AllowedValues("server", "wasm", "auto", "standalone-wasm", "hybrid")]
        [Description("The app's Blazor render mode.")] string renderMode)
    {
        return Truncate(BlowSetupGuide.Get(renderMode)
            ?? $"'{renderMode}' is not a known render mode. Use one of: {string.Join(", ", BlowSetupGuide.RenderModes)}.");
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlowGuideSection), Title = "Read a reference-guide section", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one section of the Bit.Blow reference guide (the library's README) as Markdown, with its code samples - e.g. 'Getting started', 'Custom nodes', 'Handles and connections', 'Sub-flows and grouping', 'Edges', 'Interaction', 'The viewport', 'Auto-layout', 'Saving and restoring', 'Undo and redo', 'Theming', 'Hosting models', 'Coming from React Flow'. Sub-sections are included, and heading matching ignores case and punctuation. Omit the heading for the index of sections with their sizes.")]
    public string GetBlowGuideSection(
        [Description("The section's heading, e.g. 'Custom nodes'. Case and punctuation are ignored; a heading the guide uses twice is told apart as 'Parent > Heading', as the index shows. Omit it for the index of sections.")] string? heading = null)
    {
        if (string.IsNullOrWhiteSpace(heading)) return BlowSourceCatalog.RenderGuideIndex();

        var section = BlowSourceCatalog.GetGuideSection(heading);

        if (section is not null) return Truncate(section);

        var candidates = BlowSourceCatalog.GuideSections
            .Where(s => s.Heading.Contains(heading, StringComparison.OrdinalIgnoreCase) || heading.Contains(s.Heading, StringComparison.OrdinalIgnoreCase))
            .Select(s => $"'{s.Key}'")
            .Distinct(StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The guide has no section called '{heading}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The guide has no section called '{heading}'. Call GetBlowGuideSection with no heading for the index of sections, or SearchBlow to find the topic.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlowApi), Title = "Public API reference", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the public API of Bit.Blow as Markdown, read out of the shipped assembly: one type with every Blazor parameter, property, method, event or enum value it has - each with its C# type, its real default value and its documentation. Omit the type name for the index of every public type. Call it before using a member you are unsure about, e.g. 'BlowCanvas', 'BlowNode', 'BlowEdge', 'BlowHandle', 'BlowNodeResizer', 'BlowMiniMap', 'BlowLayout', 'BlowLayeredLayoutOptions', 'BlowGraphUtils', 'BlowHistory', 'BlowClipboard'.")]
    public string GetBlowApi(
        [Description("The type's name without its namespace, e.g. 'BlowCanvas' or 'BlowEdgePathOptions'. Omit it for the index of types.")] string? typeName = null)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return BlowApiCatalog.RenderIndex();

        var rendered = BlowApiCatalog.RenderType(typeName);

        if (rendered is not null) return Truncate(rendered);

        var candidates = BlowApiCatalog.Types
            .Where(t => t.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"Bit.Blow has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"Bit.Blow has no public type called '{typeName}'. Every public type is prefixed 'Blow' (e.g. 'BlowCanvas' rather than 'ReactFlow', 'BlowNode' rather than 'Node'); call GetBlowApi with no type name for the index.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlowTheme), Title = "Theme custom properties", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the CSS custom properties Bit.Blow's look is built from, read out of the shipped stylesheet: every --bit-blow-* property with its light value, its dark value and the --bit-blow-dark-* property dark mode copies it from, and what it paints. Pass one property for it in full - every rule that paints with it, the component parameters that default to it, and the override to write. Restyle a flow by overriding these on .bit-blow rather than by overriding the library's selectors; restyle dark mode through the --bit-blow-dark-* ones.")]
    public string GetBlowTheme(
        [Description("The custom property, e.g. '--bit-blow-node-selected' (or just 'node-selected'). Omit it for the table of every property.")] string? property = null)
    {
        if (string.IsNullOrWhiteSpace(property)) return Truncate(BlowThemeCatalog.RenderIndex());

        var rendered = BlowThemeCatalog.Render(property);

        if (rendered is not null) return Truncate(rendered);

        var word = property.Trim().TrimStart('-').Replace("bit-blow-", string.Empty, StringComparison.OrdinalIgnoreCase);

        var candidates = BlowThemeCatalog.Properties
            .Where(p => word.Length > 0 && p.Name.Contains(word, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Name)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The stylesheet declares no custom property called '{property}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The stylesheet declares no custom property called '{property}'. Every one is named --bit-blow-*; call GetBlowTheme with no property for the table of them.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBlowFlow), Title = "Inspect a Blow flow", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Renders a flow - its nodes and edges as JSON, in the shape System.Text.Json gives BlowCanvas.ToObject() (a BlowSnapshot) - through the real BlowCanvas and reports what the library makes of it: whether it deserializes, what OnError raises (duplicate node, edge and handle ids, edges to missing nodes, missing parents), node and edge types, bounds from BlowGraphUtils.GetNodesBounds, the sub-flow structure, and what the canvas does not report itself (parent cycles, edges naming a handle the node lacks) - with notes on what to fix. Optionally runs BlowLayout and returns every node's new position. Call it with no json for a sample flow, the quickest way to see the format.")]
    public Task<BlowFlowReportDto> InspectBlowFlow(
        [Description("The flow's JSON: an object with 'nodes' and 'edges' arrays and optionally a 'viewport' (up to 2 MB). camelCase or PascalCase names; enums as names or numbers. A node's 'measured' size and 'handles', as JsonSerializer writes them, are honoured. Omit it to get a sample flow instead.")] string? json = null,
        [AllowedValues("none", "layered", "layered-left-to-right", "layered-bottom-to-top", "layered-right-to-left", "grid", "circle")]
        [Description("A BlowLayout to run and report on: 'layered' (top to bottom, or a direction), 'grid' or 'circle'. 'none' (the default) runs none.")] string layout = "none")
    {
        return BlowFlowLab.InspectAsync(json, layout);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlowSourceFile), Title = "Read a source file", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one working Bit.Blow source file verbatim - e.g. 'Demo/Client/Pages/CustomNodes.razor' (a custom node template with several handles), 'Demo/Client/Pages/Advanced.razor' (groups and resizing), 'Demo/Client/Pages/Examples/SaveRestoreExample.razor', 'Demo/Client/Pages/Examples/DragNDropExample.razor', 'Demo/Client/Examples/ExampleCatalog.cs' (the examples gallery), or 'Library/Styles/bit-blow.css' (the shipped stylesheet). Omit the path for the index of files: the demo pages and the examples gallery, each a working flow, and the hosting files of a Blazor Web App. These are worked examples - for what a parameter means, GetBlowApi answers in a fraction of the characters.")]
    public string GetBlowSourceFile(
        [Description("The file's path as the index lists it, e.g. 'Demo/Client/Pages/CustomNodes.razor'. Omit it for the index of files.")] string? path = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return Truncate(BlowSourceCatalog.RenderSourceIndex());

        var content = BlowSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BlowSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBlowSourceFile with no path for the index of files.";
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
