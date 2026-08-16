using ModelContextProtocol.Server;
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
/// the same catalogs, so neither can go stale relative to the other.
/// </para>
/// </summary>
[McpServerResourceType]
public class McpResources(HtmlRenderer htmlRenderer)
{
    [McpServerResource(UriTemplate = "brouter://guide", Name = "Bit.Brouter reference guide", MimeType = "text/markdown")]
    [Description("The complete Bit.Brouter reference guide (the library's README), every section in one document.")]
    public static string Guide() => BrouterSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "brouter://guide/{heading}", Name = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the Bit.Brouter reference guide by heading, e.g. brouter://guide/Async%20guards.")]
    public static string GuideSection(string heading)
        => BrouterSourceCatalog.GetGuideSection(heading) ?? $"The guide has no section called '{heading}'.";

    [McpServerResource(UriTemplate = "brouter://api", Name = "Bit.Brouter public API", MimeType = "text/markdown")]
    [Description("Every public Bit.Brouter type with its kind and summary.")]
    public static string ApiList()
    {
        var lines = BrouterApiCatalog.Types.Select(type => $"- **{type.Name}** ({type.Kind}) - {type.Summary}");

        return $"# Bit.Brouter public API\n\n{string.Join('\n', lines)}";
    }

    [McpServerResource(UriTemplate = "brouter://api/{typeName}", Name = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.Brouter type, e.g. brouter://api/BrouterOptions.")]
    public static string ApiType(string typeName)
    {
        var details = BrouterApiCatalog.GetTypeDetails(typeName);
        if (details is null) return $"Bit.Brouter has no public type called '{typeName}'.";

        var builder = new System.Text.StringBuilder();

        builder.AppendLine($"# {details.Name} ({details.Kind})").AppendLine();
        if (details.Summary is not null) builder.AppendLine(details.Summary).AppendLine();
        if (details.Remarks is not null) builder.AppendLine(details.Remarks).AppendLine();

        foreach (var group in details.Members.GroupBy(m => m.Kind))
        {
            builder.AppendLine($"## {group.Key}").AppendLine();

            foreach (var member in group)
            {
                builder.Append($"- **{member.Name}**{member.Signature}");
                if (member.Type is not null) builder.Append($" : `{member.Type}`");
                if (member.Default is not null) builder.Append($" = `{member.Default}`");
                if (member.Required) builder.Append(" **(required)**");
                if (member.Summary is not null) builder.Append($" - {member.Summary}");
                builder.AppendLine();
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    [McpServerResource(UriTemplate = "brouter://source/{path}", Name = "Demo source file", MimeType = "text/plain")]
    [Description("One source file of the demo or of the hosting samples, e.g. brouter://source/Demo%2FClient%2FAppRouter.razor.")]
    public static string Source(string path)
        => BrouterSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.";

    [McpServerResource(UriTemplate = "brouter://docs/{slug}", Name = "Documentation page", MimeType = "text/markdown")]
    [Description("One page of the Bit.Brouter documentation site, rendered as Markdown, e.g. brouter://docs/guards.")]
    public async Task<string> DocsPage(string slug)
    {
        var page = DocsCatalog.FindBySlug(slug is "overview" or "index" ? string.Empty : slug);
        if (page is null) return $"No documentation page has the slug '{slug}'.";

        var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await htmlRenderer.RenderComponentAsync(page.PageType);

            return component.ToHtmlString();
        });

        return html.ToMarkdown();
    }
}
