using System.Text;
using System.Reflection;

namespace Bit.Belemetry.Demo.Server.Services;

/// <summary>
/// The reference tables - feature bits, storage keys, the semantic conventions the browser and .NET emit, the
/// Azure Monitor mapping, environment variables, browser options - read out of the embedded README and the
/// contracts in CLAUDE.md, plus the attribute-name constants of <see cref="BelemetrySemanticConventions"/>
/// reflected from the assembly.
/// <para>
/// Every table in those two documents is indexed, so a table added to either of them is searchable here
/// without a change to this class; and nothing here restates a row, so a lookup cannot disagree with them.
/// </para>
/// </summary>
public static class BelemetryConventions
{
    private static readonly Lazy<ConventionTable[]> _tables = new(Load);

    /// <summary>Every table, in document order: the README's first, then the contracts'.</summary>
    public static ConventionTable[] Tables => _tables.Value;

    /// <summary>The attribute-name constants of <see cref="BelemetrySemanticConventions"/>, as (constant, value).</summary>
    public static (string Name, string Value)[] Constants { get; } =
    [
        .. typeof(BelemetrySemanticConventions)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (f.Name, (string)f.GetRawConstantValue()!))
    ];

    /// <summary>
    /// The rows that match every word of <paramref name="query"/> (any cell, the table's section, or its headers),
    /// grouped under their table, plus the matching constants, as markdown. An empty query lists the tables.
    /// </summary>
    public static string Lookup(string? query, int maxRows = 60)
    {
        var builder = new StringBuilder();
        var words = (query ?? string.Empty).Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (words.Length == 0)
        {
            builder.AppendLine("# Belemetry reference tables").AppendLine();
            builder.AppendLine("Pass a query (an attribute name, a feature, a record type, an Azure table, a storage key...) to get the matching rows. The tables:").AppendLine();

            foreach (var table in Tables)
            {
                builder.AppendLine($"- **{table.Section}** ({table.Source}): {string.Join(" | ", table.Headers)} - {table.Rows.Length} rows");
            }

            builder.AppendLine($"- **BelemetrySemanticConventions** (assembly): {Constants.Length} attribute-name constants");

            return builder.ToString();
        }

        var total = 0;

        foreach (var table in Tables)
        {
            // A query that names the table ("azure mapping", "storage keys") returns all of it.
            var tableMatches = words.All(w => table.Section.Contains(w, StringComparison.OrdinalIgnoreCase)
                                           || table.Headers.Any(h => h.Contains(w, StringComparison.OrdinalIgnoreCase)));

            var rows = table.Rows.Where(row => tableMatches || words.All(w =>
                           row.Any(cell => cell.Contains(w, StringComparison.OrdinalIgnoreCase))
                           || table.Section.Contains(w, StringComparison.OrdinalIgnoreCase)))
                         .ToArray();

            // Words that only matched the section name would return the whole table under a query about one row;
            // keep those for queries that really named the table.
            if (tableMatches is false)
            {
                rows = [.. rows.Where(row => words.Any(w => row.Any(cell => cell.Contains(w, StringComparison.OrdinalIgnoreCase))))];
            }

            if (rows.Length == 0 || total >= maxRows) continue;

            builder.AppendLine($"### {table.Section} ({table.Source})").AppendLine();
            builder.AppendLine("| " + string.Join(" | ", table.Headers) + " |");
            builder.AppendLine("|" + string.Concat(Enumerable.Repeat(" --- |", table.Headers.Length)));

            foreach (var row in rows.Take(maxRows - total))
            {
                builder.AppendLine("| " + string.Join(" | ", row) + " |");
                total++;
            }

            builder.AppendLine();
        }

        var constants = Constants.Where(c => words.All(w => c.Name.Contains(w, StringComparison.OrdinalIgnoreCase) || c.Value.Contains(w, StringComparison.OrdinalIgnoreCase))).ToArray();

        if (constants.Length > 0)
        {
            builder.AppendLine("### BelemetrySemanticConventions constants (assembly)").AppendLine();
            builder.AppendLine("| Constant | Attribute |").AppendLine("| --- | --- |");
            foreach (var (name, value) in constants) builder.AppendLine($"| `BelemetrySemanticConventions.{name}` | `{value}` |");
            builder.AppendLine();
        }

        return builder.Length == 0
            ? $"Nothing in the reference tables matches '{query}'. Call LookupBelemetryConventions with an empty query to list the tables, or SearchBelemetry for prose."
            : builder.ToString();
    }

    private static ConventionTable[] Load()
    {
        return [.. Parse(BelemetrySourceCatalog.Readme, "README"), .. Parse(BelemetrySourceCatalog.Contracts, "CLAUDE.md contracts")];
    }

    /// <summary>Every pipe table in <paramref name="markdown"/>, labelled with the heading it sits under.</summary>
    public static IEnumerable<ConventionTable> Parse(string markdown, string source)
    {
        if (string.IsNullOrEmpty(markdown)) yield break;

        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var heading = source;
        var inFence = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            if (line.StartsWith("```", StringComparison.Ordinal)) { inFence = !inFence; continue; }
            if (inFence) continue;

            if (line.StartsWith('#'))
            {
                heading = line.TrimStart('#').Trim();
                continue;
            }

            // A table is a header row followed by a delimiter row of dashes.
            if (line.StartsWith('|') is false || i + 1 >= lines.Length || IsDelimiter(lines[i + 1].Trim()) is false) continue;

            var headers = Cells(line);
            var rows = new List<string[]>();
            i += 2;

            while (i < lines.Length && lines[i].Trim().StartsWith('|'))
            {
                rows.Add(Cells(lines[i].Trim()));
                i++;
            }

            i--;

            // The first column of a header-less table (the README's "What is collected") is blank.
            yield return new(source, heading, [.. headers.Select((h, index) => h.Length == 0 && index == 0 ? "-" : h)], [.. rows]);
        }
    }

    private static bool IsDelimiter(string line)
        => line.StartsWith('|') && line.Replace("|", "", StringComparison.Ordinal).Trim().Trim('-', ':', ' ').Length == 0 && line.Contains("---", StringComparison.Ordinal);

    private static string[] Cells(string line)
    {
        var trimmed = line.Trim().Trim('|');
        var cells = new List<string>();
        var current = new StringBuilder();
        var inCode = false;

        // A pipe inside a code span (`a|b`) is part of the cell, not a column break.
        foreach (var c in trimmed)
        {
            if (c == '`') inCode = !inCode;

            if (c == '|' && inCode is false)
            {
                cells.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        cells.Add(current.ToString().Trim());

        return [.. cells];
    }
}

/// <summary>One markdown table of the reference documents.</summary>
/// <param name="Source">Which document it came from.</param>
/// <param name="Section">The heading it sits under.</param>
/// <param name="Headers">The column headers.</param>
/// <param name="Rows">The cells of every row.</param>
public sealed record ConventionTable(string Source, string Section, string[] Headers, string[][] Rows);
