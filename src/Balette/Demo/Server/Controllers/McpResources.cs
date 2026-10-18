using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using System.ComponentModel;
using Bit.Balette.Demo.Server.Services;

namespace Bit.Balette.Demo.Server.Controllers;

/// <summary>
/// The same body of knowledge the tools serve, exposed as MCP resources.
/// <para>
/// Tools are for an agent that has decided what it needs; resources are for a client that wants to
/// attach documentation to a conversation up front, or let a person browse and pin it. Both read
/// the same catalogs, so neither can go stale relative to the other - and where a tool and a
/// resource take the same argument, they resolve it through the same method, so a key that works
/// in one cannot fail in the other.
/// </para>
/// <para>
/// A client showing <c>balette://guide/{heading}</c> in a picker cannot fill it in, because nothing
/// on the wire says which headings exist. Two things close that gap - the top-level guide sections
/// are also listed as concrete resources (see <c>ListGuideSections</c>, wired up in Program.cs), and
/// every placeholder here is completable through <c>completion/complete</c>.
/// </para>
/// </summary>
[McpServerResourceType]
public static class McpResources
{
    [McpServerResource(UriTemplate = "balette://guide", Name = "balette-guide", Title = "Bit.Balette reference guide", MimeType = "text/markdown")]
    [Description("The complete Bit.Balette reference guide (the library's README), every section in one document.")]
    public static string Guide() => BaletteSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "balette://guide/{heading}", Name = "balette-guide-section", Title = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the Bit.Balette reference guide by heading, e.g. balette://guide/Nested%20pages.")]
    public static string GuideSection(string heading)
        => BaletteSourceCatalog.GetGuideSection(heading) ?? $"The guide has no section called '{heading}'.";

    [McpServerResource(UriTemplate = "balette://api", Name = "balette-api", Title = "Bit.Balette public API", MimeType = "text/markdown")]
    [Description("Every public Bit.Balette type with its kind and summary.")]
    public static string ApiList() => BaletteApiCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "balette://api/{typeName}", Name = "balette-api-type", Title = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.Balette type, e.g. balette://api/BaletteCommand.")]
    public static string ApiType(string typeName)
        => BaletteApiCatalog.RenderType(typeName) ?? $"Bit.Balette has no public type called '{typeName}'.";

    [McpServerResource(UriTemplate = "balette://theme", Name = "balette-theme", Title = "Theme variables", MimeType = "text/markdown")]
    [Description("Every CSS custom property of the default theme with its light value and its dark override, and the data-balette hooks of an unstyled palette.")]
    public static string Theme() => BaletteThemeCatalog.Render();

    [McpServerResource(UriTemplate = "balette://source/{path}", Name = "balette-source", Title = "Source file", MimeType = "text/plain")]
    [Description("One source file of the demo or the library, e.g. balette://source/Demo%2FClient%2FServices%2FDemoCommands.cs.")]
    public static string Source(string path)
        => BaletteSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.";

    /// <summary>
    /// The top-level sections of the guide as resources a client can list, show and pin, rather than
    /// as a placeholder someone has to already know how to fill in.
    /// <para>
    /// These sit alongside the attributed resources above rather than replacing the
    /// <c>balette://guide/{heading}</c> template: the template is what actually serves a read - the
    /// exact URI is looked for first and the templates are matched afterwards - while these give
    /// <c>resources/list</c> something a person can choose from. The sub-sections, the API types and
    /// the source files stay templated, because listing hundreds of them would bury the handful
    /// anyone browses to.
    /// </para>
    /// </summary>
    public static IEnumerable<Resource> ListGuideSections()
    {
        return BaletteSourceCatalog.GuideSections
            .Where(section => section.Level == 2)
            .Select(section => new Resource
            {
                Uri = $"balette://guide/{Uri.EscapeDataString(section.Key)}",
                Name = $"balette-guide-{Slug(section.Heading)}",
                Title = $"Guide: {section.Heading}",
                Description = $"The '{section.Heading}' section of the Bit.Balette reference guide ({section.Lines} lines).",
                MimeType = "text/markdown"
            });
    }

    private static string Slug(string heading)
        => string.Join('-', heading.ToLowerInvariant().Split(heading.Where(c => char.IsLetterOrDigit(c) is false).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries));
}
