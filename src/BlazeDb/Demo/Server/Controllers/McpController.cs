using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bit.BlazeDb.Demo.Server.Dtos;
using Bit.BlazeDb.Demo.Server.Services;

namespace Bit.BlazeDb.Demo.Server.Controllers;

/// <summary>
/// The BlazeDb MCP server: the tools an AI agent calls to write database code with Bit.BlazeDb without
/// guessing at its API.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the XML documentation
/// compiled into the Bit.BlazeDb assemblies, the README, the source generator itself and the demo's
/// source files - so an agent gets what the current version actually does rather than a snapshot
/// someone wrote down. The same methods are exposed as plain HTTP GET endpoints under /api/mcp/...,
/// which makes each of them inspectable from a browser.
/// </para>
/// <para>
/// One rule holds across the reference tools: a tool takes the key of the thing you want and answers
/// with that thing; leave the key out and it answers with the index of what there is. That is what a
/// listing tool used to be - and a listing tool costs a name, a description and a schema in every
/// request an agent makes, forever, to say what an optional argument says for nothing.
/// </para>
/// <para>
/// Each tool carries the two annotations that mean something for a tool which only reads:
/// <c>ReadOnly</c>, which is what lets a client run it without stopping to ask a person, and
/// <c>OpenWorld = false</c>, which says the answers come from a closed, known body of material rather
/// than from the internet. Only the two tools whose answer is machine-actionable - a ranked hit carrying
/// its own follow-up call, the generator's verdict on a table type - set <c>UseStructuredContent</c> and
/// publish an output schema; the rest answer in Markdown, which costs a model a fraction of the same
/// reference sent as JSON.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public class McpController : ControllerBase
{
    // The README's longest sections and the demo's longest pages land below the cap; it is there so a
    // pathological answer cannot dominate a client's context window.
    private const int MaxDocumentLength = 40_000;

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBlazeDb), Title = "Search everything about Bit.BlazeDb", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches everything known about Bit.BlazeDb at once - the reference guide, every public type and member of the engine, the browser backends and the EF Core provider, the source generator's diagnostics, the setup guides and the demo's source files - and returns the best matches, each with the exact follow-up call that returns its full text. Call it first whenever you do not already know which section or type holds the answer, then call the hit's tool verbatim. Example queries: 'persist to opfs', 'unique index', 'transaction rollback', 'second tab locked', 'linq dbcontext', 'encrypt'.")]
    public BlazeDbSearchResultDto SearchBlazeDb(
        [Description("What you are looking for, in the words you would use for it - a feature, a symptom, or a member name. A few words rank better than a whole sentence.")] string query,
        [Description("How many hits to return. Clamped to 1..50.")] int limit = 12)
    {
        return BlazeDbSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlazeDbSetupGuide), Title = "Setup guide for one hosting model", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the complete wiring needed to add Bit.BlazeDb to an app for one hosting model: 'wasm' (Blazor Web App, InteractiveWebAssembly - how this demo is built, with its real files), 'standalone-wasm', 'auto' (InteractiveAuto), 'server' (Blazor Server or ASP.NET Core), 'hybrid' (MAUI, WPF, Windows Forms) or 'desktop' (console apps, tools and tests). Call this before writing setup code - the browser backends exist only on WebAssembly and must not be opened while prerendering, and the engine assumes one writer with no concurrent readers, so where the database lives and how it is shared differ per hosting model.")]
    public string GetBlazeDbSetupGuide(
        // Declared rather than merely described: the values then sit in the JSON schema itself, where a
        // model cannot pass another one past them, and a client offers them as completions.
        [AllowedValues("wasm", "standalone-wasm", "auto", "server", "hybrid", "desktop")]
        [Description("The app's hosting model, e.g. 'wasm' for a Blazor Web App rendering InteractiveWebAssembly.")] string hostingModel)
    {
        return BlazeDbSetupGuide.Get(hostingModel)
            ?? $"'{hostingModel}' is not a known hosting model. Use one of: {string.Join(", ", BlazeDbSetupGuide.HostingModels)}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlazeDbGuideSection), Title = "Read a reference-guide section", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one section of the Bit.BlazeDb reference guide (the library's README) as Markdown, with its code samples - e.g. 'Quick start', 'Rows are live objects', 'Threading model', 'Hosting models', 'Encryption at rest', 'Storage quota', 'Multiple tabs', 'EF Core', 'How LINQ is translated', 'Schema evolution', 'Compile-time diagnostics'. Sub-sections are included, and heading matching ignores case and punctuation. Omit the heading for the index of sections with their sizes.")]
    public string GetBlazeDbGuideSection(
        [Description("The section's heading, e.g. 'Multiple tabs'. Case and punctuation are ignored; a heading the guide uses twice is told apart as 'Parent > Heading', as the index shows. Omit it for the index of sections.")] string? heading = null)
    {
        if (string.IsNullOrWhiteSpace(heading)) return BlazeDbSourceCatalog.RenderGuideIndex();

        var section = BlazeDbSourceCatalog.GetGuideSection(heading);

        if (section is not null) return Truncate(section);

        var candidates = BlazeDbSourceCatalog.GuideSections
            .Where(s => s.Heading.Contains(heading, StringComparison.OrdinalIgnoreCase) || heading.Contains(s.Heading, StringComparison.OrdinalIgnoreCase))
            .Select(s => $"'{s.Key}'")
            .Distinct(StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The guide has no section called '{heading}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The guide has no section called '{heading}'. Call GetBlazeDbGuideSection with no heading for the index of sections, or SearchBlazeDb to find the topic.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlazeDbApi), Title = "Public API reference", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the public API of Bit.BlazeDb as Markdown, read out of the shipped assemblies (Bit.BlazeDb, Bit.BlazeDb.Browser, Bit.BlazeDb.EntityFrameworkCore): one type with every property, method, event or enum value it has - each with its C# type, its real default value and its documentation. Omit the type name for the index of every public type. Call it before using a member you are unsure about, e.g. 'BlazeDbDatabase', 'BlazeDbDatabaseOptions', 'BlazeDbTable', 'BlazeDbQuery', 'BlazeDbOpfsStorage', 'BlazeDbIndexedDbStorage', 'BlazeDbEncryptedStorage', 'BlazeDbTabSync', 'BlazeDbIndexAttribute', 'BlazeDbDbContextOptionsExtensions'.")]
    public string GetBlazeDbApi(
        [Description("The type's name without its namespace, e.g. 'BlazeDbTable' or 'BlazeDbDatabaseOptions'. Omit it for the index of types.")] string? typeName = null)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return BlazeDbApiCatalog.RenderIndex();

        var rendered = BlazeDbApiCatalog.RenderType(typeName);

        if (rendered is not null) return Truncate(rendered);

        var candidates = BlazeDbApiCatalog.Types
            .Where(t => t.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"Bit.BlazeDb has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"Bit.BlazeDb has no public type called '{typeName}'. Every public type is prefixed 'BlazeDb' (e.g. 'BlazeDbDatabase' rather than 'Database', 'BlazeDbOpfsStorage' rather than 'OpfsStorage'); call GetBlazeDbApi with no type name for the index. The members generated for a [BlazeDbTable] type ('Table', 'Indexes') are reported by InspectBlazeDbTable.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlazeDbDiagnostics), Title = "Explain a generator diagnostic", ReadOnly = true, OpenWorld = false)]
    [Description("Explains one BLZ diagnostic of the Bit.BlazeDb source generator - its severity, the message a build reports, why the generator rejects the shape, a rejected table type and the fixed one. Omit the code for the table of every diagnostic. Call it whenever a build or InspectBlazeDbTable reports a BLZ id.")]
    public string GetBlazeDbDiagnostics(
        [Description("The diagnostic id, e.g. 'BLZ001' ('blz1' and '1' work too). Omit it for the table of every diagnostic.")] string? code = null)
    {
        if (string.IsNullOrWhiteSpace(code)) return BlazeDbDiagnosticsReference.RenderIndex();

        var entry = BlazeDbDiagnosticsReference.Find(code);

        return entry is not null
            ? BlazeDbDiagnosticsReference.Render(entry)
            : $"The BlazeDb generator has no diagnostic '{code}'. It reports {string.Join(", ", BlazeDbDiagnosticsReference.Entries.Select(e => e.Id))}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBlazeDbTable), Title = "Inspect a table type with the real generator", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Runs the real Bit.BlazeDb source generator over C# source containing one or more [BlazeDbTable] types and reports its verdict: every BLZ diagnostic a build would report, the C# compiler errors in the result, and for each table its stored name, its key, the field number and wire type of every stored property (and whether the number was pinned with [BlazeDbField]), its hash, ordered and compound indexes, and the members the generator adds ('Table', 'Indexes.X'). Check every table type with it before you build on it, and use the generated names it lists rather than guessed ones. The usual usings (System, System.Collections.Generic, Bit.BlazeDb, ...) are implied, so a class on its own is enough. The source is compiled, never run.")]
    public BlazeDbTableInspectionDto InspectBlazeDbTable(
        [Description("The table source: one or more types marked [BlazeDbTable], as they would be written in a .cs file. At most 50,000 characters.")] string source,
        [Description("True to also return the full generated source. Off by default - the table report is what a caller normally needs.")] bool includeGeneratedCode = false)
    {
        return BlazeDbTableInspector.Inspect(source, includeGeneratedCode);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBlazeDbSourceFile), Title = "Read a source file", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one working Bit.BlazeDb source file verbatim - e.g. 'Demo/Client/Data/DemoModels.cs' (the demo's [BlazeDbTable] types), 'Demo/Client/Data/DemoDbService.cs' (opening OPFS once per tab, with the in-memory fallback), 'Demo/Client/Pages/Todos.razor' (a CRUD app over indexes), 'Demo/Client/Pages/EfCore.razor' (the EF Core provider), 'Demo/Client/Pages/Transactions.razor' or 'Demo/Client/Pages/Durability.razor'. Omit the path for the index of files: the demo pages, each driving a real database, the data files and the hosting files of a Blazor Web App. These are worked examples - for what a member means, GetBlazeDbApi answers in a fraction of the characters.")]
    public string GetBlazeDbSourceFile(
        [Description("The file's path as the index lists it, e.g. 'Demo/Client/Data/DemoModels.cs'. Omit it for the index of files.")] string? path = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return BlazeDbSourceCatalog.RenderSourceIndex();

        var content = BlazeDbSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BlazeDbSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBlazeDbSourceFile with no path for the index of files.";
        }

        return Truncate(content);
    }

    internal static string Truncate(string text)
    {
        if (text.Length <= MaxDocumentLength) return text;

        // Never between the two halves of a surrogate pair: half a pair is not text, and a client that
        // re-encodes the answer turns it into a replacement character or rejects it outright.
        var cut = char.IsHighSurrogate(text[MaxDocumentLength - 1]) ? MaxDocumentLength - 1 : MaxDocumentLength;

        return $"{text[..cut]}\n\n[truncated - the full text is longer than {MaxDocumentLength} characters]";
    }
}
