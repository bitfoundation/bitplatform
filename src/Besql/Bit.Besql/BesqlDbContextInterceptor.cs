using System.Data.Common;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bit.Besql;

public class BesqlDbContextInterceptor(IBesqlStorage storage) : IDbCommandInterceptor, ISingletonInterceptor
{
    private readonly string[] keywords = ["INSERT", "UPDATE", "DELETE", "CREATE", "ALTER", "DROP"];

    public DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        if (IsTargetedCommand(command.CommandText))
        {
            _ = ThrottledSync(eventData.Context!.Database.GetDbConnection().DataSource).ConfigureAwait(false);
        }

        return result;
    }

    public async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken)
    {
        if (IsTargetedCommand(command.CommandText))
        {
            _ = ThrottledSync(eventData.Context!.Database.GetDbConnection().DataSource).ConfigureAwait(false);
        }

        return result;
    }

    public int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        if (IsTargetedCommand(command.CommandText))
        {
            _ = ThrottledSync(eventData.Context!.Database.GetDbConnection().DataSource).ConfigureAwait(false);
        }

        return result;
    }

    public async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken)
    {
        if (IsTargetedCommand(command.CommandText))
        {
            _ = ThrottledSync(eventData.Context!.Database.GetDbConnection().DataSource).ConfigureAwait(false);
        }

        return result;
    }

    public object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        if (IsTargetedCommand(command.CommandText))
        {
            _ = ThrottledSync(eventData.Context!.Database.GetDbConnection().DataSource).ConfigureAwait(false);
        }
        return result;
    }

    public async ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken)
    {
        if (IsTargetedCommand(command.CommandText))
        {
            _ = ThrottledSync(eventData.Context!.Database.GetDbConnection().DataSource).ConfigureAwait(false);
        }
        return result;
    }

    protected virtual bool IsTargetedCommand(string sql)
    {
        return keywords.Any(k => sql.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private readonly ConcurrentDictionary<string, Guid?> filesSyncIds = [];
    private async Task ThrottledSync(string dataSource)
    {
        var fileName = dataSource.Trim('/');

        var localLastSyncId = filesSyncIds[fileName] = Guid.NewGuid();

        await Task.Delay(50).ConfigureAwait(false);

        if (localLastSyncId != filesSyncIds[fileName])
            return;

        await storage.Persist(fileName).ConfigureAwait(false);
    }
}
