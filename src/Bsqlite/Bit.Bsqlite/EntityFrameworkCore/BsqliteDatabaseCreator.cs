using Bit.Bsqlite.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bit.Bsqlite.EntityFrameworkCore;

/// <summary>
/// An <see cref="IRelationalDatabaseCreator"/> whose schema operations are genuinely asynchronous.
/// </summary>
/// <remarks>
/// <para>
/// <c>RelationalDatabaseCreator.CreateAsync</c> has no async implementation of its own - it calls
/// the synchronous <c>Create()</c>, which opens the connection synchronously. That is why both
/// <c>EnsureCreatedAsync</c> and <c>MigrateAsync</c> fail against the default tier unless this
/// service is replaced. Everything above it (<c>CreateTablesAsync</c>, the migration command
/// executor) is already async, so overriding the four abstract members is enough.
/// </para>
/// <para>
/// <c>Delete</c> empties the schema - views, then tables, then <c>VACUUM</c> - rather than asking
/// the storage provider to forget the file. The transport keeps the database open across EF's
/// per-operation close, so there is no file to unlink while a session holds it;
/// <c>BsqliteDatabase.WhileClosedAsync</c> plus the provider's admin API is the way to wipe
/// persistent storage outright.
/// </para>
/// </remarks>
public sealed class BsqliteDatabaseCreator(
    RelationalDatabaseCreatorDependencies dependencies,
    IRawSqlCommandBuilder rawSqlCommandBuilder)
    : RelationalDatabaseCreator(dependencies)
{
    // EF's own version omits the NOT LIKE clause, because its Delete() removes the database file
    // outright and so never has to reason about leftovers. Deletion here drops objects instead, and
    // SQLite's internal bookkeeping tables (sqlite_sequence, created by AUTOINCREMENT) cannot be
    // dropped - counting them would make an emptied database look populated.
    private const string CountTablesSql = """
        SELECT COUNT(*) FROM "sqlite_master"
        WHERE "type" = 'table' AND "rootpage" IS NOT NULL AND "name" NOT LIKE 'sqlite_%'
        """;

    // Views first, then tables: a view over a dropped table is harmless to SQLite but not to the
    // next EnsureCreated, which would trip over the leftover. Indexes and triggers go with their
    // table.
    private const string ListObjectsSql = """
        SELECT "type", "name" FROM "sqlite_master"
        WHERE "type" IN ('table', 'view') AND "name" NOT LIKE 'sqlite_%'
        ORDER BY CASE "type" WHEN 'view' THEN 0 ELSE 1 END, "rowid"
        """;

    /// <summary>
    /// Always true: opening is what brings a SQLite database into being, and whether any backing
    /// storage exists is the storage provider's question, not EF's.
    /// </summary>
    public override bool Exists() => true;

    public override Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public override void Create()
        => throw BsqliteSynchronousApiNotSupportedException.ForMember(
            nameof(Create), nameof(CreateAsync));

    public override async Task CreateAsync(CancellationToken cancellationToken = default)
    {
        await Dependencies.Connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await Dependencies.Connection.CloseAsync().ConfigureAwait(false);
    }

    public override bool HasTables()
        => throw BsqliteSynchronousApiNotSupportedException.ForMember(
            nameof(HasTables), nameof(HasTablesAsync));

    public override async Task<bool> HasTablesAsync(CancellationToken cancellationToken = default)
    {
        var count = await ExecuteScalarAsync(CountTablesSql, cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(count) != 0L;
    }

    public override void Delete()
        => throw BsqliteSynchronousApiNotSupportedException.ForMember(
            nameof(Delete), nameof(DeleteAsync));

    public override async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        var objects = new List<(string Type, string Name)>();

        await Dependencies.Connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var command = rawSqlCommandBuilder.Build(ListObjectsSql);
            await using var reader = await command
                .ExecuteReaderAsync(CreateParameterObject(), cancellationToken)
                .ConfigureAwait(false);

            while (await reader.DbDataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                objects.Add((reader.DbDataReader.GetString(0), reader.DbDataReader.GetString(1)));
            }
        }
        finally
        {
            await Dependencies.Connection.CloseAsync().ConfigureAwait(false);
        }

        if (objects.Count == 0)
        {
            return;
        }

        // With foreign keys on - and the worker turns them on - DROP TABLE runs an implicit DELETE
        // that a referencing table with rows can veto, and the drop order out of sqlite_master is
        // creation order, parents first. Enforcement is suspended for the drops and put back to
        // whatever it was, since the connection lives on after this.
        var foreignKeys = Convert.ToInt64(
            await ExecuteScalarAsync("PRAGMA foreign_keys", cancellationToken).ConfigureAwait(false));
        if (foreignKeys != 0L)
        {
            await ExecuteNonQueryAsync("PRAGMA foreign_keys=OFF", cancellationToken).ConfigureAwait(false);
        }

        try
        {
            foreach (var (type, name) in objects)
            {
                var keyword = type == "view" ? "VIEW" : "TABLE";
                await ExecuteNonQueryAsync($"DROP {keyword} IF EXISTS \"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"", cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            if (foreignKeys != 0L)
            {
                await ExecuteNonQueryAsync("PRAGMA foreign_keys=ON", CancellationToken.None).ConfigureAwait(false);
            }
        }

        // Returns the freed pages to the storage backend, which would otherwise keep the file at
        // its high-water mark.
        await ExecuteNonQueryAsync("VACUUM", cancellationToken).ConfigureAwait(false);
    }

    private async Task<object?> ExecuteScalarAsync(string sql, CancellationToken cancellationToken)
    {
        await Dependencies.Connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await rawSqlCommandBuilder
                .Build(sql)
                .ExecuteScalarAsync(CreateParameterObject(), cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            await Dependencies.Connection.CloseAsync().ConfigureAwait(false);
        }
    }

    private async Task ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken)
    {
        await Dependencies.Connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await rawSqlCommandBuilder
                .Build(sql)
                .ExecuteNonQueryAsync(CreateParameterObject(), cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            await Dependencies.Connection.CloseAsync().ConfigureAwait(false);
        }
    }

    private RelationalCommandParameterObject CreateParameterObject()
        => new(
            Dependencies.Connection,
            parameterValues: null,
            readerColumns: null,
            context: Dependencies.CurrentContext.Context,
            logger: Dependencies.CommandLogger);
}
