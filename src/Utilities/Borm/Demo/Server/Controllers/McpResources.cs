using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using System.ComponentModel;
using Bit.Borm.Demo.Server.Services;

namespace Bit.Borm.Demo.Server.Controllers;

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
/// A client showing <c>borm://guide/{heading}</c> in a picker cannot fill it in, because nothing on
/// the wire says which headings exist. Two things close that gap - the top-level guide sections are
/// also listed as concrete resources (see <c>ListGuideSections</c>, wired up in Program.cs), and
/// every placeholder here is completable through <c>completion/complete</c>.
/// </para>
/// </summary>
[McpServerResourceType]
public static class McpResources
{
    [McpServerResource(UriTemplate = "borm://guide", Name = "borm-guide", Title = "Bit.Borm reference guide", MimeType = "text/markdown")]
    [Description("The complete Bit.Borm reference guide (the library's README), every section in one document.")]
    public static string Guide() => BormSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "borm://guide/{heading}", Name = "borm-guide-section", Title = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the Bit.Borm reference guide by heading, e.g. borm://guide/Async%20and%20cascading%20options.")]
    public static string GuideSection(string heading)
        => BormSourceCatalog.GetGuideSection(heading) ?? $"The guide has no section called '{heading}'.";

    [McpServerResource(UriTemplate = "borm://api", Name = "borm-api", Title = "Bit.Borm public API", MimeType = "text/markdown")]
    [Description("Every public Bit.Borm type with its kind and summary.")]
    public static string ApiList() => BormApiCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "borm://api/{typeName}", Name = "borm-api-type", Title = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.Borm type, e.g. borm://api/BormState.")]
    public static string ApiType(string typeName)
        => BormApiCatalog.RenderType(typeName) ?? $"Bit.Borm has no public type called '{typeName}'.";

    [McpServerResource(UriTemplate = "borm://theme", Name = "borm-theme", Title = "Theme custom properties", MimeType = "text/markdown")]
    [Description("Every --bit-borm-* custom property of the shipped stylesheet, with its light and dark value and what it is used for.")]
    public static string Theme() => BormThemeCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "borm://sample-schema", Name = "borm-sample-schema", Title = "Sample Borm JSON Schema", MimeType = "application/schema+json")]
    [Description("A two-step wizard built with BormBuilder and written by BormJsonSchemaExporter - conditions, a select, a range and a tag list included: the format and its x- extensions, by example.")]
    public static string SampleSchema() => BormSchemaLab.Sample;

    [McpServerResource(UriTemplate = "borm://source/{path}", Name = "borm-source", Title = "Source file", MimeType = "text/plain")]
    [Description("One source file of the demo or the library, e.g. borm://source/Demo%2FClient%2FPages%2FWizardDemo.razor.")]
    public static string Source(string path)
        => BormSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.";

    /// <summary>
    /// The top-level sections of the guide as resources a client can list, show and pin, rather than
    /// as a placeholder someone has to already know how to fill in.
    /// <para>
    /// These sit alongside the attributed resources above rather than replacing the
    /// <c>borm://guide/{heading}</c> template: the template is what actually serves a read - the
    /// exact URI is looked for first and the templates are matched afterwards - while these give
    /// <c>resources/list</c> something a person can choose from. The sub-sections, the API types and
    /// the source files stay templated, because listing hundreds of them would bury the handful
    /// anyone browses to.
    /// </para>
    /// </summary>
    public static IEnumerable<Resource> ListGuideSections()
    {
        return BormSourceCatalog.GuideSections
            .Where(section => section.Level == 2)
            .Select(section => new Resource
            {
                Uri = $"borm://guide/{Uri.EscapeDataString(section.Key)}",
                Name = $"borm-guide-{Slug(section.Heading)}",
                Title = $"Guide: {section.Heading}",
                Description = $"The '{section.Heading}' section of the Bit.Borm reference guide ({section.Lines} lines).",
                MimeType = "text/markdown"
            });
    }

    private static string Slug(string heading)
        => string.Join('-', heading.ToLowerInvariant().Split(heading.Where(c => char.IsLetterOrDigit(c) is false).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries));
}
