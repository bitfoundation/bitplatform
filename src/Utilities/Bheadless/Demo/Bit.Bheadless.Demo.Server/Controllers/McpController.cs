using System.ComponentModel;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;
using Bit.Bheadless.Demo.Server.Services;

namespace Bit.Bheadless.Demo.Server.Controllers;

/// <summary>
/// The bit Bheadless MCP server: the tools an AI agent calls to build UI with this library
/// without guessing at it.
/// <para>
/// Every tool answers from this process: the compiled library and its XML documentation (which parts
/// exist, what they take and what they default to), the demo site's catalog and pages (which parts form
/// a family and how they are composed in working code), and the library's README (the guide). Nothing is
/// written down for the server, so it describes the version that ships rather than a snapshot of one.
/// The same methods are plain HTTP GET endpoints under /api/mcp/..., which makes each inspectable from a
/// browser.
/// </para>
/// <para>
/// There are four, and the count is the design: a tool's description is paid for in every request of
/// every session. A listing is not a tool but what a retrieval tool answers when asked for nothing in
/// particular, and a lookup of one name is not a tool when the one taking several resolves each. The
/// answers are Markdown with no output schema - a parameter table names its columns once, where JSON
/// would repeat them per row and a structured result would cross the wire twice.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public partial class McpController : ControllerBase
{
    [HttpGet]
    [McpServerTool(Name = nameof(SearchBheadless), Title = "Search everything about bit Bheadless",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Searches the component families, every part and parameter, the library's other types, every demo example and the guide at once, and returns the best matches, each with the exact follow-up call that returns its full text. Ask by what the UI has to do ('dropdown menu with submenus', 'pick several options', 'close on escape') or by a name or hook ('BitBhListbox', 'data-highlighted').")]
    public string SearchBheadless(
        [Description("What the UI has to do, in your own words, or a name.")] string query,
        [Description("How many matches to return.")] int limit = 10)
        => BheadlessSearchIndex.Search(query, limit);

    [HttpGet]
    [McpServerTool(Name = nameof(GetBheadlessComponent), Title = "Parts, parameters and hooks of a component family",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets the full reference of a bit Bheadless component family - every part (BitBhMenu, BitBhMenuButton, BitBhMenuItems, BitBhMenuItem ...) with its parameters, their types and defaults, which are two-way bindable, the element it renders, the data-*/aria-* styling hooks it emits, the types its parameters take, and its demo examples with the first one's code. Accepts a family ('Menu', 'radiogroup'), a part ('BitBhMenuItem') or another library type ('BitBhAnchorOptions'); several, separated by commas. Omit names for the catalog of every family, grouped by area.")]
    public string GetBheadlessComponent(
        [Description("Optional. Families, parts or types separated by commas, e.g. 'Menu', 'BitBhTab, Dialog'. Omitted, the catalog is returned.")] string? names = null)
    {
        if (string.IsNullOrWhiteSpace(names)) return BheadlessMarkdown.Catalog();

        var families = new List<(ComponentFamily Family, ComponentPart? Part)>();
        var types = new List<Type>();
        var misses = new List<string>();

        foreach (var name in SplitNames(names))
        {
            if (BheadlessComponentCatalog.Find(name) is { } found) families.Add(found);
            else if (BheadlessComponentCatalog.FindType(name) is { } type) types.Add(type);
            else misses.Add(name);
        }

        var answers = new List<string>();

        if (families.Count > 0) answers.Add(BheadlessMarkdown.Families(families));

        answers.AddRange(types.Distinct().Select(BheadlessMarkdown.Type));

        // A miss is answered with the neighbours, never with a failed call: the name an agent guessed
        // is usually an edit or two from the one this library chose.
        answers.AddRange(misses.Select(Miss));

        return BheadlessMarkdown.Bound(string.Join("\n\n---\n\n", answers), "Ask for fewer names at a time.");
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBheadlessExamples), Title = "Working code of a component family",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets the demo site's examples of one component family as the code it runs - the Razor of each example with its CSS, or the Razor with Tailwind classes - each with the paragraph that explains it. This is where the composition, the Template contexts and the styling hooks show up working together. Narrow it with 'example'; the titles come back from GetBheadlessComponent.")]
    public string GetBheadlessExamples(
        [Description("The family or one of its parts, e.g. 'Combobox', 'BitBhTab'.")] string name,
        [Description("Optional. An example's title or part of one, e.g. 'Multiple'. Omitted, every example is returned, as many as fit.")] string? example = null,
        [Description("Optional. 'css' (hand-written CSS, the default) or 'tailwind'.")] string? styling = null)
    {
        if (BheadlessComponentCatalog.Find(name) is not { } found) return Miss(name);

        return BheadlessMarkdown.Examples(found.Family, example, styling);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBheadlessGuide), Title = "One section of the bit Bheadless guide",
                   ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Gets a section of the library's guide: installation and service registration, compound components and controlled / uncontrolled state, Template, the common parameters, positioning (Anchor), overlays, dismissal and focus, announcements, right-to-left, styling hooks (plain CSS, Tailwind, Bootstrap) and accessibility testing. Omit the topic for the list of sections and the installation section.")]
    public string GetBheadlessGuide(
        [Description("Optional. A heading or what it is about, e.g. 'Installation', 'Positioning', 'rtl', 'testing'.")] string? topic = null)
        => BheadlessMarkdown.Bound(BheadlessGuide.Get(topic), "Ask for a sub-section by its heading.");

    private static string Miss(string name)
    {
        var similar = BheadlessComponentCatalog.Similar(name);

        return similar.Length > 0
            ? $"bit Bheadless has no component family, part or type called '{name}'. Did you mean: {string.Join(", ", similar)}?"
            : $"bit Bheadless has no component family, part or type called '{name}'. Call GetBheadlessComponent with no names for the catalog, or SearchBheadless to find it by what it does.";
    }

    /// <summary>
    /// The names in one argument. Commas separate them; a piece that resolves to nothing is tried again
    /// word by word, so 'BitBhTab BitBhTabList' works too, while 'Alert Dialog' still names one family.
    /// </summary>
    private static IEnumerable<string> SplitNames(string names)
    {
        foreach (var piece in names.Split([',', ';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (BheadlessComponentCatalog.Find(piece) is not null || BheadlessComponentCatalog.FindType(piece) is not null)
            {
                yield return piece;
                continue;
            }

            var words = WhitespaceRegex().Split(piece).Where(word => word.Length > 0).ToArray();

            if (words.Length > 1 && words.All(word => BheadlessComponentCatalog.Find(word) is not null || BheadlessComponentCatalog.FindType(word) is not null))
            {
                foreach (var word in words) yield return word;
            }
            else
            {
                yield return piece;
            }
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
