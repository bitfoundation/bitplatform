using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using System.ComponentModel;
using Bit.BlazeDb.Demo.Server.Services;

namespace Bit.BlazeDb.Demo.Server.Controllers;

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
/// A client showing <c>blazedb://guide/{heading}</c> in a picker cannot fill it in, because nothing on
/// the wire says which headings exist. Two things close that gap - the top-level guide sections are
/// also listed as concrete resources (see <c>ListGuideSections</c>, wired up in Program.cs), and
/// every placeholder here is completable through <c>completion/complete</c>.
/// </para>
/// </summary>
[McpServerResourceType]
public static class McpResources
{
    [McpServerResource(UriTemplate = "blazedb://guide", Name = "blazedb-guide", Title = "Bit.BlazeDb reference guide", MimeType = "text/markdown")]
    [Description("The complete Bit.BlazeDb reference guide (the library's README), every section in one document.")]
    public static string Guide() => BlazeDbSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "blazedb://guide/{heading}", Name = "blazedb-guide-section", Title = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the Bit.BlazeDb reference guide by heading, e.g. blazedb://guide/Multiple%20tabs.")]
    public static string GuideSection(string heading)
        => BlazeDbSourceCatalog.GetGuideSection(heading) ?? $"The guide has no section called '{heading}'.";

    [McpServerResource(UriTemplate = "blazedb://api", Name = "blazedb-api", Title = "Bit.BlazeDb public API", MimeType = "text/markdown")]
    [Description("Every public Bit.BlazeDb type, grouped by package, with its kind and summary.")]
    public static string ApiList() => BlazeDbApiCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "blazedb://api/{typeName}", Name = "blazedb-api-type", Title = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.BlazeDb type, e.g. blazedb://api/BlazeDbDatabaseOptions.")]
    public static string ApiType(string typeName)
        => BlazeDbApiCatalog.RenderType(typeName) ?? $"Bit.BlazeDb has no public type called '{typeName}'.";

    [McpServerResource(UriTemplate = "blazedb://diagnostics", Name = "blazedb-diagnostics", Title = "Generator diagnostics", MimeType = "text/markdown")]
    [Description("Every BLZ diagnostic the Bit.BlazeDb source generator reports, with its severity.")]
    public static string Diagnostics() => BlazeDbDiagnosticsReference.RenderIndex();

    [McpServerResource(UriTemplate = "blazedb://diagnostics/{code}", Name = "blazedb-diagnostic", Title = "Diagnostic reference", MimeType = "text/markdown")]
    [Description("One generator diagnostic in full, with a rejected table type and the fix, e.g. blazedb://diagnostics/BLZ013.")]
    public static string Diagnostic(string code)
        => BlazeDbDiagnosticsReference.Find(code) is { } entry ? BlazeDbDiagnosticsReference.Render(entry) : $"The BlazeDb generator has no diagnostic '{code}'.";

    [McpServerResource(UriTemplate = "blazedb://source/{path}", Name = "blazedb-source", Title = "Source file", MimeType = "text/plain")]
    [Description("One source file of the demo, e.g. blazedb://source/Demo%2FClient%2FData%2FDemoModels.cs.")]
    public static string Source(string path)
        => BlazeDbSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.";

    /// <summary>
    /// The top-level sections of the guide as resources a client can list, show and pin, rather than
    /// as a placeholder someone has to already know how to fill in.
    /// <para>
    /// These sit alongside the attributed resources above rather than replacing the
    /// <c>blazedb://guide/{heading}</c> template: the template is what actually serves a read - the
    /// exact URI is looked for first and the templates are matched afterwards - while these give
    /// <c>resources/list</c> something a person can choose from. The sub-sections, the API types and
    /// the source files stay templated, because listing hundreds of them would bury the handful
    /// anyone browses to.
    /// </para>
    /// </summary>
    public static IEnumerable<Resource> ListGuideSections()
    {
        return BlazeDbSourceCatalog.GuideSections
            .Where(section => section.Level == 2)
            .Select(section => new Resource
            {
                Uri = $"blazedb://guide/{Uri.EscapeDataString(section.Key)}",
                Name = $"blazedb-guide-{Slug(section.Heading)}",
                Title = $"Guide: {section.Heading}",
                Description = $"The '{section.Heading}' section of the Bit.BlazeDb reference guide ({section.Lines} lines).",
                MimeType = "text/markdown"
            });
    }

    private static string Slug(string heading)
        => string.Join('-', heading.ToLowerInvariant().Split(heading.Where(c => char.IsLetterOrDigit(c) is false).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries));
}
