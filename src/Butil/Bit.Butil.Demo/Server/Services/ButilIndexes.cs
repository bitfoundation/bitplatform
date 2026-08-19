using System.Text;
using Bit.Butil.Demo.Client.Docs;

namespace Bit.Butil.Demo.Server.Services;

/// <summary>
/// The three listings this server hands out: what documentation pages exist, what the reference
/// guide is divided into, and what source files can be asked for.
/// <para>
/// Each of these was a tool of its own once, and none of them earned the place: an agent reaches a
/// listing either through <see cref="ButilSearchIndex"/>, whose every hit already names the call
/// that returns the text, or by calling the retrieval tool with no argument - which is what these
/// answer. A listing costs a tool description in every client's context for the whole of every
/// session, and these are read perhaps once in a session, by an agent that arrived here from a
/// search that had already told it where to go.
/// </para>
/// <para>
/// Markdown rather than a DTO, deliberately. A listing is read, not parsed: what a caller does with
/// it is pick one line and pass a value from it back. Rendering it as a table costs a fraction of
/// the same content as JSON and puts the columns where a model reads them.
/// </para>
/// </summary>
public static class ButilIndexes
{
    /// <summary>
    /// Every documentation page with the API it covers, the services behind it, the engines that
    /// implement it and what it demands of the page - the browser-support matrix and the page index
    /// are the same table, because they were always the same rows read two ways.
    /// </summary>
    public static string DocsPages()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Butil documentation pages").AppendLine();
        builder.AppendLine("Pass a slug to `GetButilDocsPage`. \"Requires\" is what the page has to arrange before the")
               .AppendLine("API works at all; `PlanButilFeature` spells out any of it in full.").AppendLine();

        foreach (var group in DocsNav.Groups)
        {
            builder.AppendLine($"## {group.Title}").AppendLine();
            builder.AppendLine("| Slug | Title | Services | Engines | Requires |");
            builder.AppendLine("| --- | --- | --- | --- | --- |");

            foreach (var link in group.Links)
            {
                var services = link.TypeNames();

                builder.AppendLine($"| `{link.Url}` | {link.Title} | {Join(services)} | {link.Support.Label()} | {Requires(link)} |");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>Every heading of the reference guide, with the size of what it would return.</summary>
    public static string GuideSections()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Butil reference guide").AppendLine();
        builder.AppendLine("Pass a heading to `GetButilGuideSection`. A section is returned with its sub-sections,")
               .AppendLine("so asking for a level-2 heading returns the level-3 ones under it as well.").AppendLine();

        foreach (var section in ButilSourceCatalog.GuideSections)
        {
            // Indented by level, so the nesting a heading belongs to is visible without a Parent column.
            var indent = section.Level > 2 ? new string(' ', (section.Level - 2) * 2) : string.Empty;

            builder.AppendLine($"{indent}- `{section.Heading}` ({section.Lines} lines)");
        }

        return builder.ToString();
    }

    /// <summary>Every source file this server can hand out verbatim.</summary>
    public static string SourceFiles()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Butil working source files").AppendLine();
        builder.AppendLine("Pass a path to `GetButilSourceFile`. Every page of this site is a complete working")
               .AppendLine("example of the API it documents; the samples are the minimal hosting for one model.").AppendLine();

        foreach (var group in ButilSourceCatalog.SourceFiles.GroupBy(file => file.Kind))
        {
            builder.AppendLine($"## {group.Key}").AppendLine();

            foreach (var file in group)
            {
                var description = file.Description is null ? string.Empty : $" - {file.Description}";

                builder.AppendLine($"- `{file.Path}` ({file.Lines} lines){description}");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string Join(string[] values) => values.Length == 0 ? "-" : string.Join(", ", values);

    /// <summary>
    /// The names of a page's preconditions, without the sentence that explains each - the table is a
    /// map, and the explanation is one <c>PlanButilFeature</c> call away.
    /// "Secure context: only available over HTTPS or on localhost." -> "Secure context".
    /// </summary>
    private static string Requires(DocLink link)
    {
        return Join([.. link.Needs.Labels().Select(label =>
        {
            var colon = label.IndexOf(':', StringComparison.Ordinal);

            return colon > 0 ? label[..colon] : label;
        })]);
    }
}
