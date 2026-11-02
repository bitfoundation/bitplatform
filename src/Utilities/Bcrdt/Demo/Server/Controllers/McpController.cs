using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bit.Bcrdt.Demo.Server.Dtos;
using Bit.Bcrdt.Demo.Server.Services;

namespace Bit.Bcrdt.Demo.Server.Controllers;

/// <summary>
/// The Bcrdt MCP server: the tools an AI agent calls to write collaborative features with Bit.Bcrdt
/// without guessing at its API - or at what its merge will decide.
/// <para>
/// Every tool answers from the shipped library or from this site's own content - the XML
/// documentation compiled into the four packages, the README, the library's own engine and decoders,
/// and the demo's source files - so an agent gets what the current version actually does rather than a
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
/// rather than from the internet. The two labs run the engine, but on documents of their own that
/// live for one call: nothing anybody else holds is touched.
/// </para>
/// <para>
/// Only the three tools whose answer is machine-actionable - a ranked hit carrying its own follow-up
/// call, a merge report, a decoded payload - set <c>UseStructuredContent</c> and publish an output
/// schema. The rest answer in Markdown: a structured answer goes over the wire twice, once as the
/// object and once as the JSON text the spec asks for on its behalf, and documentation read as
/// Markdown is smaller than either half.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public class McpController : ControllerBase
{
    // The README's longest sections, the demo's longest pages and the Web App setup guide land below
    // the cap; it is there so a pathological answer cannot dominate a client's context window.
    private const int MaxDocumentLength = 40_000;

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBcrdt), Title = "Search everything about Bit.Bcrdt", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Searches everything known about Bit.Bcrdt at once - the reference guide, every public type and member of its four packages, the setup guides and the demo's source files - and returns the best matches, each with the exact follow-up call that returns its full text. Call it first whenever you do not already know which section or type holds the answer, then call the hit's tool verbatim. Example queries: 'concurrent map writes', 'presence cursor', 'offline reconnect', 'signalr hub absolute url', 'undo only my edits', 'custom transport'.")]
    public BcrdtSearchResultDto SearchBcrdt(
        [Description("What you are looking for, in the words you would use for it - a feature, a symptom, or a member name. A few words rank better than a whole sentence.")] string query,
        [Description("How many hits to return. Clamped to 1..50.")] int limit = 12)
    {
        return BcrdtSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBcrdtSetupGuide), Title = "Setup guide for one hosting model", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the complete wiring for Bit.Bcrdt in one hosting model, with files that work: 'web-app' (Blazor Web App on InteractiveWebAssembly with a SignalR hub - how this demo is built, with its real files), 'standalone-wasm', 'server' (Blazor Server: a replica per circuit, presence and CircuitHandler), 'hybrid' (MAUI, WPF, Windows Forms), 'websocket' (the WebSocket transport and relay instead of SignalR) or 'console' (a non-UI peer: a BcrdtDocument and a transport, no DI). Omit it for the index of hosting models. Call this before writing setup code - which containers register the services, the absolute hub URL, AddBitBcrdtHub, UseWebSockets and when a page may open a document are the mistakes that compile.")]
    public string GetBcrdtSetupGuide(
        // Declared rather than merely described: the values then sit in the JSON schema itself,
        // where a model cannot pass another one past them, and a client offers them as completions.
        [AllowedValues("web-app", "standalone-wasm", "server", "hybrid", "websocket", "console")]
        [Description("The app's hosting model. Omit it for the index of hosting models.")] string? hostingModel = null)
    {
        var guide = BcrdtSetupGuide.Get(hostingModel);

        return guide is null
            ? $"'{hostingModel}' is not a known hosting model. Use one of: {string.Join(", ", BcrdtSetupGuide.HostingModels)}."
            : Truncate(guide);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBcrdtGuideSection), Title = "Read a reference-guide section", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one section of the Bit.Bcrdt reference guide (the library's README) as Markdown, with its code samples - e.g. 'Getting started', 'The five-minute version', 'Choosing a data type', 'Working with documents', 'Binding to inputs', 'Presence', 'Offline and reconnection', 'Persistence', 'Undo', 'Hosting models', 'Transports and relays', 'Writing a transport', 'The synchronization protocol'. Sub-sections are included, and heading matching ignores case and punctuation. Omit the heading for the index of sections with their sizes.")]
    public string GetBcrdtGuideSection(
        [Description("The section's heading, e.g. 'Choosing a data type'. Case and punctuation are ignored; a heading the guide uses twice is told apart as 'Parent > Heading', as the index shows. Omit it for the index of sections.")] string? heading = null)
    {
        if (string.IsNullOrWhiteSpace(heading)) return BcrdtSourceCatalog.RenderGuideIndex();

        var section = BcrdtSourceCatalog.GetGuideSection(heading);

        if (section is not null) return Truncate(section);

        var candidates = BcrdtSourceCatalog.GuideSections
            .Where(s => s.Heading.Contains(heading, StringComparison.OrdinalIgnoreCase) || heading.Contains(s.Heading, StringComparison.OrdinalIgnoreCase))
            .Select(s => $"'{s.Key}'")
            .Distinct(StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"The guide has no section called '{heading}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"The guide has no section called '{heading}'. Call GetBcrdtGuideSection with no heading for the index of sections, or SearchBcrdt to find the topic.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBcrdtApi), Title = "Public API reference", ReadOnly = true, OpenWorld = false)]
    [Description("Gets the public API of Bit.Bcrdt as Markdown, read out of the four shipped assemblies: one type with its constructors, Blazor parameters, properties, methods, events or enum values - each with its C# type, its real default value and its documentation, protected members of the types you derive from included. Omit the type name for the index of every public type, by package. Call it before using a member you are unsure about, e.g. 'IBcrdtCollaborationService', 'BcrdtCollaborativeDocument', 'BcrdtDocument', 'BcrdtText', 'BcrdtMap', 'BcrdtCollaborationOptions', 'BcrdtCollaborationScope', 'BcrdtComponentBase', 'BcrdtSyncProvider', 'BcrdtHub', 'BcrdtSimulatedNetwork'.")]
    public string GetBcrdtApi(
        [Description("The type's name without its namespace, e.g. 'BcrdtCollaborativeDocument' or 'BcrdtCollaborationOptions'. Omit it for the index of types.")] string? typeName = null)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return BcrdtApiCatalog.RenderIndex();

        var rendered = BcrdtApiCatalog.RenderType(typeName);

        if (rendered is not null) return Truncate(rendered);

        var candidates = BcrdtApiCatalog.Types
            .Where(t => t.Name.Contains(typeName, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .Take(10)
            .ToArray();

        return candidates.Length > 0
            ? $"Bit.Bcrdt has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
            : $"Bit.Bcrdt has no public type called '{typeName}'. Every public type is prefixed 'Bcrdt' (e.g. 'BcrdtDocument' rather than 'Doc', 'BcrdtText' rather than 'YText'); call GetBcrdtApi with no type name for the index.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBcrdtMerge), Title = "See what a merge decides", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Runs Bit.Bcrdt's own engine on concurrent edits and reports what the merge decides: two to four replicas each make their operations while apart, then every replica receives every other's updates, in every delivery order. It reports each replica's state before the merge, the state they converge to, whether they all converged, each update's size in bytes, and per contested key, text, array or set what was decided - a map key keeps one value, a text keeps both insertions. One operation per line: '<replica>: <type> <name> <action> <arguments>', values in JSON, e.g. 'A: map meta set title \"Draft\"', 'B: text notes insert 0 \"Hi\"', 'A: array todo push \"x\"', 'B: set tags add \"urgent\"'; 'base:' lines build the shared starting state, and 'root/key' names a type nested in a root map. Omit the operations for a sample that shows the format.")]
    public BcrdtMergeReportDto InspectBcrdtMerge(
        [Description("The operations, one per line (or separated by ';'): '<replica>: text <name> insert <index> \"text\" [{attributes}] | delete <index> <length> | format <index> <length> {attributes}', 'map <name> set <key> <value> | delete <key>', 'array <name> insert <index> <value>... | push <value>... | delete <index> [<length>] | move <from> <to>', 'set <name> add <value> | remove <value>'. 'base:' lines run first, on every replica. Omit it for a sample.")] string? operations = null)
    {
        return BcrdtMergeLab.Inspect(operations);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(InspectBcrdtUpdate), Title = "Decode an update or protocol message", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Decodes a Bit.Bcrdt update or sync-protocol message with the library's own decoders and reports what is in it: the message kind (SyncStep1, SyncStep2, Update, SyncStep1Reply, AwarenessUpdate, ...), the blocks each client contributed with their clock ranges, content kinds, lengths and anchors, the delete set, the state vector the update implies, presence entries, and the size of every part - or, for a malformed payload, the BcrdtFormatException message a replica would reject it with. Pass the bytes as base64 (what BcrdtDocument.Updated hands out, what a store holds, what crosses a transport), or as hex prefixed with 0x. Omit them to decode a sample update made by the real engine.")]
    public BcrdtUpdateInspectionDto InspectBcrdtUpdate(
        [Description("The update or message, as base64 (standard or URL-safe) or as hex prefixed with 0x. Omit it to decode a sample.")] string? payload = null,
        [AllowedValues("auto", "update", "message")]
        [Description("How to read the bytes: 'update' (a bare update, as a store or BcrdtDocument.Updated holds it), 'message' (a protocol message: a version byte, a kind byte and its payload) or 'auto' (the default: whichever the library accepts, a message when both do).")] string readAs = "auto")
    {
        return BcrdtUpdateInspector.Inspect(payload, readAs);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBcrdtSourceFile), Title = "Read a source file", ReadOnly = true, OpenWorld = false)]
    [Description("Gets one working Bit.Bcrdt demo file verbatim - e.g. 'Demo/Client/Pages/Live.razor' (the page an application would write: BcrdtCollaborationScope, BcrdtTextArea and BcrdtPresenceView over the app's own registration, through a SignalR hub or a WebSocket relay), 'Demo/Client/Pages/CollaborativeForm.razor' (scalar fields versus nested text), 'Demo/Client/Pages/CollaborativeList.razor' (nested types in a list), 'Demo/Client/Pages/UndoRedo.razor', 'Demo/Client/Collaboration/ConflictLabSession.cs' (provoked conflicts and their expected outcomes) or 'Demo/Server/Program.cs' (both relays mapped). The pages run several replicas side by side; the samples under 'Demo/Samples/' - e.g. 'Demo/Samples/Notes.razor', 'Demo/Samples/TicketForm.razor', 'Demo/Samples/TodoList.razor' - are the same features as an application writes them, one participant through BcrdtCollaborationScope, and the closest thing to copy. Omit the path for the index of files. These are worked examples - for what a member means, GetBcrdtApi answers in a fraction of the characters.")]
    public string GetBcrdtSourceFile(
        [Description("The file's path as the index lists it, e.g. 'Demo/Client/Pages/Live.razor'. Omit it for the index of files.")] string? path = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return BcrdtSourceCatalog.RenderSourceIndex();

        var content = BcrdtSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BcrdtSourceCatalog.SourceFiles
                .Where(f => f.Path.Contains(path, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBcrdtSourceFile with no path for the index of files.";
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
