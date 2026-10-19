using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using System.ComponentModel;
using Bit.Bortable.Demo.Server.Services;

namespace Bit.Bortable.Demo.Server.Controllers;

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
/// A client showing <c>bortable://guide/{heading}</c> in a picker cannot fill it in, because nothing
/// on the wire says which headings exist. Two things close that gap - the top-level guide sections are
/// also listed as concrete resources (see <c>ListGuideSections</c>, wired up in Program.cs), and
/// every placeholder here is completable through <c>completion/complete</c>.
/// </para>
/// </summary>
[McpServerResourceType]
public static class McpResources
{
    [McpServerResource(UriTemplate = "bortable://guide", Name = "bortable-guide", Title = "Bit.Bortable reference guide", MimeType = "text/markdown")]
    [Description("The complete Bit.Bortable reference guide (the library's README), every section in one document.")]
    public static string Guide() => BortableSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "bortable://guide/{heading}", Name = "bortable-guide-section", Title = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the Bit.Bortable reference guide by heading, e.g. bortable://guide/Shared%20lists.")]
    public static string GuideSection(string heading)
        => BortableSourceCatalog.GetGuideSection(heading) ?? $"The guide has no section called '{heading}'.";

    [McpServerResource(UriTemplate = "bortable://api", Name = "bortable-api", Title = "Bit.Bortable public API", MimeType = "text/markdown")]
    [Description("Every public Bit.Bortable type with its kind and summary.")]
    public static string ApiList() => BortableApiCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "bortable://api/{typeName}", Name = "bortable-api-type", Title = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.Bortable type, e.g. bortable://api/Bortable.")]
    public static string ApiType(string typeName)
        => BortableApiCatalog.RenderType(typeName) ?? $"Bit.Bortable has no public type called '{typeName}'.";

    [McpServerResource(UriTemplate = "bortable://theme", Name = "bortable-theme", Title = "Theme custom properties and classes", MimeType = "text/markdown")]
    [Description("Every --bit-bortable-* custom property of the shipped stylesheet with its default and what it paints, and every drag-state class with the parameter that replaces it.")]
    public static string Theme() => BortableThemeCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "bortable://sample-drag", Name = "bortable-sample-drag", Title = "Sample drag", MimeType = "application/json")]
    [Description("Two lists of a board and a card dragged between them, as InspectBortableDrag reads it: the input format, by example.")]
    public static string SampleDrag() => BortableDragLab.Sample;

    [McpServerResource(UriTemplate = "bortable://source/{path}", Name = "bortable-source", Title = "Source file", MimeType = "text/plain")]
    [Description("One source file of the demo or the library, e.g. bortable://source/Demo%2FClient%2FPages%2FShared.razor.")]
    public static string Source(string path)
        => BortableSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.";

    /// <summary>
    /// The top-level sections of the guide as resources a client can list, show and pin, rather than
    /// as a placeholder someone has to already know how to fill in.
    /// <para>
    /// These sit alongside the attributed resources above rather than replacing the
    /// <c>bortable://guide/{heading}</c> template: the template is what actually serves a read - the
    /// exact URI is looked for first and the templates are matched afterwards - while these give
    /// <c>resources/list</c> something a person can choose from. The sub-sections, the API types and
    /// the source files stay templated, because listing all of them would bury the handful anyone
    /// browses to.
    /// </para>
    /// </summary>
    public static IEnumerable<Resource> ListGuideSections()
    {
        return BortableSourceCatalog.GuideSections
            .Where(section => section.Level == 2)
            .Select(section => new Resource
            {
                Uri = $"bortable://guide/{Uri.EscapeDataString(section.Key)}",
                Name = $"bortable-guide-{Slug(section.Heading)}",
                Title = $"Guide: {section.Heading}",
                Description = $"The '{section.Heading}' section of the Bit.Bortable reference guide ({section.Lines} lines).",
                MimeType = "text/markdown"
            });
    }

    private static string Slug(string heading)
        => string.Join('-', heading.ToLowerInvariant().Split(heading.Where(c => char.IsLetterOrDigit(c) is false).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries));
}
