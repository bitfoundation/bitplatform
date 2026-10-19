using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using System.ComponentModel;
using Bit.Bdnd.Demo.Server.Services;

namespace Bit.Bdnd.Demo.Server.Controllers;

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
/// A client showing <c>bdnd://guide/{heading}</c> in a picker cannot fill it in, because nothing on
/// the wire says which headings exist. Two things close that gap - the top-level guide sections are
/// also listed as concrete resources (see <c>ListGuideSections</c>, wired up in Program.cs), and
/// every placeholder here is completable through <c>completion/complete</c>.
/// </para>
/// </summary>
[McpServerResourceType]
public static class McpResources
{
    [McpServerResource(UriTemplate = "bdnd://guide", Name = "bdnd-guide", Title = "Bit.Bdnd reference guide", MimeType = "text/markdown")]
    [Description("The complete Bit.Bdnd reference guide (the library's README), every section in one document.")]
    public static string Guide() => BdndSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "bdnd://guide/{heading}", Name = "bdnd-guide-section", Title = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the Bit.Bdnd reference guide by heading, e.g. bdnd://guide/Drop%20results.")]
    public static string GuideSection(string heading)
        => BdndSourceCatalog.GetGuideSection(heading) ?? $"The guide has no section called '{heading}'.";

    [McpServerResource(UriTemplate = "bdnd://api", Name = "bdnd-api", Title = "Bit.Bdnd public API", MimeType = "text/markdown")]
    [Description("Every public Bit.Bdnd type with its kind and summary.")]
    public static string ApiList() => BdndApiCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "bdnd://api/{typeName}", Name = "bdnd-api-type", Title = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.Bdnd type, e.g. bdnd://api/BdndDropTarget.")]
    public static string ApiType(string typeName)
        => BdndApiCatalog.RenderType(typeName) ?? $"Bit.Bdnd has no public type called '{typeName}'.";

    [McpServerResource(UriTemplate = "bdnd://sample-scenario", Name = "bdnd-sample-scenario", Title = "Sample drag scenario", MimeType = "application/json")]
    [Description("A drag over nested drop targets in the shape InspectBdndDrop reads - a draggable, a tree of targets and the hovered one: the format, by example.")]
    public static string SampleScenario() => BdndDropLab.Sample;

    [McpServerResource(UriTemplate = "bdnd://source/{path}", Name = "bdnd-source", Title = "Source file", MimeType = "text/plain")]
    [Description("One source file of the demo or the library, e.g. bdnd://source/Demo%2FClient%2FPages%2FSortable%2FSortableCard.razor.")]
    public static string Source(string path)
        => BdndSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.";

    /// <summary>
    /// The top-level sections of the guide as resources a client can list, show and pin, rather than
    /// as a placeholder someone has to already know how to fill in.
    /// <para>
    /// These sit alongside the attributed resources above rather than replacing the
    /// <c>bdnd://guide/{heading}</c> template: the template is what actually serves a read - the
    /// exact URI is looked for first and the templates are matched afterwards - while these give
    /// <c>resources/list</c> something a person can choose from. The sub-sections, the API types and
    /// the source files stay templated, because listing all of them would bury the handful anyone
    /// browses to.
    /// </para>
    /// </summary>
    public static IEnumerable<Resource> ListGuideSections()
    {
        return BdndSourceCatalog.GuideSections
            .Where(section => section.Level == 2)
            .Select(section => new Resource
            {
                Uri = $"bdnd://guide/{Uri.EscapeDataString(section.Key)}",
                Name = $"bdnd-guide-{Slug(section.Heading)}",
                Title = $"Guide: {section.Heading}",
                Description = $"The '{section.Heading}' section of the Bit.Bdnd reference guide ({section.Lines} lines).",
                MimeType = "text/markdown"
            });
    }

    private static string Slug(string heading)
        => string.Join('-', heading.ToLowerInvariant().Split(heading.Where(c => char.IsLetterOrDigit(c) is false).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries));
}
