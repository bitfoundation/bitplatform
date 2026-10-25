using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bit.Belemetry.EntityFrameworkCore;

/// <summary>
/// Starts a client span on the <see cref="SourceName"/> source around every command Entity Framework Core executes,
/// with the database semantic conventions (<c>db.system.name</c>, <c>db.namespace</c>, <c>server.address</c>,
/// <c>server.port</c>, <c>error.type</c>), so the Belemetry activity collector - and any other
/// <see cref="ActivityListener"/> - records it. Registered by <c>UseBitBelemetry()</c> on the context options.
/// </summary>
/// <remarks>
/// EF Core neither starts activities of its own nor lets an interceptor carry state from the executing to the
/// executed callback, so the span is kept by the command's correlation id (<c>CommandEventData.CommandId</c>,
/// the same for the executing, executed, failed and canceled callbacks of one execution) rather than looked up as
/// <see cref="Activity.Current"/>: with more than one interceptor registered, EF Core calls them through an async
/// composite whose state machine restores the execution context when its synchronous part returns, so the span
/// started in the executing callback is no longer current in the executed one. For the same reason the span is only
/// current during the ADO.NET call when it is the sole interceptor; otherwise a provider's own child spans parent to
/// the request. The span covers the execution, not the reading of a result set, which is what the OpenTelemetry EF
/// Core instrumentation measures too. Nothing is allocated while no listener wants the source:
/// <see cref="ActivitySource.StartActivity(string, ActivityKind)"/> returns null first.
/// </remarks>
public sealed class BelemetryDbCommandInterceptor : DbCommandInterceptor
{
    /// <summary>The <see cref="ActivitySource"/> name; the core collects every <c>Bit.Belemetry.*</c> source by default.</summary>
    public const string SourceName = "Bit.Belemetry.EntityFrameworkCore";

    private const int MaxCachedServers = 64;

    private static readonly ActivitySource _source = new(SourceName, typeof(BelemetryDbCommandInterceptor).Assembly.GetName().Version?.ToString());

    // Per-command work that would otherwise allocate on every command: the db.system.name of a connection type, the
    // name of a CommandSource value, and the host and (boxed) port parsed from a DataSource. An app has a handful of
    // each; the servers cache is capped so a tenant-per-server app cannot grow it without bound.
    private static readonly ConcurrentDictionary<Type, string> _systems = new();
    private static readonly ConcurrentDictionary<CommandSource, string> _sourceNames = new();
    private static readonly ConcurrentDictionary<string, (string? Host, object? Port)> _servers = new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<Guid, Activity> _active = new();
    private readonly BelemetryEntityFrameworkCoreOptions _options;

    /// <summary>Creates the interceptor.</summary>
    public BelemetryDbCommandInterceptor(BelemetryEntityFrameworkCoreOptions? options = null)
    {
        _options = options ?? new BelemetryEntityFrameworkCoreOptions();
    }

    /// <summary>Whether something is listening to the source (settable in tests to assert the no-listener path).</summary>
    internal static bool HasListeners => _source.HasListeners();

