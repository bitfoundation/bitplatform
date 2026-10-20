using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Bit.Bsqlite.Data;

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

    /// <summary>Runs every command and returns the sum of their affected rows.</summary>
    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default)
    {
        var results = await ExecuteCoreAsync(BsqliteResultKind.NonQuery, rowLimit: null, cancellationToken)
            .ConfigureAwait(false);

        var total = 0;
        for (var i = 0; i < results.Count; i++)
        {
            total += results[i].RecordsAffected;
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
                return results[i].Rows[0][0];
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

        var results = await _connection.ExecuteBatchAsync(requests, cancellationToken).ConfigureAwait(false);
        if (results.Count != requests.Length)
        {
            throw new InvalidOperationException(
                $"The transport returned {results.Count} result(s) for {requests.Length} command(s).");
        }

        return results;
    }
}

/// <summary>One command inside a <see cref="BsqliteBatch"/>.</summary>
public sealed class BsqliteBatchCommand : DbBatchCommand
{
    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;

    public override CommandType CommandType
    {
        get => CommandType.Text;
        set
        {
            if (value != CommandType.Text)
            {
                throw new ArgumentException(
                    $"SQLite has no stored procedures or table-direct commands; only {nameof(System.Data.CommandType.Text)} is supported.",
                    nameof(value));
            }
        }
    }

    private int _recordsAffected;

    public override int RecordsAffected => _recordsAffected;

    internal void SetRecordsAffected(int value) => _recordsAffected = value;

    /// <summary>The parameters, typed.</summary>
    public new BsqliteParameterCollection Parameters { get; } = [];

    protected override DbParameterCollection DbParameterCollection => Parameters;

    public override bool CanCreateParameter => true;

    public override DbParameter CreateParameter() => new BsqliteParameter();

    internal BsqliteCommandRequest ToRequest(BsqliteResultKind resultKind, int? rowLimit)
    {
        var parameters = Parameters.Count == 0 ? [] : new BsqliteParameterValue[Parameters.Count];
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = Parameters[i];
            parameters[i] = new BsqliteParameterValue(
                parameter.ParameterName,
                BsqliteParameterBinding.ToTransportValue(parameter.Value));
        }

        return new BsqliteCommandRequest
        {
            CommandText = CommandText,
            Parameters = parameters,
            ResultKind = resultKind,
            RowLimit = rowLimit,
        };
    }
}

/// <summary>The commands of a <see cref="BsqliteBatch"/>.</summary>
public sealed class BsqliteBatchCommandCollection : DbBatchCommandCollection
{
    private readonly List<BsqliteBatchCommand> _commands = [];

    public override int Count => _commands.Count;

    public override bool IsReadOnly => false;

    public new BsqliteBatchCommand this[int index]
    {
        get => _commands[index];
        set => _commands[index] = value;
    }

    public override void Add(DbBatchCommand item) => _commands.Add(Cast(item));

    public override void Clear() => _commands.Clear();

    public override bool Contains(DbBatchCommand item) => _commands.Contains(Cast(item));

    public override void CopyTo(DbBatchCommand[] array, int arrayIndex) => ((ICollection<DbBatchCommand>)_commands.ToList<DbBatchCommand>()).CopyTo(array, arrayIndex);

    public override IEnumerator<DbBatchCommand> GetEnumerator() => _commands.GetEnumerator();

    public override int IndexOf(DbBatchCommand item) => _commands.IndexOf(Cast(item));

    public override void Insert(int index, DbBatchCommand item) => _commands.Insert(index, Cast(item));

    public override bool Remove(DbBatchCommand item) => _commands.Remove(Cast(item));

    public override void RemoveAt(int index) => _commands.RemoveAt(index);

    protected override DbBatchCommand GetBatchCommand(int index) => _commands[index];

    protected override void SetBatchCommand(int index, DbBatchCommand batchCommand)
        => _commands[index] = Cast(batchCommand);

    private static BsqliteBatchCommand Cast(DbBatchCommand command)
        => command as BsqliteBatchCommand
            ?? throw new InvalidCastException(
                $"Expected {nameof(BsqliteBatchCommand)} but received {command?.GetType().Name ?? "null"}.");
}
