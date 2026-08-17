using ModelContextProtocol.Server;
using System.ComponentModel;
using Bit.Bswup.Demo.Client;
using Bit.Bswup.Demo.Server.Services;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bswup.Demo.Server.Controllers;

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
    [McpServerResource(UriTemplate = "bswup://guide", Name = "bit Bswup reference guide", MimeType = "text/markdown")]
    [Description("The complete bit Bswup reference guide (the library's README), every section in one document.")]
    public static string Guide() => BswupSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "bswup://guide/{heading}", Name = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the bit Bswup reference guide by heading, e.g. bswup://guide/JavaScript%20API.")]
    public static string GuideSection(string heading)
        => BswupSourceCatalog.GetGuideSection(heading) ?? $"The guide has no section called '{heading}'.";

    [McpServerResource(UriTemplate = "bswup://settings", Name = "Service worker settings", MimeType = "text/markdown")]
    [Description("Every self.* setting of the service-worker file, with its type, default and summary.")]
    public static string Settings()
    {
        var lines = BswupScriptCatalog.WorkerSettings.Select(setting =>
            $"- **self.{setting.Name}**{(setting.Type is null ? null : $" : `{setting.Type}`")}" +
            $"{(setting.Default is null ? null : $" = `{setting.Default}`")} - {setting.Summary}");

        return $"""
            # bit Bswup service-worker settings

            Assigned on `self` in `service-worker.js` (and `service-worker.published.js`) BEFORE
            `self.importScripts('_content/Bit.Bswup/bit-bswup.sw.js')`.

            {string.Join('\n', lines)}

            ## Built-in asset include patterns

            {string.Join(", ", BswupScriptCatalog.DefaultAssetsInclude.Select(pattern => $"`{pattern}`"))}

            ## Built-in asset exclude patterns

            {string.Join(", ", BswupScriptCatalog.DefaultAssetsExclude.Select(pattern => $"`{pattern}`"))}
            """;
    }

    [McpServerResource(UriTemplate = "bswup://options", Name = "Script tag options", MimeType = "text/markdown")]
    [Description("Every attribute of the bit-bswup.js script tag, with its default and summary.")]
    public static string Options()
    {
        var lines = BswupScriptCatalog.ScriptOptions.Select(option =>
            $"- **{option.Name}**{(option.Type is null ? null : $" : `{option.Type}`")}" +
            $"{(option.Default is null ? null : $" = `{option.Default}`")} - {option.Summary}");

        return $"# bit Bswup script-tag options\n\n{string.Join('\n', lines)}";
    }

    [McpServerResource(UriTemplate = "bswup://events", Name = "Lifecycle events", MimeType = "text/markdown")]
    [Description("The lifecycle messages a Bswup handler receives, with the payload each one carries.")]
    public static string Events()
    {
        var lines = BswupScriptCatalog.Events.Select(message =>
            $"- **BswupMessage.{message.Name}** (`'{message.Message}'`)" +
            $"{(message.Payload is null ? null : $" - data: `{message.Payload}`")} - {message.Summary}" +
            $"{(message.Deprecated is null ? null : $" **DEPRECATED:** {message.Deprecated}")}");

        return $"# bit Bswup lifecycle events\n\n{string.Join('\n', lines)}";
    }

    [McpServerResource(UriTemplate = "bswup://source/{path}", Name = "Source file", MimeType = "text/plain")]
    [Description("One source file of the library, the demo or the samples, e.g. bswup://source/Library%2FScripts%2Fbit-bswup.sw.ts.")]
    public static string Source(string path)
        => BswupSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.";

    [McpServerResource(UriTemplate = "bswup://docs/{slug}", Name = "Documentation page", MimeType = "text/markdown")]
    [Description("One page of the bit Bswup documentation site, rendered as Markdown, e.g. bswup://docs/service-worker.")]
    public async Task<string> DocsPage(string slug)
    {
        var page = DocsCatalog.FindBySlug(slug is "overview" or "index" or "home" ? string.Empty : slug);
        if (page is null) return $"No documentation page has the slug '{slug}'.";

        var (markdown, error) = await DocsPageRenderer.TryRenderMarkdownAsync(htmlRenderer, page);

        return markdown ?? DocsPageRenderer.Unavailable(page, error);
    }
}
