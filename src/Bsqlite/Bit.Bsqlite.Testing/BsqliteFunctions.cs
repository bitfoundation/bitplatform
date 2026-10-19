using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace Bit.Bsqlite.Testing;

/// <summary>
/// Registers the scalar functions, aggregates, and collation that EF Core's SQLite provider
/// normally installs itself.
/// </summary>
/// <remarks>
/// <para>
/// EF's <c>SqliteRelationalConnection</c> only installs these when the connection is literally a
/// <see cref="SqliteConnection"/>; against any other <c>DbConnection</c> it logs
/// <c>UnexpectedConnectionTypeWarning</c> and moves on. Since Bsqlite supplies its own
/// connection, it inherits the obligation - miss it and every <see cref="decimal"/> comparison,
/// aggregate, and ordering silently produces wrong answers.
/// </para>
/// <para>
/// This implementation is the reference for the worker-side UDF host: same names, same semantics.
/// </para>
/// </remarks>
public static class BsqliteFunctions
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    /// <summary>Installs every function EF Core's SQLite provider expects to exist.</summary>
    public static void Register(SqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        // SQLite calls regexp(pattern, input) for `input REGEXP pattern` - arguments reversed
        // relative to Regex.IsMatch. RegexOptions.NonBacktracking is deliberately *not* used: it
        // rejects lookaround and backreferences that the stock provider accepts, which S4 caught as
        // a conformance break. The timeout is the guard against catastrophic backtracking instead,
        // and it cannot change the result of a pattern that terminates.
        connection.CreateFunction<string, string, bool?>(
            "regexp",
            (pattern, input) => input is null || pattern is null
                ? null
                : Regex.IsMatch(input, pattern, RegexOptions.None, RegexTimeout),
            isDeterministic: true);

        connection.CreateFunction(
            "ef_mod",
            (decimal? dividend, decimal? divisor) => divisor == 0m ? null : dividend % divisor,
            isDeterministic: true);

        connection.CreateFunction(
            "ef_add",
            (decimal? left, decimal? right) => left + right,
            isDeterministic: true);

        connection.CreateFunction(
            "ef_divide",
            (decimal? dividend, decimal? divisor) => divisor == 0m ? null : dividend / divisor,
            isDeterministic: true);

        connection.CreateFunction(
            "ef_compare",
            (decimal? left, decimal? right) => left.HasValue && right.HasValue
                ? decimal.Compare(left.Value, right.Value)
                : default(int?),
            isDeterministic: true);

        connection.CreateFunction(
            "ef_multiply",
            (decimal? left, decimal? right) => left * right,
            isDeterministic: true);

        connection.CreateFunction(
            "ef_negate",
            (decimal? value) => -value,
            isDeterministic: true);

        connection.CreateAggregate(
            "ef_sum",
            seed: (decimal?)null,
            (decimal? sum, decimal? value) => value is null ? sum : (sum ?? 0m) + value.Value);

        connection.CreateAggregate(
            "ef_avg",
            seed: (Sum: 0m, Count: 0UL),
            ((decimal Sum, ulong Count) acc, decimal? value) => value is null
                ? acc
                : (acc.Sum + value.Value, acc.Count + 1),
            acc => acc.Count == 0 ? null : (decimal?)(acc.Sum / acc.Count));

        connection.CreateAggregate(
            "ef_max",
            seed: (decimal?)null,
            (decimal? max, decimal? value) => value is null
                ? max
                : max is null ? value : Math.Max(max.Value, value.Value));

        connection.CreateAggregate(
            "ef_min",
            seed: (decimal?)null,
            (decimal? min, decimal? value) => value is null
                ? min
                : min is null ? value : Math.Min(min.Value, value.Value));

        connection.CreateCollation(
            "EF_DECIMAL",
            (x, y) => decimal.Compare(
                decimal.Parse(x, NumberStyles.Number, CultureInfo.InvariantCulture),
                decimal.Parse(y, NumberStyles.Number, CultureInfo.InvariantCulture)));
    }
}
