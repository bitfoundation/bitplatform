using Bit.Bsqlite.Data;
using Bit.Bsqlite.Demo.Server.Dtos;

namespace Bit.Bsqlite.Demo.Server.Services;

/// <summary>
/// Puts SQL to the library's own static rules - the feature guards that refuse what a backend cannot
/// honour, and the table scan live queries are keyed by - so "will this statement run on IndexedDB, and
/// which tables will a live query over it watch?" is answered by the code that decides it, before the
/// statement ever reaches a browser.
/// <para>
/// Nothing is executed. <see cref="BsqliteFeatureGuards"/> and <see cref="BsqliteTableNames"/> are
/// pattern matches over the text, public and static, and they are the very calls the command layer
/// makes before a statement leaves for the worker; there is no engine on this server for the SQL to
/// reach, and none is opened for it.
/// </para>
/// </summary>
public static class BsqliteSqlCheck
{
    /// <summary>
    /// The longest SQL checked. A statement or a short script is what a question is about; and the
    /// patterns are run over the whole text once per backend, so an unbounded input is unbounded work.
    /// </summary>
    public const int MaxSqlLength = 10_000;

    public static BsqliteSqlCheckDto Check(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return new BsqliteSqlCheckDto
            {
                Statements = [.. BsqliteStorageCatalog.GuardedStatements.Select(Report)],
                Notes =
                [
                    "No SQL was passed, so these are the statements the guards exist for, judged on every first-party backend. " +
                    "Pass sql to check your own - a migration script, a PRAGMA, the text a LINQ query's ToQueryString() returns.",
                    .. GuardNotes
                ]
            };
        }

        if (sql.Length > MaxSqlLength)
        {
            // A partial check would read as a clean bill of health for the part that was not checked.
            return new BsqliteSqlCheckDto
            {
                Statements = [],
                Error = $"The SQL is {sql.Length} characters long; this tool checks up to {MaxSqlLength}. Check one statement or one migration at a time.",
                Notes = []
            };
        }

        var report = Report(sql);
        var notes = new List<string>();

        if (report.Backends.All(verdict => verdict.Accepted))
        {
            notes.Add("Every backend's guards let this through. The guards cover WAL, ATTACH and PRAGMA page_size only: whether the SQL itself is valid is the engine's to say, in the browser.");
        }
        else
        {
            notes.AddRange(GuardNotes);
        }

        notes.Add(report.Tables.Length == 0
            ? "No table names were found, so a live query over this SQL would have nothing to watch - BsqliteLiveQuery treats an empty table list as \"every write\"."
            : $"A live query over this SQL re-runs when {string.Join(", ", report.Tables.Select(table => $"'{table}'"))} is written - by this tab or another one. AsLiveQuery() reads the tables the same way, out of ToQueryString().");

        if (report.LooksLikeWrite)
        {
            notes.Add("It writes. The worker reports what a write really changed from SQLite's update hook, triggers and cascades included, and only once the transaction commits; the table scan above is the fallback for a transport that cannot.");
        }

        if (report.LooksLikeSchemaChange)
        {
            notes.Add("It contains DDL, which the update hook does not see: the statement text is what notifies live queries of it, and it clears the worker's prepared-statement cache.");
        }

        return new BsqliteSqlCheckDto { Statements = [report], Notes = [.. notes] };
    }

    private static readonly string[] GuardNotes =
    [
        "WAL is refused everywhere - no web VFS has the shared memory it needs; journaling is DELETE or TRUNCATE.",
        "ATTACH is refused where the backend declares SupportsMultiDatabaseTransactions = false: open each database on its own session instead.",
        "PRAGMA page_size is refused where the backend declares CanChangePageSize = false: its page size is fixed to its block size.",
        "The command layer refuses before the statement leaves for the worker, and the worker refuses again, so a JavaScript caller cannot bypass the capability declaration either. OnOpen statements run outside the guards, and still cannot enable WAL.",
    ];

    private static BsqliteSqlReportDto Report(string sql) => new()
    {
        Sql = sql,
        Backends = [.. BsqliteStorageCatalog.Providers.Select(provider =>
            BsqliteStorageCatalog.Judge(provider.Name, sql, BsqliteStorageCatalog.LimitsOf(BsqliteStorageCatalog.Instance(provider.Name)!.Capabilities)))],
        Tables = [.. BsqliteTableNames.Extract(sql).Order(StringComparer.OrdinalIgnoreCase)],
        LooksLikeWrite = BsqliteTableNames.LooksLikeWrite(sql),
        LooksLikeSchemaChange = BsqliteTableNames.LooksLikeSchemaChange(sql)
    };
}
