using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>
/// A database command Entity Framework Core reported executing (or failing): its SQL as EF logged it - parameters by
/// name, their values only where the app enabled sensitive data logging - and how long it took.
/// </summary>
public sealed record BevDbCommand(string? CommandText, double? ElapsedMs, bool Failed);

/// <summary>
/// Entity Framework Core's commands, read from what it logs - no reference to EF, no interceptor: the category
/// <c>Microsoft.EntityFrameworkCore.Database.Command</c> at Information, events 20101 (executed) and 20102 (failed),
/// whose values carry the SQL and the elapsed milliseconds. An app whose logging filters that category out has none.
/// </summary>
public static class BevDbCommands
{
    public const string Category = "Microsoft.EntityFrameworkCore.Database.Command";

    public const int ExecutedEventId = 20101;

    public const int FailedEventId = 20102;

    internal static bool IsCommand(string category, int eventId) =>
        eventId is ExecutedEventId or FailedEventId && string.Equals(category, Category, StringComparison.Ordinal);

    /// <summary>
    /// Reads the command from the entry's own values (before the inspector formats them): EF writes <c>elapsed</c> as a
    /// formatted number ("1,234") and <c>commandText</c> as the SQL. The SQL is an application value - a LINQ constant can
    /// be inlined into it - so it is kept at Full and Shallow access, redacted, and left out under None.
    /// </summary>
    internal static BevDbCommand Read(int eventId, IReadOnlyList<KeyValuePair<string, object?>> pairs, BevValueAccess access)
    {
        string? text = null;
        double? elapsed = null;
        for (var i = 0; i < pairs.Count && i < 32; i++)
        {
            var (name, value) = pairs[i];
            if (name == "commandText" && value is string sql && access != BevValueAccess.None)
            {
                sql = BevRedactor.RedactText(sql.Trim());
                text = sql.Length <= 4000 ? sql : sql[..3999] + "…";
            }
            else if (name == "elapsed" && value is not null)
            {
                elapsed = ParseMs(Convert.ToString(value, CultureInfo.InvariantCulture));
            }
        }

        return new BevDbCommand(text, elapsed, eventId == FailedEventId);
    }

    // EF formats with the current culture's group separator ("1,234" or "1.234"): only digits count, and an
    // elapsed time is whole milliseconds.
    private static double? ParseMs(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var digits = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (char.IsAsciiDigit(c))
            {
                digits.Append(c);
            }
        }

        return digits.Length is > 0 and <= 12 ? double.Parse(digits.ToString(), CultureInfo.InvariantCulture) : null;
    }

    /// <summary>The SQL with its whitespace collapsed: what makes two executions the same command.</summary>
    public static string Normalize(string sql)
    {
        var sb = new StringBuilder(sql.Length);
        var space = false;
        foreach (var c in sql)
        {
            if (char.IsWhiteSpace(c))
            {
                space = sb.Length > 0;
                continue;
            }

            if (space)
            {
                sb.Append(' ');
                space = false;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>The commands among the log entries, grouped by their SQL, busiest first.</summary>
    internal static List<BevDbCommandSummary> Summarize(IEnumerable<BevLogRecord> logs) =>
        logs.Where(l => l.DbCommand is not null)
            .GroupBy(l => l.DbCommand!.CommandText is { } sql ? Normalize(sql) : "", StringComparer.Ordinal)
            .Select(g =>
            {
                var times = g.Where(l => l.DbCommand!.ElapsedMs is not null).Select(l => l.DbCommand!.ElapsedMs!.Value).Order().ToArray();
                return new BevDbCommandSummary(
                    g.Key,
                    g.Count(),
                    g.Count(l => l.DbCommand!.Failed),
                    times.Sum(),
                    times.Length == 0 ? null : BevStats.Percentile(times, 95),
                    times.Length == 0 ? null : times[^1],
                    g.Max(l => l.At),
                    g.Where(l => l.CallerComponent is not null).GroupBy(l => l.CallerComponent!, StringComparer.Ordinal)
                        .OrderByDescending(c => c.Count()).Take(3).Select(c => (c.Key, c.Count())).ToList(),
                    g.Select(l => l.Id).ToList());
            })
            .OrderByDescending(s => s.Executions).ThenByDescending(s => s.TotalMs)
            .ToList();
}

/// <summary>One SQL command's executions in the log buffer.</summary>
internal sealed record BevDbCommandSummary(
    string CommandText,
    int Executions,
    int Failed,
    double TotalMs,
    double? P95Ms,
    double? MaxMs,
    DateTimeOffset LastAt,
    IReadOnlyList<(string Component, int Executions)> Senders,
    IReadOnlyList<long> LogIds);
