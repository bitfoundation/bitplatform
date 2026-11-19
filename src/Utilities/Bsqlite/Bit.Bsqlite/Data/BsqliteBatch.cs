using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Bit.Bsqlite;

/// <summary>
/// Several commands sent to the worker in one round trip, each with its own parameters.
/// </summary>
/// <remarks>
/// In the browser the cost that dominates is crossing the interop boundary, not executing SQL, so
/// a batch of twenty inserts costs about what one insert does. <c>ExecuteReaderAsync</c>
/// returns a reader over every command's result set, advanced with <c>NextResultAsync</c>.
/// </remarks>
public sealed class BsqliteBatch : DbBatch
{
    private BsqliteConnection? _connection;

    public BsqliteBatch(BsqliteConnection connection)
        => _connection = connection;

    public BsqliteBatch()
    {
    }

    /// <summary>The commands, typed.</summary>
    public new BsqliteBatchCommandCollection BatchCommands { get; } = [];

    protected override DbBatchCommandCollection DbBatchCommands => BatchCommands;

    /// <inheritdoc cref="BsqliteCommand.CommandTimeout"/>
    public override int Timeout { get; set; } = 30;

    protected override DbConnection? DbConnection
    {
        get => _connection;
        set => _connection = (BsqliteConnection?)value;
    }

    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel()
    {
    }

    public override void Prepare()
    {
    }

    public override Task PrepareAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    protected override DbBatchCommand CreateDbBatchCommand() => new BsqliteBatchCommand();

    public override int ExecuteNonQuery()
        => throw BsqliteSynchronousApiNotSupportedException.ForMember(
            nameof(ExecuteNonQuery), nameof(ExecuteNonQueryAsync));

    public override object? ExecuteScalar()
        => throw BsqliteSynchronousApiNotSupportedException.ForMember(
            nameof(ExecuteScalar), nameof(ExecuteScalarAsync));

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        => throw BsqliteSynchronousApiNotSupportedException.ForMember(
            nameof(ExecuteReader), nameof(ExecuteReaderAsync));

    /// <summary>
    /// Runs every command and returns the sum of their affected rows - -1 when every command was a query, which
    /// affects none - as ADO.NET adds them up.
    /// </summary>
    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default)
    {
        var results = await ExecuteCoreAsync(BsqliteResultKind.NonQuery, rowLimit: null, cancellationToken)
            .ConfigureAwait(false);

        var total = -1;
        for (var i = 0; i < results.Count; i++)
        {
            if (results[i].RecordsAffected >= 0)
            {
                total = Math.Max(total, 0) + results[i].RecordsAffected;
            }

            BatchCommands[i].SetRecordsAffected(results[i].RecordsAffected);
        }

        return total;
    }

    /// <summary>Runs every command and returns the first column of the first row of the first command that produced one.</summary>
    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default)
    {
        var results = await ExecuteCoreAsync(BsqliteResultKind.Reader, rowLimit: 1, cancellationToken)
            .ConfigureAwait(false);

        for (var i = 0; i < results.Count; i++)
        {
            BatchCommands[i].SetRecordsAffected(results[i].RecordsAffected);
            if (results[i].Rows.Count > 0 && results[i].Rows[0].Length > 0)
            {
                return results[i].Rows[0][0] ?? DBNull.Value;
            }
        }

        return null;
    }

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
        CommandBehavior behavior,
        CancellationToken cancellationToken)
    {
        int? rowLimit = behavior.HasFlag(CommandBehavior.SchemaOnly)
            ? 0
            : behavior.HasFlag(CommandBehavior.SingleRow) ? 1 : null;

        var results = await ExecuteCoreAsync(BsqliteResultKind.Reader, rowLimit, cancellationToken)
            .ConfigureAwait(false);

        for (var i = 0; i < results.Count; i++)
        {
            BatchCommands[i].SetRecordsAffected(results[i].RecordsAffected);
        }

        return new BsqliteDataReader(results, behavior.HasFlag(CommandBehavior.CloseConnection) ? _connection : null);
    }

    private async Task<IReadOnlyList<BsqliteCommandResult>> ExecuteCoreAsync(
        BsqliteResultKind resultKind,
        int? rowLimit,
        CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("The batch requires an open connection.");
        }

        var requests = new BsqliteCommandRequest[BatchCommands.Count];
        for (var i = 0; i < requests.Length; i++)
        {
            requests[i] = BatchCommands[i].ToRequest(resultKind, rowLimit);
        }

        var results = await _connection
            .ExecuteBatchAsync(requests, DbTransaction, explicitEnlistment: false, Timeout, cancellationToken)
            .ConfigureAwait(false);
        if (results.Count != requests.Length)
        {
            throw new InvalidOperationException(
                $"The transport returned {results.Count} result(s) for {requests.Length} command(s).");
        }

        return results;
    }
}
