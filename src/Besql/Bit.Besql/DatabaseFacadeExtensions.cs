using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore;

public static class DatabaseFacadeExtensions
{
    /// <summary>
    /// This method must be called before invoking Database.MigrateAsync or Database.EnsureCreatedAsync, to ensure proper functionality of bit Besql.
    /// </summary>
    public static async Task<DatabaseFacade> ConfigureSqliteSynchronous(this DatabaseFacade database)
    {
        if (database.ProviderName!.EndsWith("Sqlite", StringComparison.InvariantCulture) is false)
            throw new InvalidOperationException("It's not supposed to call this method on any database other than sqlite.");

        if (OperatingSystem.IsBrowser() is false)
            return database;

        await using var connection = new SqliteConnection(database.GetConnectionString());
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA synchronous = FULL;";
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);

        return database;
    }
}
