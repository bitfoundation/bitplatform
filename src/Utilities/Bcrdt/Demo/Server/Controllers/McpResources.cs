using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using System.ComponentModel;
using Bit.Bcrdt.Demo.Server.Services;

namespace Bit.Bcrdt.Demo.Server.Controllers;

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
/// A client showing <c>bcrdt://guide/{heading}</c> in a picker cannot fill it in, because nothing
/// on the wire says which headings exist. Two things close that gap - the top-level guide sections
/// are also listed as concrete resources (see <c>ListGuideSections</c>, wired up in Program.cs), and
/// every placeholder here is completable through <c>completion/complete</c>.
/// </para>
/// </summary>
[McpServerResourceType]
public static class McpResources
{
    [McpServerResource(UriTemplate = "bcrdt://guide", Name = "bcrdt-guide", Title = "Bit.Bcrdt reference guide", MimeType = "text/markdown")]
    [Description("The complete Bit.Bcrdt reference guide (the library's README), every section in one document.")]
    public static string Guide() => BcrdtSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "bcrdt://guide/{heading}", Name = "bcrdt-guide-section", Title = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the Bit.Bcrdt reference guide by heading, e.g. bcrdt://guide/Choosing%20a%20data%20type.")]
    public static string GuideSection(string heading)
        => BcrdtSourceCatalog.GetGuideSection(heading) ?? $"The guide has no section called '{heading}'.";

    [McpServerResource(UriTemplate = "bcrdt://api", Name = "bcrdt-api", Title = "Bit.Bcrdt public API", MimeType = "text/markdown")]
    [Description("Every public type of the four Bit.Bcrdt packages with its kind and summary, by package.")]
    public static string ApiList() => BcrdtApiCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "bcrdt://api/{typeName}", Name = "bcrdt-api-type", Title = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.Bcrdt type, e.g. bcrdt://api/BcrdtCollaborativeDocument.")]
    public static string ApiType(string typeName)
        => BcrdtApiCatalog.RenderType(typeName) ?? $"Bit.Bcrdt has no public type called '{typeName}'.";

    [McpServerResource(UriTemplate = "bcrdt://setup", Name = "bcrdt-setup", Title = "Setup guides", MimeType = "text/markdown")]
    [Description("The hosting models Bit.Bcrdt has a setup guide for, each with what sets it apart.")]
    public static string Setup() => BcrdtSetupGuide.RenderIndex();

    [McpServerResource(UriTemplate = "bcrdt://source/{path}", Name = "bcrdt-source", Title = "Source file", MimeType = "text/plain")]
    [Description("One source file of the demo, e.g. bcrdt://source/Demo%2FClient%2FPages%2FLive.razor.")]
    public static string Source(string path)
        => BcrdtSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.";

    /// <summary>
    /// The top-level sections of the guide as resources a client can list, show and pin, rather than
    /// as a placeholder someone has to already know how to fill in.
    /// <para>
    /// These sit alongside the attributed resources above rather than replacing the
    /// <c>bcrdt://guide/{heading}</c> template: the template is what actually serves a read - the
    /// exact URI is looked for first and the templates are matched afterwards - while these give
    /// <c>resources/list</c> something a person can choose from. The sub-sections, the API types and
    /// the source files stay templated, because listing hundreds of them would bury the handful
    /// anyone browses to.
    /// </para>
    /// </summary>
    public static IEnumerable<Resource> ListGuideSections()
    {
        return BcrdtSourceCatalog.GuideSections
            .Where(section => section.Level == 2)
            .Select(section => new Resource
            {
                Uri = $"bcrdt://guide/{Uri.EscapeDataString(section.Key)}",
                Name = $"bcrdt-guide-{Slug(section.Heading)}",
                Title = $"Guide: {section.Heading}",
                Description = $"The '{section.Heading}' section of the Bit.Bcrdt reference guide ({section.Lines} lines).",
                MimeType = "text/markdown"
            });
    }

    private static string Slug(string heading)
        => string.Join('-', heading.ToLowerInvariant().Split(heading.Where(c => char.IsLetterOrDigit(c) is false).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries));
}
