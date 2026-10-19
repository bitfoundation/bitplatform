using System.Text.RegularExpressions;

namespace Bit.Bsqlite.Data;

/// <summary>
/// Pulls table names out of SQL so a live query can re-run when any of them change.
/// </summary>
/// <remarks>
/// <para>
/// Table-level and deliberately coarse: comments and quoted identifiers that contain SQL keywords
/// can produce extra names, which only causes extra re-queries, never a missed update.
/// </para>
/// <para>
/// This is the fallback. When the transport reports what the engine actually wrote - the worker
/// does, through <c>sqlite3_update_hook</c> - the connection uses that instead, which also covers
/// triggers and cascading deletes that no regular expression can see.
/// </para>
/// <para>
/// The worker carries the same two patterns in JavaScript (<c>bit-bsqlite-worker.js</c>) for
/// the tables its update hook cannot see - DDL, and the truncate-optimised <c>DELETE FROM t</c> -
/// so a change here is a change there.
/// </para>
/// </remarks>
public static partial class BsqliteTableNames
{
    /// <summary>
    /// The tables <paramref name="sql"/> reads or writes. Accepts the identifier quoting SQLite
    /// does (<c>"t"</c>, <c>`t`</c>, <c>[t]</c>), a schema prefix (<c>main."t"</c>), the
    /// conflict clause an <c>UPDATE</c> may carry (<c>UPDATE OR REPLACE t</c>), and the objects
    /// DDL names (<c>CREATE VIEW v</c>, <c>CREATE TEMP TABLE t</c>, <c>CREATE INDEX i ON t</c>).
    /// </summary>
    public static IReadOnlySet<string> Extract(string? sql)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(sql))
        {
            return names;
        }

        foreach (Match match in TableName().Matches(sql))
        {
            var name = match.Groups[1].Value;
            if (!name.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase))
            {
                names.Add(name);
            }
        }

        return names;
    }

    /// <summary>
    /// Whether <paramref name="sql"/> contains a statement that writes.
    /// </summary>
    /// <remarks>
    /// Every statement is considered, not just the first. A batch is routinely
    /// <c>BEGIN; INSERT …; COMMIT;</c> or several statements EF sent together, and looking only at
    /// the leading keyword would call all of those reads - which is a missed live-query update, the
    /// one failure mode this heuristic is not allowed to have. A leading common table expression
    /// is skipped for the same reason: <c>WITH … INSERT INTO</c> is a write. So are leading
    /// comments, which migrations and hand-written scripts both carry.
    /// </remarks>
    public static bool LooksLikeWrite(string? sql)
        => !string.IsNullOrWhiteSpace(sql) && Write().IsMatch(sql);

    /// <summary>
    /// Whether <paramref name="sql"/> contains DDL - <c>CREATE</c>, <c>DROP</c>, or <c>ALTER</c> - which
    /// changes what a query can see without changing a row the update hook would report.
    /// </summary>
    public static bool LooksLikeSchemaChange(string? sql)
        => !string.IsNullOrWhiteSpace(sql) && SchemaChange().IsMatch(sql);

    [GeneratedRegex(
        @"\b(?:from|join|into|update(?:\s+or\s+(?:rollback|abort|replace|fail|ignore))?|table|view|trigger"
        + @"|index\s+(?:if\s+(?:not\s+)?exists\s+)?[\w""`\[\]]+\s+on)\s+"
        + @"(?:if\s+(?:not\s+)?exists\s+)?(?:(?:temp|temporary)\s+)?"
        + @"(?:[""`\[]?[A-Za-z_]\w*[""`\]]?\s*\.\s*)?"
        + @"[""`\[]?([A-Za-z_]\w*)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TableName();

    // A block comment is spelled out (/\*[^*]*\*+(?:[^/*][^*]*\*+)*/) rather than matched lazily
    // (/\*.*?\*/): a lazy body can end at any */, so a run of */ /* that never reaches a write
    // backtracks through every way of splitting it - exponential in its length.
    [GeneratedRegex(
        @"(?:^|;)\s*(?:--[^\n]*\n\s*|/\*[^*]*\*+(?:[^/*][^*]*\*+)*/\s*)*(?:with\b.*?)?\b(?:insert|update|delete|replace|create|drop|alter)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex Write();

    [GeneratedRegex(
        @"(?:^|;)\s*(?:--[^\n]*\n\s*|/\*[^*]*\*+(?:[^/*][^*]*\*+)*/\s*)*(?:create|drop|alter)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex SchemaChange();
}
