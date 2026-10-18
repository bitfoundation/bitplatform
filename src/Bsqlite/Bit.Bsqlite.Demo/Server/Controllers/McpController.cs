using System.Reflection;
using System.ComponentModel;
using ModelContextProtocol.Server;
using Bit.Bsqlite.Demo.Server.Dtos;
using Bit.Bsqlite.Demo.Server.Services;
using System.ComponentModel.DataAnnotations;

namespace Bit.Bsqlite.Demo.Server.Controllers;

/// <summary>
/// The Bsqlite MCP server: the tools an AI agent calls to write browser database code with
/// Bit.Bsqlite without guessing at its API - or at where its data will end up.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the XML documentation
/// compiled into the five packages, the README and the guides, the storage providers' declared
/// capabilities, the library's own selection and SQL guards, and the demo's source files - so an agent
/// gets what the current version actually does rather than a snapshot someone wrote down. The same
/// methods are exposed as plain HTTP GET endpoints under /api/mcp/..., which makes each of them
/// inspectable from a browser - and is what the demo's own MCP page calls to show them working live.
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
/// than from the internet. Neither lab executes SQL: the storage planner opens nothing, and the SQL
/// check is the library's pattern-matching guards, run over text.
/// </para>
/// <para>
/// Only the three tools whose answer is machine-actionable - a ranked hit carrying its own follow-up
/// call, a storage plan, a per-backend verdict - set <c>UseStructuredContent</c> and publish an output
/// schema. The rest answer in Markdown: a structured answer goes over the wire twice, once as the object
/// and once as the JSON text the spec asks for on its behalf, and documentation read as Markdown is
/// smaller than either half.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public class McpController : ControllerBase
{
    // The README, the longest guide and the longest setup guide - which quotes eight demo files whole -
    // land far below the cap; it is there so a pathological answer cannot dominate a client's context window.
    private const int MaxDocumentLength = 40_000;

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBsqlite), Title = "Search everything about Bit.Bsqlite", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches everything known about Bit.Bsqlite at once - the README and every guide, every public type and member of its five packages, the storage backends, the setup guides, the two labs and the demo's source files - and returns the best matches, each with the exact follow-up call that returns its full text. Call it first whenever you do not already know which document or type holds the answer, then call the hit's tool verbatim. Example queries: 'live query', 'opfs fallback indexeddb', 'prerendering', 'migrate to another backend', 'unit test without a browser', 'attach'.")]
    public BsqliteSearchResultDto SearchBsqlite(
        [Description("What you are looking for, in the words you would use for it - a feature, a symptom, or a member name. A few words rank better than a whole sentence.")] string query,
        [Description("How many hits to return. Clamped to 1..50.")] int limit = 12)
    {
        return BsqliteSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBsqliteSetupGuide), Title = "Setup guide for one scenario", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the complete wiring for one Bit.Bsqlite scenario, with real code: 'standalone-wasm' (Blazor WebAssembly), 'wasm' (Blazor Web App on InteractiveWebAssembly with prerendering - how this demo is built, with its real files), 'auto' (InteractiveAuto), 'server' (InteractiveServer: not supported, and what to do instead), 'storage' (choosing and adding storage backends), 'move-from-besql' (keeping a Bit.Besql app's data), 'testing' (Bit.Bsqlite.Testing: the in-process transport and the conformance kit) or 'custom-provider' (writing a storage backend). Call this before writing setup code: where the services are registered, when a page may open the database, and which backend is first are the mistakes that compile.")]
    public string GetBsqliteSetupGuide(
        // Declared rather than merely described: the values then sit in the JSON schema itself, where a
        // model cannot pass another one past them.
        [AllowedValues("standalone-wasm", "wasm", "auto", "server", "storage", "move-from-besql", "testing", "custom-provider")]
        [Description("The scenario.")] string scenario)
    {
        var guide = BsqliteSetupGuide.Get(scenario);

        return guide is null
            ? $"'{scenario}' is not a known scenario. Use one of: {string.Join(", ", BsqliteSetupGuide.Scenarios)}."
            : Truncate(guide);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBsqliteDocument), Title = "Read a document or one of its sections", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one Bit.Bsqlite document as Markdown, or one section of it, with its code samples: the README ('readme'), a guide ('getting-started', 'storage', 'live-queries', 'architecture', 'limits', 'extending') or a package's own README ('Bit.Bsqlite.Testing', 'Bit.Bsqlite.Storage.Opfs', ...). Sub-sections are included, and name and heading matching ignore case and punctuation. Omit the name for the index of documents with their sections and sizes; omit the section for the whole document.")]
    public string GetBsqliteDocument(
        [Description("The document's name, e.g. 'storage', 'readme' or 'Bit.Bsqlite.Testing'. Omit it for the index of documents.")] string? name = null,
        [Description("One heading of that document, e.g. 'Selection' or 'First-party backends'; a heading a document uses twice is told apart as 'Parent > Heading', as the index shows. Omit it for the whole document.")] string? section = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return BsqliteSourceCatalog.RenderDocumentIndex();

        var resolved = BsqliteSourceCatalog.ResolveDocumentName(name);

        if (resolved is null)
        {
            return $"There is no Bit.Bsqlite document called '{name}'. The documents are: " +
                   $"{string.Join(", ", BsqliteSourceCatalog.Documents.Select(document => $"'{document.Name}'"))}.";
        }

        if (string.IsNullOrWhiteSpace(section)) return Truncate(BsqliteSourceCatalog.GetDocument(resolved)!);

        var text = BsqliteSourceCatalog.GetDocumentSection(resolved, section);

        if (text is not null) return Truncate(text);

        var sections = BsqliteSourceCatalog.GetSections(resolved);
        var candidates = sections
            .Where(s => s.Heading.Contains(section, StringComparison.OrdinalIgnoreCase) || section.Contains(s.Heading, StringComparison.OrdinalIgnoreCase))
            .Select(s => $"'{s.Key}'")
            .Distinct(StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The '{resolved}' document has no section called '{section}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The '{resolved}' document has no section called '{section}'. Its sections are: {string.Join(", ", sections.Select(s => $"'{s.Key}'"))}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBsqliteApi), Title = "Public API reference", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the public API of the Bit.Bsqlite packages as Markdown, read out of the shipped assemblies: one type with its namespace and package, every constructor, property, method, event or enum value it has - each with its C# type, its real default value and its documentation. Omit the type name for the index of every public type, grouped by package and namespace. Call it before using a member you are unsure about, e.g. 'BsqliteBuilder', 'BsqliteStorageSelectionBuilder', 'BsqliteDatabase', 'IBsqliteDbContextFactory', 'BsqliteDbContextOptions', 'BsqliteConnection', 'BsqliteLiveQuery', 'BsqliteStorageResolution', 'IBsqliteStorageProvider', 'BsqliteInProcessTransport'.")]
    public string GetBsqliteApi(
        [Description("The type's name without its namespace, e.g. 'BsqliteDatabase' or 'BsqliteStorageCapabilities'. Omit it for the index of types.")] string? typeName = null)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return BsqliteApiCatalog.RenderIndex();

        var rendered = BsqliteApiCatalog.RenderType(typeName);

        if (rendered is not null) return Truncate(rendered);

        var candidates = BsqliteApiCatalog.Types
            .Where(t => t.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"Bit.Bsqlite has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"Bit.Bsqlite has no public type called '{typeName}'. Its public types are prefixed 'Bsqlite' or 'IBsqlite' (e.g. 'BsqliteConnection' rather than 'SqliteConnection', 'IBsqliteDbContextFactory' rather than 'DbContextFactory'), except the DI entry point class 'BitBsqlite'; call GetBsqliteApi with no type name for the index.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBsqliteStorageProviders), Title = "Storage backends and their capabilities", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the four first-party storage backends - opfs, indexeddb, cache-storage, in-memory - read out of their provider types: each one's package and namespace, the engine build it needs, whether it persists, the rest of its declared capabilities with what each means, its VFS module, and which statements the library's guards refuse on it (WAL, ATTACH, PRAGMA page_size). Omit the provider for the table of all four. Use it to pick backends and their order; PlanBsqliteStorageSelection then predicts which one a database lands on.")]
    public string GetBsqliteStorageProviders(
        [Description("The backend's name, e.g. 'opfs' or 'indexeddb' (its provider type, e.g. 'BsqliteOpfsStorageProvider', also works). Omit it for the table of every backend.")] string? provider = null)
    {
        if (string.IsNullOrWhiteSpace(provider)) return BsqliteStorageCatalog.RenderIndex();

        return BsqliteStorageCatalog.Render(provider)
            ?? $"No first-party storage backend is called '{provider}'. The backends are: {string.Join(", ", BsqliteStorageCatalog.Providers.Select(p => p.Name))}. A backend of your own implements IBsqliteStorageProvider - see GetBsqliteSetupGuide(scenario: \"custom-provider\").";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(PlanBsqliteStorageSelection), Title = "Plan a storage selection", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Runs Bit.Bsqlite's own storage selection - its resolver, migrator and session factory, as AddBitBsqliteServices composes them - on a configuration and a browser you describe, and reports what the library decides: the backend the database opens on (or the BsqliteStorageUnavailableException it throws), every candidate's status with the library's own explanation, whether the sticky binding decided, the binding before and after, any automatic migration and whether it copied data, what the library logged, the equivalent C# registration, and notes on what to change. First-party backends keep the capabilities their provider types declare; nothing is opened and no SQL runs. Example: candidates 'opfs, indexeddb', unavailable 'opfs', existingBinding 'indexeddb', migrationMode 'AutomaticOnOpen'.")]
    public Task<BsqliteStoragePlanDto> PlanBsqliteStorageSelection(
        [Description("The preference order, most preferred first, comma-separated: what UseStorage(s => s.Prefer(...).Fallback(...)) lists. Default 'opfs, indexeddb'.")] string? candidates = null,
        [Description("Optional. The providers registered with AddStorageProvider, comma-separated - including ones that are not candidates. Default: every candidate.")] string? registered = null,
        [Description("Optional. The backends whose probe reports them unusable in this browser, comma-separated, e.g. 'opfs' for a browser without OPFS.")] string? unavailable = null,
        [Description("Optional. Custom (not first-party) backends that do not survive a reload, comma-separated. First-party backends use their declared persistence.")] string? nonPersistent = null,
        [Description("Whether AllowNonPersistentFallback() is called. Default false, as in the library.")] bool allowNonPersistentFallback = false,
        [AllowedValues("KeepExisting", "Manual", "AutomaticOnOpen")]
        [Description("WithMigrationMode(...): what happens when a better backend is available for an existing database. Default KeepExisting, as in the library.")] string migrationMode = "KeepExisting",
        [Description("Optional. The backend the database already lives on - its sticky binding from an earlier session. Omit it for a database opened for the first time.")] string? existingBinding = null,
        [Description("Optional. The free space every available backend's probe reports, in bytes. A migration needs room for the database (the plan's stand-in is 64 KB), so a smaller value shows a migration that fails safely.")] long? freeBytes = null,
        [Description("Optional. The name passed to AddDatabase. Default 'app.db'.")] string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        return BsqliteStorageLab.PlanAsync(candidates, registered, unavailable, nonPersistent, allowNonPersistentFallback,
            migrationMode, existingBinding, freeBytes, databaseName, cancellationToken);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(CheckBsqliteSql), Title = "Check SQL against the backends' guards", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Runs Bit.Bsqlite's own static SQL rules on a statement or a short script without executing it: whether BsqliteFeatureGuards refuses it on each first-party backend - under the limits that backend's capabilities give a session - with the exact BsqliteException message a command would throw, the tables BsqliteTableNames reads out of it (what a live query over it watches), and whether it counts as a write or as DDL. Omit the SQL for the statements the guards exist for, judged on every backend. Example: 'PRAGMA page_size = 8192', a migration's script, or what a LINQ query's ToQueryString() returns.")]
    public BsqliteSqlCheckDto CheckBsqliteSql(
        [Description("The SQL to check, up to 10,000 characters. It is pattern-matched, never executed. Omit it for the guard reference.")] string? sql = null)
    {
        return BsqliteSqlCheck.Check(sql);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBsqliteSourceFile), Title = "Read a source file", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one working Bit.Bsqlite demo file verbatim - e.g. 'Demo/Client/Extensions/IServiceCollectionExtensions.cs' (the registration, every backend), 'Demo/Client/Components/SessionLifecycle.razor' (opening the database only once interactive), 'Demo/Client/BrowserDatabase.cs' (moving a database between backends), 'Demo/Client/Pages/Queries.razor' (live queries and raw ADO.NET), 'Demo/Client/Pages/Storage.razor' (the resolution, capabilities and probes), 'Demo/Data/AppDbContext.cs' (the model both sides share) or 'Demo/Server/Program.cs'. Omit the path for the index of files. These are worked examples - for what a member means, GetBsqliteApi answers in a fraction of the characters.")]
    public string GetBsqliteSourceFile(
        [Description("The file's path as the index lists it, e.g. 'Demo/Client/Pages/Storage.razor'. Omit it for the index of files.")] string? path = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return BsqliteSourceCatalog.RenderSourceIndex();

        var content = BsqliteSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BsqliteSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBsqliteSourceFile with no path for the index of files.";
        }

        return Truncate(content);
    }

    /// <summary>
    /// Everything this server exposes over MCP - its tools, prompts and resources - read off the
    /// attributes that declare them.
    /// <para>
    /// Deliberately not an MCP tool: an agent already gets this from the protocol's own
    /// <c>tools/list</c>. It exists for the /mcp-server demo page, which documents the server to a human
    /// reader. Reflecting over the registrations rather than restating them means the page cannot
    /// describe a tool that no longer exists, or miss one that was just added.
    /// </para>
    /// </summary>
    [HttpGet]
    public BsqliteMcpCatalogDto GetMcpCatalog()
    {
        return new BsqliteMcpCatalogDto
        {
            Tools = [.. typeof(McpController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(method => (Method: method, Tool: method.GetCustomAttribute<McpServerToolAttribute>()))
                .Where(entry => entry.Tool is not null)
                .Select(entry => new BsqliteMcpMemberDto
                {
                    Name = entry.Tool!.Name ?? entry.Method.Name,
                    Title = entry.Tool.Title,
                    Description = entry.Method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
                    Parameters = [.. entry.Method.GetParameters().Where(IsArgument).Select(FormatParameter)]
                })
                .OrderBy(tool => tool.Name, StringComparer.Ordinal)],

            Prompts = [.. typeof(McpPrompts)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(method => (Method: method, Prompt: method.GetCustomAttribute<McpServerPromptAttribute>()))
                .Where(entry => entry.Prompt is not null)
                .Select(entry => new BsqliteMcpMemberDto
                {
                    Name = entry.Prompt!.Name ?? entry.Method.Name,
                    Title = entry.Prompt.Title,
                    Description = entry.Method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
                    Parameters = [.. entry.Method.GetParameters().Where(IsArgument).Select(FormatParameter)]
                })
                .OrderBy(prompt => prompt.Name, StringComparer.Ordinal)],

            Resources = [.. typeof(McpResources)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(method => (Method: method, Resource: method.GetCustomAttribute<McpServerResourceAttribute>()))
                .Where(entry => entry.Resource is not null)
                .Select(entry => new BsqliteMcpMemberDto
                {
                    Name = entry.Resource!.UriTemplate ?? entry.Resource.Name ?? string.Empty,
                    Title = entry.Resource.Title,
                    Description = entry.Method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
                    Parameters = []
                })
                .OrderBy(resource => resource.Name, StringComparer.Ordinal)]
        };
    }

    /// <summary>A parameter the caller supplies - not the cancellation token the transport supplies.</summary>
    private static bool IsArgument(ParameterInfo parameter) => parameter.ParameterType != typeof(CancellationToken);

    /// <summary>A tool or prompt parameter, as "name: type" with "?" for the optional ones.</summary>
    private static string FormatParameter(ParameterInfo parameter)
    {
        var type = BsqliteApiCatalog.FriendlyName(parameter.ParameterType);

        return parameter.HasDefaultValue ? $"{parameter.Name}?: {type.TrimEnd('?')}" : $"{parameter.Name}: {type}";
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
