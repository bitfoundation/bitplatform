using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using System.ComponentModel;
using Bit.Bink.Demo.Server.Services;

namespace Bit.Bink.Demo.Server.Controllers;

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
/// A client showing <c>bink://guide/{heading}</c> in a picker cannot fill it in, because nothing on
/// the wire says which headings exist. Two things close that gap - the top-level guide sections are
/// also listed as concrete resources (see <c>ListGuideSections</c>, wired up in Program.cs), and
/// every placeholder here is completable through <c>completion/complete</c>.
/// </para>
/// </summary>
[McpServerResourceType]
public static class McpResources
{
    [McpServerResource(UriTemplate = "bink://guide", Name = "bink-guide", Title = "Bit.Bink reference guide", MimeType = "text/markdown")]
    [Description("The complete Bit.Bink reference guide (the library's README), every section in one document.")]
    public static string Guide() => BinkSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "bink://guide/{heading}", Name = "bink-guide-section", Title = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the Bit.Bink reference guide by heading, e.g. bink://guide/Saving%20and%20loading.")]
    public static string GuideSection(string heading)
        => BinkSourceCatalog.GetGuideSection(heading) ?? $"The guide has no section called '{heading}'.";

    [McpServerResource(UriTemplate = "bink://api", Name = "bink-api", Title = "Bit.Bink public API", MimeType = "text/markdown")]
    [Description("Every public Bit.Bink type with its kind and summary.")]
    public static string ApiList() => BinkApiCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "bink://api/{typeName}", Name = "bink-api-type", Title = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.Bink type, e.g. bink://api/BinkCanvas.")]
    public static string ApiType(string typeName)
        => BinkApiCatalog.RenderType(typeName) ?? $"Bit.Bink has no public type called '{typeName}'.";

    [McpServerResource(UriTemplate = "bink://brushes", Name = "bink-brushes", Title = "Built-in brushes", MimeType = "text/markdown")]
    [Description("Every brush Bit.Bink registers by default, with its real default values.")]
    public static string Brushes() => BinkBrushCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "bink://brushes/{kind}", Name = "bink-brush", Title = "Brush reference", MimeType = "text/markdown")]
    [Description("One built-in brush in full, with the JSON a stroke stores it as, e.g. bink://brushes/pen.")]
    public static string Brush(string kind)
        => BinkBrushCatalog.Render(kind) ?? $"Bit.Bink registers no brush called '{kind}'.";

    [McpServerResource(UriTemplate = "bink://sample-document", Name = "bink-sample-document", Title = "Sample Bink document", MimeType = "application/json")]
    [Description("A small Bink document written by BinkSerializer with readable points: the format, by example.")]
    public static string SampleDocument() => BinkDocumentLab.Sample;

    [McpServerResource(UriTemplate = "bink://source/{path}", Name = "bink-source", Title = "Source file", MimeType = "text/plain")]
    [Description("One source file of the demo or the library, e.g. bink://source/Demo%2FClient%2FPages%2FSignature.razor.")]
    public static string Source(string path)
        => BinkSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.";

    /// <summary>
    /// The top-level sections of the guide as resources a client can list, show and pin, rather than
    /// as a placeholder someone has to already know how to fill in.
    /// <para>
    /// These sit alongside the attributed resources above rather than replacing the
    /// <c>bink://guide/{heading}</c> template: the template is what actually serves a read - the
    /// exact URI is looked for first and the templates are matched afterwards - while these give
    /// <c>resources/list</c> something a person can choose from. The sub-sections, the API types and
    /// the source files stay templated, because listing hundreds of them would bury the handful
    /// anyone browses to.
    /// </para>
    /// </summary>
    public static IEnumerable<Resource> ListGuideSections()
    {
        return BinkSourceCatalog.GuideSections
            .Where(section => section.Level == 2)
            .Select(section => new Resource
            {
                Uri = $"bink://guide/{Uri.EscapeDataString(section.Key)}",
                Name = $"bink-guide-{Slug(section.Heading)}",
                Title = $"Guide: {section.Heading}",
                Description = $"The '{section.Heading}' section of the Bit.Bink reference guide ({section.Lines} lines).",
                MimeType = "text/markdown"
            });
    }

    private static string Slug(string heading)
        => string.Join('-', heading.ToLowerInvariant().Split(heading.Where(c => char.IsLetterOrDigit(c) is false).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries));
}
