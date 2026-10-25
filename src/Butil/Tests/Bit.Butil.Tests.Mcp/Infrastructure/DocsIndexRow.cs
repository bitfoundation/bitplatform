using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Butil.Tests.Mcp.Infrastructure;

/// <summary>
/// One row of the index <c>GetButilDocsPage</c> answers with when it is called with no slug - which
/// is also the whole of <c>butil://support</c>, the page listing and the browser-support matrix
/// having been folded into one table.
/// <para>
/// Parsed out of Markdown rather than deserialized, because that is the form the answer takes: a
/// listing is read and then one value from it is passed back, so it ships as a table an agent reads
/// instead of as a DTO with a tool description to advertise it. The suite reads it the same way,
/// which also holds the table's columns to a shape.
/// </para>
/// </summary>
public sealed partial record DocsIndexRow(string Group, string Slug, string Title, string Summary, string[] Services, string Engines, string[] Requires)
{
    /// <summary>Every row of the index, with the group heading each one sat under.</summary>
    public static DocsIndexRow[] ParseAll(string markdown)
    {
        var rows = new List<DocsIndexRow>();
        var group = string.Empty;

        foreach (var line in markdown.Split('\n').Select(line => line.TrimEnd('\r')))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                group = line[3..].Trim();
                continue;
            }

            var match = RowRegex().Match(line);
            if (match.Success is false) continue;

            // Split on the pipes rather than on the pattern: the row is six cells, and a cell that
            // went missing should read as a short row here rather than as a row that did not match.
            // Such a row is thrown on rather than dropped - a table that quietly lost a column would
            // otherwise shrink every listing the suite compares against it, on both sides at once.
            var cells = SplitCells(line);
            if (cells.Length != 6) throw new FormatException($"The index has a row of {cells.Length} cells rather than six: {line.Trim()}");

            rows.Add(new DocsIndexRow(group, match.Groups["slug"].Value, cells[1], cells[2], Cell(cells[3]), cells[4], Cell(cells[5])));
        }

        return [.. rows];
    }

    /// <summary>
    /// A row's cells, split on the pipes that are column breaks rather than on every pipe. The
    /// renderer writes a pipe inside a cell as <c>\|</c>, which is one character of that cell's
    /// text; splitting on the raw character would read such a row as a column too long and throw on
    /// it - reporting the corruption this exists to catch against the one row that is not corrupt.
    /// </summary>
    private static string[] SplitCells(string line)
    {
        var body = line.Trim();
        if (body.StartsWith('|')) body = body[1..];
        if (body.EndsWith('|') && body.EndsWith(@"\|", StringComparison.Ordinal) is false) body = body[..^1];

        var cells = new List<string>();
        var cell = new StringBuilder();

        for (var i = 0; i < body.Length; i++)
        {
            if (body[i] == '\\' && i + 1 < body.Length && body[i + 1] == '|')
            {
                cell.Append('|');
                i++;
            }
            else if (body[i] == '|')
            {
                cells.Add(cell.ToString().Trim());
                cell.Clear();
            }
            else cell.Append(body[i]);
        }

        cells.Add(cell.ToString().Trim());

        return [.. cells];
    }

    /// <summary>A list cell: comma-separated, or "-" when the row has none of that thing.</summary>
    private static string[] Cell(string text)
        => text is "-" or "" ? [] : [.. text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    [GeneratedRegex(@"^\|\s*`(?<slug>[^`]+)`\s*\|")]
    private static partial Regex RowRegex();
}
