using System.Text;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Bit.Butil.Demo.Client.Docs;
using Bit.Butil.Demo.Server.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Butil.Demo.Server.Controllers;

/// <summary>
/// The same body of knowledge the tools serve, exposed as MCP resources.
/// <para>
/// Tools are for an agent that has decided what it needs; resources are for a client that wants to
/// attach documentation to a conversation up front, or let a person browse and pin it. Both read
/// the same catalogs, so neither can go stale relative to the other.
/// </para>
/// <para>
/// Each one carries a slug for its Name and a sentence for its Title, which is the split the
/// protocol asks for and the split a resource picker needs: the name is the identifier a client
/// stores and a completion returns, and it has to stay the same across releases, while the title
/// is the line a person reads in the list and is free to be rewritten whenever it reads better.
/// </para>
/// </summary>
[McpServerResourceType]
public class McpResources(HtmlRenderer htmlRenderer, NavigationManager navigationManager, IHttpContextAccessor httpContextAccessor,
                          ILogger<McpResources> logger)
{
    /// <summary>
    /// The one answer here that is deliberately not capped: "every section in one document" is what
    /// this resource is for, and a resource is attached by a person who asked for the whole guide
    /// rather than pulled by a model mid-turn. The per-item resources below are capped like the
    /// tools covering the same material, because those are reached by guessing.
    /// </summary>
    [McpServerResource(UriTemplate = "butil://guide", Name = "butil-guide", Title = "Bit.Butil reference guide", MimeType = "text/markdown")]
    [Description("The complete Bit.Butil reference guide (the library's README), every section in one document.")]
    public static string Guide() => ButilSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "butil://guide/{heading}", Name = "butil-guide-section", Title = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the Bit.Butil reference guide by heading, e.g. butil://guide/Subscriptions%20are%20disposable.")]
    public static string GuideSection(string heading)
        => DocsPageRenderer.Truncate(ButilSourceCatalog.GetGuideSection(heading) ?? $"The guide has no section called '{heading}'.");

    [McpServerResource(UriTemplate = "butil://api", Name = "butil-api", Title = "Bit.Butil public API", MimeType = "text/markdown")]
    [Description("Every public Bit.Butil type with its kind and summary, the injectable services first.")]
    public static string ApiList()
    {
        var builder = new StringBuilder("# Bit.Butil public API\n\n## Injectable services\n\n");

        foreach (var type in ButilApiCatalog.Types.Where(t => t.IsInjectable))
        {
            builder.AppendLine($"- **{type.Name}** - {type.Summary}");
        }

        builder.AppendLine().AppendLine("## Everything else").AppendLine();

        foreach (var type in ButilApiCatalog.Types.Where(t => t.IsInjectable is false))
        {
            builder.AppendLine($"- **{type.Name}** ({type.Kind}) - {type.Summary}");
        }

        return builder.ToString();
    }

    [McpServerResource(UriTemplate = "butil://api/{typeName}", Name = "butil-api-type", Title = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.Butil type, e.g. butil://api/Clipboard.")]
    public static string ApiType(string typeName)
    {
        var details = ButilApiCatalog.GetTypeDetails(typeName);
        if (details is null) return $"Bit.Butil has no public type called '{typeName}'.";

        var builder = new StringBuilder();

        builder.AppendLine($"# {details.Name} ({details.Kind})").AppendLine();
        if (details.Inject is not null) builder.AppendLine($"```razor\n{details.Inject}\n```").AppendLine();
        if (details.Summary is not null) builder.AppendLine(details.Summary).AppendLine();
        if (details.Remarks is not null) builder.AppendLine(details.Remarks).AppendLine();
        if (details.DocsUrl is not null) builder.AppendLine($"Documentation page: {details.DocsUrl}").AppendLine();

        foreach (var group in details.Members.GroupBy(m => m.Kind))
        {
            builder.AppendLine($"## {group.Key}").AppendLine();

            foreach (var member in group)
            {
                builder.Append($"- **{member.Name}**{member.Signature}");
                if (member.Type is not null) builder.Append($" : `{member.Type}`");
                if (member.Default is not null) builder.Append($" = `{member.Default}`");
                if (member.Summary is not null) builder.Append($" - {member.Summary}");
                builder.AppendLine();
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>
    /// The same table <c>GetButilDocsPage</c> answers with when it is asked for no page in
    /// particular. A support matrix and a page index are one set of rows read two ways - which
    /// engines run this, and where is it written up - so they are built once and served twice
    /// rather than drifting apart as two renderings of the same nav. The name and the description
    /// say both halves, because a reader who attached this expecting only the matrix would read the
    /// handful of guide rows as APIs that no engine implements.
    /// </summary>
    [McpServerResource(UriTemplate = "butil://support", Name = "butil-support", Title = "Documentation index and browser support matrix", MimeType = "text/markdown")]
    [Description("Every page of the Bit.Butil documentation site in one table: its slug, what it covers, the services behind it, the engines that implement it and what it needs from the page. Every browser API Bit.Butil wraps has a row - that is the support matrix - and the \"Overview\" rows are guides to the library rather than APIs, marked Guide in place of an engine list.")]
    public static string Support() => ButilIndexes.DocsPages();

    [McpServerResource(UriTemplate = "butil://source/{path}", Name = "butil-source", Title = "Demo source file", MimeType = "text/plain")]
    [Description("One source file of the demo or of the hosting samples, e.g. butil://source/Demo%2FClient%2FPages%2FClipboardPage.razor.")]
    public static string Source(string path)
        => DocsPageRenderer.Truncate(ButilSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.");

    [McpServerResource(UriTemplate = "butil://docs/{slug}", Name = "butil-docs-page", Title = "Documentation page", MimeType = "text/markdown")]
    [Description("One page of the Bit.Butil documentation site, rendered as Markdown, e.g. butil://docs/clipboard.")]
    public async Task<string> DocsPage(string slug, CancellationToken cancellationToken)
    {
        var page = DocsNav.FindByUrl(slug);
        if (page is null) return $"No documentation page has the slug '{slug}'.";

        // The same rendering the tool serves: one render per page and origin, and the same cap on
        // what a single answer may cost a client's context window.
        var (markdown, error) = await DocsPageRenderer.RenderCachedMarkdownAsync(
            htmlRenderer, navigationManager, logger, DocsPageRenderer.BaseUri(httpContextAccessor), page, cancellationToken);

        return markdown is null ? DocsPageRenderer.Unavailable(page, error) : DocsPageRenderer.Truncate(markdown);
    }
}