    /// <inheritdoc />
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Start(command, eventData);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        Start(command, eventData);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        Start(command, eventData);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        Start(command, eventData);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        Start(command, eventData);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Start(command, eventData);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Stop(eventData, null);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        Stop(eventData, null);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        Stop(eventData, null);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        Stop(eventData, null);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Stop(eventData, null);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Stop(eventData, null);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData) => Stop(eventData, eventData.Exception);

    /// <inheritdoc />
    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Stop(eventData, eventData.Exception);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override void CommandCanceled(DbCommand command, CommandEndEventData eventData) => Stop(eventData, null, canceled: true);

    /// <inheritdoc />
    public override Task CommandCanceledAsync(DbCommand command, CommandEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Stop(eventData, null, canceled: true);
        return Task.CompletedTask;
    }

    private void Start(DbCommand command, CommandEventData eventData)
    {
        if (_source.HasListeners() is false) return;

        Activity? activity = null;
        try
        {
            if (_options.Filter is { } filter && filter(command, eventData) is false) return;

            var connection = command.Connection;
            var system = connection is null ? "other_sql" : _systems.GetOrAdd(connection.GetType(), static t => GetDbSystem(t));
            var database = connection?.Database;

            // Named after the database, as the OpenTelemetry EF Core instrumentation names its spans: the statement
            // would be high-cardinality, and the semantic conventions ask for a low-cardinality summary.
            activity = _source.StartActivity(string.IsNullOrEmpty(database) ? system : database!, ActivityKind.Client);
            if (activity is null) return;

            // Stored before anything that could throw, so a failure below still ends it in the executed callback.
            _active[eventData.CommandId] = activity;

            activity.SetTag(BelemetrySemanticConventions.DbSystemName, system);
            if (string.IsNullOrEmpty(database) is false) activity.SetTag("db.namespace", database);
            activity.SetTag("efcore.command.source", _sourceNames.GetOrAdd(eventData.CommandSource, static s => s.ToString()));

            if (activity.IsAllDataRequested)
            {
                if (system is not "sqlite" && connection?.DataSource is { Length: > 0 } dataSource)
                {
                    var (host, port) = GetServer(dataSource);
                    if (host is not null) activity.SetTag(BelemetrySemanticConventions.ServerAddress, host);
                    if (port is not null) activity.SetTag(BelemetrySemanticConventions.ServerPort, port);
                }

                if (_options.CaptureQueryText) activity.SetTag(BelemetrySemanticConventions.DbQueryText, command.CommandText);
            }
        }
        catch
        {
            // Telemetry must never fail the query - nor leave a span current on the thread.
            if (activity is not null && _active.TryRemove(eventData.CommandId, out _)) activity.Stop();
        }
    }

    private void Stop(CommandEventData eventData, Exception? exception, bool canceled = false)
    {
        if (_active.IsEmpty || _active.TryRemove(eventData.CommandId, out var activity) is false) return;

        try
        {
            if (exception is not null)
            {
                activity.SetTag(BelemetrySemanticConventions.ErrorType, exception.GetType().FullName);
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            else if (canceled)
            {
                activity.SetTag(BelemetrySemanticConventions.ErrorType, typeof(OperationCanceledException).FullName);
                activity.SetStatus(ActivityStatusCode.Error, "canceled");
            }
        }
        finally
        {
            // Stop restores Activity.Current to the parent only when this span is current (the sole-interceptor case).
            activity.Stop();
        }
    }

    private static (string? Host, object? Port) GetServer(string dataSource)
    {
        if (_servers.TryGetValue(dataSource, out var cached)) return cached;

        var (host, port) = ParseServer(dataSource);
        var entry = (host, port is null ? null : (object)port.Value);
        if (_servers.Count < MaxCachedServers) _servers.TryAdd(dataSource, entry);
        return entry;
    }

    /// <summary>
    /// The <c>db.system.name</c> value for the ADO.NET provider behind a connection - decided by the connection's
    /// type, which exists for every command, rather than by the context's provider name, which does not.
    /// </summary>
    internal static string GetDbSystem(DbConnection? connection) => connection is null ? "other_sql" : GetDbSystem(connection.GetType());

    private static string GetDbSystem(Type connectionType)
    {
        var ns = connectionType.Namespace ?? string.Empty;
        if (ns.StartsWith("Microsoft.Data.SqlClient", StringComparison.Ordinal) || ns.StartsWith("System.Data.SqlClient", StringComparison.Ordinal)) return "microsoft.sql_server";
        if (ns.StartsWith("Npgsql", StringComparison.Ordinal)) return "postgresql";
        if (ns.StartsWith("Microsoft.Data.Sqlite", StringComparison.Ordinal) || ns.StartsWith("System.Data.SQLite", StringComparison.Ordinal)) return "sqlite";
        if (ns.StartsWith("MySqlConnector", StringComparison.Ordinal) || ns.StartsWith("MySql.Data", StringComparison.Ordinal)) return "mysql";
        if (ns.StartsWith("Oracle.ManagedDataAccess", StringComparison.Ordinal)) return "oracle.db";
        if (ns.StartsWith("FirebirdSql", StringComparison.Ordinal)) return "firebirdsql";
        if (ns.StartsWith("IBM.Data", StringComparison.Ordinal)) return "ibm.db2";
        if (ns.StartsWith("Snowflake", StringComparison.Ordinal)) return "snowflake";
        return ns.Length == 0 ? "other_sql" : ns.Split('.')[0].ToLowerInvariant();
    }

    /// <summary>
    /// The host and port of an ADO.NET <c>DataSource</c>: <c>tcp:host,1433</c> (SQL Server), <c>host:5432</c>,
    /// <c>host</c>, or a named-pipe or file reference, which has no host.
    /// </summary>
    internal static (string? Host, int? Port) ParseServer(string dataSource)
    {
        var value = dataSource.AsSpan().Trim();
        if (value.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase)) value = value[4..];
        if (value.StartsWith("//", StringComparison.Ordinal)) value = value[2..];
        if (value.IsEmpty || value.StartsWith("np:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("\\\\", StringComparison.Ordinal) || value.Contains('/')) return (null, null);

        var separator = value.LastIndexOfAny(',', ':');
        if (separator > 0 && int.TryParse(value[(separator + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var port))
        {
            return (value[..separator].ToString(), port);
        }

        // "host\instance" (SQL Server): the instance is not part of the host.
        var backslash = value.IndexOf('\\');
        if (backslash > 0) value = value[..backslash];
        return (value.ToString(), null);
    }
}
