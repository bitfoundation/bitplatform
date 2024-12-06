using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bit.Besql;

public class BesqlDbContextInterceptor(IBesqlStorage storage) : IDbCommandInterceptor, ISingletonInterceptor
{
    public async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (IsTargetedCommand(command.CommandText))
        {
            _ = Sync(eventData.Context!.Database.GetDbConnection().DataSource).ConfigureAwait(false);
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
            _ = Sync(eventData.Context!.Database.GetDbConnection().DataSource).ConfigureAwait(false);
        }

        return result;
    }

    public async ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        if (IsTargetedCommand(command.CommandText))
        {
            _ = Sync(eventData.Context!.Database.GetDbConnection().DataSource).ConfigureAwait(false);
        }
        return result;
    }

    private bool IsTargetedCommand(string sql)
    {
        var keywords = new[] { "INSERT", "UPDATE", "DELETE", "CREATE", "ALTER", "DROP" };
        return keywords.Any(k => sql.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private async Task Sync(string dataSource)
    {
        var fileName = dataSource.Trim('/');
        await Task.Yield();
        await storage.Persist(fileName).ConfigureAwait(false);
    }
}
