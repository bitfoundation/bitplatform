using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using System.ComponentModel;
using Bit.Blex.Demo.Server.Services;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blex.Demo.Server.Controllers;

/// <summary>
/// The same body of knowledge the tools serve, exposed as MCP resources.
/// <para>
/// Tools are for an agent that has decided what it needs; resources are for a client that wants to attach
/// documentation to a conversation up front, or let a person browse and pin it. Both read the same
/// catalogs, and where a tool and a resource take the same argument they resolve it through the same
/// method, so a key that works in one cannot fail in the other. The documentation pages are also listed
/// as concrete resources (see <see cref="ListDocumentationPages"/>, wired up in Program.cs), and every
/// placeholder here is completable through <c>completion/complete</c>.
/// </para>
/// </summary>
[McpServerResourceType]
public class McpResources(HtmlRenderer htmlRenderer)
{
    [McpServerResource(UriTemplate = "blex://guide", Name = "blex-guide", Title = "Bit.Blex reference guide", MimeType = "text/markdown")]
    [Description("The complete Bit.Blex reference guide (the library's README), every section in one document.")]
    public static string Guide() => BlexSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "blex://guide/{heading}", Name = "blex-guide-section", Title = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the Bit.Blex reference guide by heading, e.g. blex://guide/Persistence.")]
    public static string GuideSection(string heading)
        => BlexSourceCatalog.GetGuideSection(heading) ?? $"The guide has no section called '{heading}'.";

    [McpServerResource(UriTemplate = "blex://api", Name = "blex-api", Title = "Bit.Blex public API", MimeType = "text/markdown")]
    [Description("Every public Bit.Blex type with its kind, package and summary.")]
    public static string ApiList() => BlexApiCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "blex://api/{typeName}", Name = "blex-api-type", Title = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.Blex type, e.g. blex://api/BlexOptions.")]
    public static string ApiType(string typeName)
        => BlexApiCatalog.RenderType(typeName) ?? $"Bit.Blex has no public type called '{typeName}'.";

    [McpServerResource(UriTemplate = "blex://diagnostics", Name = "blex-diagnostics", Title = "Generator diagnostics", MimeType = "text/markdown")]
    [Description("Every diagnostic the Bit.Blex source generator reports, with its severity and title.")]
    public static string Diagnostics() => BlexDiagnosticsReference.RenderIndex();

    [McpServerResource(UriTemplate = "blex://diagnostics/{code}", Name = "blex-diagnostic", Title = "Generator diagnostic", MimeType = "text/markdown")]
    [Description("One diagnostic of the Bit.Blex source generator with its cause and fix, e.g. blex://diagnostics/BLEX001.")]
    public static string Diagnostic(string code)
        => BlexDiagnosticsReference.Find(code) is { } entry ? BlexDiagnosticsReference.Render(entry) : $"The Blex generator has no diagnostic '{code}'.";

    [McpServerResource(UriTemplate = "blex://source/{path}", Name = "blex-source", Title = "Demo source file", MimeType = "text/plain")]
    [Description("One source file of the demo or of the standalone sample, e.g. blex://source/Demo%2FClient%2FStores%2FCartStore.cs.")]
    public static string Source(string path)
        => BlexSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.";

    [McpServerResource(UriTemplate = "blex://docs/{slug}", Name = "blex-docs-page", Title = "Documentation page", MimeType = "text/markdown")]
    [Description("One page of the Bit.Blex documentation site, rendered as Markdown, e.g. blex://docs/effects.")]
    public async Task<string> DocsPage(string slug)
    {
        var page = DocsPageRenderer.FindPage(slug);

        if (page is null) return DocsPageRenderer.NoSuchPage(slug);

        var (markdown, error) = await DocsPageRenderer.TryRenderMarkdownAsync(htmlRenderer, page);

        return markdown ?? DocsPageRenderer.Unavailable(page, error);
    }

    /// <summary>
    /// Every documentation page as a resource a client can list, show and pin, rather than as a placeholder
    /// someone has to already know how to fill in. The template above still serves the reads; these give
    /// <c>resources/list</c> something to choose from.
    /// </summary>
    public static IEnumerable<Resource> ListDocumentationPages()
    {
        return DocsPageRenderer.Pages.Select(page => new Resource
        {
            Uri = $"blex://docs/{page.Slug}",
            Name = $"blex-docs-{page.Slug}",
            Title = $"{page.Section}: {page.Title}",
            Description = page.Summary,
            MimeType = "text/markdown"
        });
    }
}
