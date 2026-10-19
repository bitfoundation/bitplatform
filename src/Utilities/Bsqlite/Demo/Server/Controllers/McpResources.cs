using System.ComponentModel;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using Bit.Bsqlite.Demo.Server.Services;

namespace Bit.Bsqlite.Demo.Server.Controllers;

/// <summary>
/// The same body of knowledge the tools serve, exposed as MCP resources.
/// <para>
/// Tools are for an agent that has decided what it needs; resources are for a client that wants to
/// attach documentation to a conversation up front, or let a person browse and pin it. Both read the
/// same catalogs, so neither can go stale relative to the other - and where a tool and a resource take
/// the same argument, they resolve it through the same method, so a key that works in one cannot fail
/// in the other.
/// </para>
/// <para>
/// A client showing <c>bsqlite://docs/{name}</c> in a picker cannot fill it in, because nothing on the
/// wire says which documents exist. Two things close that gap - the documents and the setup guides are
/// also listed as concrete resources (see <c>ListDocuments</c>, wired up in Program.cs), and every
/// placeholder here is completable through <c>completion/complete</c>.
/// </para>
/// </summary>
[McpServerResourceType]
public static class McpResources
{
    [McpServerResource(UriTemplate = "bsqlite://guide", Name = "bsqlite-guide", Title = "Bit.Bsqlite README", MimeType = "text/markdown")]
    [Description("The library's README in full: what it is, the packages, installation, where it runs, what it does not claim.")]
    public static string Guide() => BsqliteSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "bsqlite://docs/{name}", Name = "bsqlite-document", Title = "Document", MimeType = "text/markdown")]
    [Description("One Bit.Bsqlite document in full - a guide or a package README - e.g. bsqlite://docs/storage or bsqlite://docs/Bit.Bsqlite.Testing.")]
    public static string Document(string name)
        => BsqliteSourceCatalog.GetDocument(name) ?? $"There is no Bit.Bsqlite document called '{name}'.";

    [McpServerResource(UriTemplate = "bsqlite://api", Name = "bsqlite-api", Title = "Bit.Bsqlite public API", MimeType = "text/markdown")]
    [Description("Every public type of the five Bit.Bsqlite packages with its kind, namespace and summary.")]
    public static string ApiList() => BsqliteApiCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "bsqlite://api/{typeName}", Name = "bsqlite-api-type", Title = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.Bsqlite type, e.g. bsqlite://api/BsqliteDatabase.")]
    public static string ApiType(string typeName)
        => BsqliteApiCatalog.RenderType(typeName) ?? $"Bit.Bsqlite has no public type called '{typeName}'.";

    [McpServerResource(UriTemplate = "bsqlite://storage", Name = "bsqlite-storage", Title = "Storage backends", MimeType = "text/markdown")]
    [Description("The four first-party storage backends with the capabilities their provider types declare.")]
    public static string Storage() => BsqliteStorageCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "bsqlite://storage/{provider}", Name = "bsqlite-storage-backend", Title = "Storage backend", MimeType = "text/markdown")]
    [Description("One storage backend in full, with what each capability means and what the guards refuse on it, e.g. bsqlite://storage/opfs.")]
    public static string StorageBackend(string provider)
        => BsqliteStorageCatalog.Render(provider) ?? $"No first-party storage backend is called '{provider}'.";

    [McpServerResource(UriTemplate = "bsqlite://setup/{scenario}", Name = "bsqlite-setup", Title = "Setup guide", MimeType = "text/markdown")]
    [Description("The complete wiring for one scenario, e.g. bsqlite://setup/wasm or bsqlite://setup/move-from-besql.")]
    public static string Setup(string scenario)
        => BsqliteSetupGuide.Get(scenario) ?? $"'{scenario}' is not a known scenario.";

    [McpServerResource(UriTemplate = "bsqlite://source/{path}", Name = "bsqlite-source", Title = "Source file", MimeType = "text/plain")]
    [Description("One source file of the demo, e.g. bsqlite://source/Demo%2FClient%2FComponents%2FSessionLifecycle.razor.")]
    public static string Source(string path)
        => BsqliteSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.";

    /// <summary>
    /// The documents and the setup guides as resources a client can list, show and pin, rather than as
    /// placeholders someone has to already know how to fill in.
    /// <para>
    /// These sit alongside the attributed resources above rather than replacing the templates: the
    /// template is what actually serves a read - the exact URI is looked for first and the templates are
    /// matched afterwards - while these give <c>resources/list</c> something a person can choose from.
    /// The API types and the source files stay templated, because listing every one of them would bury
    /// the handful anyone browses to.
    /// </para>
    /// </summary>
    public static IEnumerable<Resource> ListDocuments()
    {
        var documents = BsqliteSourceCatalog.Documents.Select(document => new Resource
        {
            Uri = $"bsqlite://docs/{Uri.EscapeDataString(document.Name)}",
            Name = $"bsqlite-docs-{Slug(document.Name)}",
            Title = $"Docs: {document.Title}",
            Description = document.Summary is null
                ? $"The '{document.Name}' document ({document.Lines} lines)."
                : $"{document.Summary} ({document.Lines} lines)",
            MimeType = "text/markdown"
        });

        var guides = BsqliteSetupGuide.Scenarios.Select(scenario => new Resource
        {
            Uri = $"bsqlite://setup/{scenario}",
            Name = $"bsqlite-setup-{scenario}",
            Title = $"Setup: {BsqliteSetupGuide.Title(scenario)}",
            Description = $"The complete wiring for {BsqliteSetupGuide.Title(scenario)}.",
            MimeType = "text/markdown"
        });

        return documents.Concat(guides);
    }

    private static string Slug(string name)
        => string.Join('-', name.ToLowerInvariant().Split(name.Where(c => char.IsLetterOrDigit(c) is false).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries));
}
