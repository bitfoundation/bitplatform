using System.Text;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using System.ComponentModel;
using Bit.Brouter.Demo.Client;
using Bit.Brouter.Demo.Server.Services;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Brouter.Demo.Server.Controllers;

/// <summary>
/// The same body of knowledge the tools serve, exposed as MCP resources.
/// <para>
/// Tools are for an agent that has decided what it needs; resources are for a client that wants to
/// attach documentation to a conversation up front, or let a person browse and pin it. Both read
/// the same catalogs, so neither can go stale relative to the other - and where a tool and a
/// resource take the same argument, they resolve it through the same method, so a slug that works
/// in one cannot fail in the other.
/// </para>
/// <para>
/// The templated ones below are only half of what a person browsing needs: a client showing
/// <c>brouter://docs/{slug}</c> in a picker cannot fill it in, because nothing on the wire says
/// which slugs exist. Two things close that gap - the documentation pages are also listed as
/// concrete resources (see <c>ListDocumentationPages</c>, wired up in Program.cs), and every
/// placeholder here is completable through <c>completion/complete</c>.
/// </para>
/// </summary>
[McpServerResourceType]
public class McpResources(HtmlRenderer htmlRenderer)
{
    [McpServerResource(UriTemplate = "brouter://guide", Name = "brouter-guide", Title = "Bit.Brouter reference guide", MimeType = "text/markdown")]
    [Description("The complete Bit.Brouter reference guide (the library's README), every section in one document.")]
    public static string Guide() => BrouterSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "brouter://guide/{heading}", Name = "brouter-guide-section", Title = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the Bit.Brouter reference guide by heading, e.g. brouter://guide/Async%20guards.")]
    public static string GuideSection(string heading)
        => BrouterSourceCatalog.GetGuideSection(heading) ?? $"The guide has no section called '{heading}'.";

    [McpServerResource(UriTemplate = "brouter://api", Name = "brouter-api", Title = "Bit.Brouter public API", MimeType = "text/markdown")]
    [Description("Every public Bit.Brouter type with its kind and summary.")]
    public static string ApiList() => BrouterApiCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "brouter://api/{typeName}", Name = "brouter-api-type", Title = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.Brouter type, e.g. brouter://api/BrouterOptions.")]
    public static string ApiType(string typeName)
        => BrouterApiCatalog.RenderType(typeName) ?? $"Bit.Brouter has no public type called '{typeName}'.";

    [McpServerResource(UriTemplate = "brouter://constraints", Name = "brouter-constraints", Title = "Route constraints", MimeType = "text/markdown")]
    [Description("Every constraint usable inside a route template, with the rule it enforces and a passing and a failing example.")]
    public static string Constraints() => BrouterConstraintReference.Render();

    [McpServerResource(UriTemplate = "brouter://source/{path}", Name = "brouter-source", Title = "Demo source file", MimeType = "text/plain")]
    [Description("One source file of the demo or of the hosting samples, e.g. brouter://source/Demo%2FClient%2FAppRouter.razor.")]
    public static string Source(string path)
        => BrouterSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.";

    [McpServerResource(UriTemplate = "brouter://docs/{slug}", Name = "brouter-docs-page", Title = "Documentation page", MimeType = "text/markdown")]
    [Description("One page of the Bit.Brouter documentation site, rendered as Markdown, e.g. brouter://docs/guards.")]
    public async Task<string> DocsPage(string slug)
    {
        var page = DocsPageRenderer.FindPage(slug);

        if (page is null) return DocsPageRenderer.NoSuchPage(slug);

        var (markdown, error) = await DocsPageRenderer.TryRenderMarkdownAsync(htmlRenderer, page);

        return markdown ?? DocsPageRenderer.Unavailable(page, error);
    }

    /// <summary>
    /// Every documentation page as a resource a client can list, show and pin, rather than as a
    /// placeholder someone has to already know how to fill in.
    /// <para>
    /// These sit alongside the attributed resources above rather than replacing the
    /// <c>brouter://docs/{slug}</c> template: the template is what actually serves a read - the
    /// exact URI is looked for first and the templates are matched afterwards - while these give
    /// <c>resources/list</c> something a person can choose from. The pages are the one set here
    /// small and self-contained enough to enumerate; the API types and the source files stay
    /// templated, because listing hundreds of them would bury the handful anyone browses to.
    /// </para>
    /// </summary>
    public static IEnumerable<Resource> ListDocumentationPages()
    {
        return DocsCatalog.Sections.SelectMany(section => section.Pages.Select(page => new Resource
        {
            Uri = $"brouter://docs/{(page.Slug.Length == 0 ? "overview" : page.Slug)}",
            Name = $"brouter-docs-{(page.Slug.Length == 0 ? "overview" : page.Slug)}",
            Title = $"{section.Title}: {page.Title}",
            Description = page.Description,
            MimeType = "text/markdown"
        }));
    }
}
