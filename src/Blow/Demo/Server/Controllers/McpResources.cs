using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using System.ComponentModel;
using Bit.Blow.Demo.Server.Services;

namespace Bit.Blow.Demo.Server.Controllers;

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
/// A client showing <c>blow://guide/{heading}</c> in a picker cannot fill it in, because nothing on
/// the wire says which headings exist. Two things close that gap - the top-level guide sections are
/// also listed as concrete resources (see <c>ListGuideSections</c>, wired up in Program.cs), and
/// every placeholder here is completable through <c>completion/complete</c>.
/// </para>
/// </summary>
[McpServerResourceType]
public static class McpResources
{
    [McpServerResource(UriTemplate = "blow://guide", Name = "blow-guide", Title = "Bit.Blow reference guide", MimeType = "text/markdown")]
    [Description("The complete Bit.Blow reference guide (the library's README), every section in one document.")]
    public static string Guide() => BlowSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "blow://guide/{heading}", Name = "blow-guide-section", Title = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the Bit.Blow reference guide by heading, e.g. blow://guide/Custom%20nodes.")]
    public static string GuideSection(string heading)
        => BlowSourceCatalog.GetGuideSection(heading) ?? $"The guide has no section called '{heading}'.";

    [McpServerResource(UriTemplate = "blow://api", Name = "blow-api", Title = "Bit.Blow public API", MimeType = "text/markdown")]
    [Description("Every public Bit.Blow type with its kind and summary.")]
    public static string ApiList() => BlowApiCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "blow://api/{typeName}", Name = "blow-api-type", Title = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.Blow type, e.g. blow://api/BlowCanvas.")]
    public static string ApiType(string typeName)
        => BlowApiCatalog.RenderType(typeName) ?? $"Bit.Blow has no public type called '{typeName}'.";

    [McpServerResource(UriTemplate = "blow://theme", Name = "blow-theme", Title = "Theme custom properties", MimeType = "text/markdown")]
    [Description("Every --bit-blow-* custom property of the shipped stylesheet, with its light and dark value and what it paints.")]
    public static string Theme() => BlowThemeCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "blow://sample-flow", Name = "blow-sample-flow", Title = "Sample Blow flow", MimeType = "application/json")]
    [Description("A small flow - a sub-flow included - built from BlowNode and BlowEdge and written by System.Text.Json: the format, by example.")]
    public static string SampleFlow() => BlowFlowLab.Sample;

    [McpServerResource(UriTemplate = "blow://source/{path}", Name = "blow-source", Title = "Source file", MimeType = "text/plain")]
    [Description("One source file of the demo or the library, e.g. blow://source/Demo%2FClient%2FPages%2FCustomNodes.razor.")]
    public static string Source(string path)
        => BlowSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.";

    /// <summary>
    /// The top-level sections of the guide as resources a client can list, show and pin, rather than
    /// as a placeholder someone has to already know how to fill in.
    /// <para>
    /// These sit alongside the attributed resources above rather than replacing the
    /// <c>blow://guide/{heading}</c> template: the template is what actually serves a read - the
    /// exact URI is looked for first and the templates are matched afterwards - while these give
    /// <c>resources/list</c> something a person can choose from. The sub-sections, the API types and
    /// the source files stay templated, because listing hundreds of them would bury the handful
    /// anyone browses to.
    /// </para>
    /// </summary>
    public static IEnumerable<Resource> ListGuideSections()
    {
        return BlowSourceCatalog.GuideSections
            .Where(section => section.Level == 2)
            .Select(section => new Resource
            {
                Uri = $"blow://guide/{Uri.EscapeDataString(section.Key)}",
                Name = $"blow-guide-{Slug(section.Heading)}",
                Title = $"Guide: {section.Heading}",
                Description = $"The '{section.Heading}' section of the Bit.Blow reference guide ({section.Lines} lines).",
                MimeType = "text/markdown"
            });
    }

    private static string Slug(string heading)
        => string.Join('-', heading.ToLowerInvariant().Split(heading.Where(c => char.IsLetterOrDigit(c) is false).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries));
}
