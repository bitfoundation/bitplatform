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
/// </summary>
[McpServerResourceType]
public class McpResources(HtmlRenderer htmlRenderer, NavigationManager navigationManager, IHttpContextAccessor httpContextAccessor)
{
    [McpServerResource(UriTemplate = "butil://guide", Name = "Bit.Butil reference guide", MimeType = "text/markdown")]
    [Description("The complete Bit.Butil reference guide (the library's README), every section in one document.")]
    public static string Guide() => ButilSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "butil://guide/{heading}", Name = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the Bit.Butil reference guide by heading, e.g. butil://guide/Subscriptions%20are%20disposable.")]
    public static string GuideSection(string heading)
        => ButilSourceCatalog.GetGuideSection(heading) ?? $"The guide has no section called '{heading}'.";

    [McpServerResource(UriTemplate = "butil://api", Name = "Bit.Butil public API", MimeType = "text/markdown")]
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

    [McpServerResource(UriTemplate = "butil://api/{typeName}", Name = "Type reference", MimeType = "text/markdown")]
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

    [McpServerResource(UriTemplate = "butil://support", Name = "Browser support matrix", MimeType = "text/markdown")]
    [Description("Every browser API Bit.Butil wraps, with the engines that implement it and what it needs from the page.")]
    public static string Support()
    {
        var builder = new StringBuilder("# Bit.Butil browser support\n\n");

        builder.AppendLine("| API | Services | Engines | Requires |");
        builder.AppendLine("| --- | --- | --- | --- |");

        foreach (var capability in ButilCapabilityCatalog.Capabilities)
        {
            var requires = capability.Requires.Length == 0
                ? "-"
                // Only the name of each precondition: the table is a map, and the sentence that
                // explains it is one InspectButilApi call away.
                : string.Join(", ", capability.Requires.Select(Name));

            builder.AppendLine($"| [{capability.Api}]({capability.DocsUrl}) | {string.Join(", ", capability.Services)} | {capability.BrowserSupport} | {requires} |");
        }

        return builder.ToString();

        // "Secure context: only available over HTTPS or on localhost." -> "Secure context".
        static string Name(string label)
        {
            var colon = label.IndexOf(':', StringComparison.Ordinal);

            return colon > 0 ? label[..colon] : label;
        }
    }

    [McpServerResource(UriTemplate = "butil://source/{path}", Name = "Demo source file", MimeType = "text/plain")]
    [Description("One source file of the demo or of the hosting samples, e.g. butil://source/Demo%2FClient%2FPages%2FClipboardPage.razor.")]
    public static string Source(string path)
        => ButilSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.";

    [McpServerResource(UriTemplate = "butil://docs/{slug}", Name = "Documentation page", MimeType = "text/markdown")]
    [Description("One page of the Bit.Butil documentation site, rendered as Markdown, e.g. butil://docs/clipboard.")]
    public async Task<string> DocsPage(string slug)
    {
        var page = DocsNav.FindByUrl(slug);
        if (page is null) return $"No documentation page has the slug '{slug}'.";

        var request = httpContextAccessor.HttpContext?.Request;
        var baseUri = request is null ? "https://localhost/" : $"{request.Scheme}://{request.Host}/";

        var (markdown, error) = await DocsPageRenderer.TryRenderMarkdownAsync(htmlRenderer, navigationManager, baseUri, page);

        return markdown ?? DocsPageRenderer.Unavailable(page, error);
    }
}
