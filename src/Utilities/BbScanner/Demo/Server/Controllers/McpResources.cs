using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using System.ComponentModel;
using Bit.BbScanner.Demo.Server.Services;

namespace Bit.BbScanner.Demo.Server.Controllers;

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
/// A client showing <c>bbscanner://guide/{heading}</c> in a picker cannot fill it in, because nothing on
/// the wire says which headings exist. Two things close that gap - the top-level guide sections are
/// also listed as concrete resources (see <c>ListGuideSections</c>, wired up in Program.cs), and
/// every placeholder here is completable through <c>completion/complete</c>.
/// </para>
/// </summary>
[McpServerResourceType]
public static class McpResources
{
    [McpServerResource(UriTemplate = "bbscanner://guide", Name = "bbscanner-guide", Title = "Bit.BbScanner reference guide", MimeType = "text/markdown")]
    [Description("The complete Bit.BbScanner reference guide (the library's README), every section in one document.")]
    public static string Guide() => BbScannerSourceCatalog.Readme;

    [McpServerResource(UriTemplate = "bbscanner://guide/{heading}", Name = "bbscanner-guide-section", Title = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the Bit.BbScanner reference guide by heading, e.g. bbscanner://guide/Using%20the%20component.")]
    public static string GuideSection(string heading)
        => BbScannerSourceCatalog.GetGuideSection(heading) ?? $"The guide has no section called '{heading}'.";

    [McpServerResource(UriTemplate = "bbscanner://api", Name = "bbscanner-api", Title = "Bit.BbScanner public API", MimeType = "text/markdown")]
    [Description("Every public Bit.BbScanner type with its kind and summary.")]
    public static string ApiList() => BbScannerApiCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "bbscanner://api/{typeName}", Name = "bbscanner-api-type", Title = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.BbScanner type, e.g. bbscanner://api/BbScanner.")]
    public static string ApiType(string typeName)
        => BbScannerApiCatalog.RenderType(typeName) ?? $"Bit.BbScanner has no public type called '{typeName}'.";

    [McpServerResource(UriTemplate = "bbscanner://formats", Name = "bbscanner-formats", Title = "Supported symbologies", MimeType = "text/markdown")]
    [Description("Every symbology Bit.BbScanner reads, with its flag, its status and whether the pipeline reads it.")]
    public static string Formats() => BbScannerFormatCatalog.RenderIndex();

    [McpServerResource(UriTemplate = "bbscanner://formats/{format}", Name = "bbscanner-format", Title = "Symbology reference", MimeType = "text/markdown")]
    [Description("One symbology in full, with the options that steer it and their real defaults, e.g. bbscanner://formats/QrCode.")]
    public static string Format(string format)
        => BbScannerFormatCatalog.Render(format) ?? $"Bit.BbScanner reads no symbology called '{format}'.";

    [McpServerResource(UriTemplate = "bbscanner://source/{path}", Name = "bbscanner-source", Title = "Source file", MimeType = "text/plain")]
    [Description("One source file of the demo or the library, e.g. bbscanner://source/Demo%2FClient%2FPages%2FScan.razor.")]
    public static string Source(string path)
        => BbScannerSourceCatalog.GetSourceFile(path) ?? $"No source file at '{path}'.";

    /// <summary>
    /// The top-level sections of the guide as resources a client can list, show and pin, rather than
    /// as a placeholder someone has to already know how to fill in.
    /// <para>
    /// These sit alongside the attributed resources above rather than replacing the
    /// <c>bbscanner://guide/{heading}</c> template: the template is what actually serves a read - the
    /// exact URI is looked for first and the templates are matched afterwards - while these give
    /// <c>resources/list</c> something a person can choose from. The sub-sections, the API types and
    /// the source files stay templated, because listing hundreds of them would bury the handful
    /// anyone browses to.
    /// </para>
    /// </summary>
    public static IEnumerable<Resource> ListGuideSections()
    {
        return BbScannerSourceCatalog.GuideSections
            .Where(section => section.Level == 2)
            .Select(section => new Resource
            {
                Uri = $"bbscanner://guide/{Uri.EscapeDataString(section.Key)}",
                Name = $"bbscanner-guide-{Slug(section.Heading)}",
                Title = $"Guide: {section.Heading}",
                Description = $"The '{section.Heading}' section of the Bit.BbScanner reference guide ({section.Lines} lines).",
                MimeType = "text/markdown"
            });
    }

    private static string Slug(string heading)
        => string.Join('-', heading.ToLowerInvariant().Split(heading.Where(c => char.IsLetterOrDigit(c) is false).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries));
}
