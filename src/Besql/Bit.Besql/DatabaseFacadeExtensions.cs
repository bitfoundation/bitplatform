﻿using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore;

public static class DatabaseFacadeExtensions
{
    /// <summary>
    /// This method must be called before invoking Database.MigrateAsync or Database.EnsureCreatedAsync, to ensure proper functionality of bit Besql.
    /// </summary>
    public static async Task<DatabaseFacade> ConfigureSqliteJournalMode(this DatabaseFacade database)
    {
        if (database.ProviderName!.EndsWith("Sqlite", StringComparison.InvariantCulture) is false)
            throw new InvalidOperationException("It's not supposed to call this method on any database other than sqlite.");

        if (OperatingSystem.IsBrowser() is false)
            return database;

        await using var connection = new SqliteConnection(database.GetConnectionString());
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = 'delete';";
        // The SQLite's default journal mode is 'delete', but ef-core sets it to 'wal'
        // Bit.Besql only stores the .db file (not .db-wal and .db-shm files) in the browser's cache storage.
        // So the wal journal mode is not the meaningful configuration while working with Bit.Besql inside browser and WebAssembly.
        // The Blazor Hybrid and Blazor Server could use the wal journal mode though.
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);

        return database;
    }
}
