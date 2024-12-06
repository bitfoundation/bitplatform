#if NET9_0_OR_GREATER
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Sqlite.Migrations.Internal;

namespace Bit.Besql;

// https://github.com/dotnet/efcore/issues/33731
public class BesqlHistoryRepository(HistoryRepositoryDependencies dependencies) : SqliteHistoryRepository(dependencies)
{
    public override IMigrationsDatabaseLock AcquireDatabaseLock()
    {
        return new NoopMigrationsDatabaseLock(this);
    }

    public override Task<IMigrationsDatabaseLock> AcquireDatabaseLockAsync(
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IMigrationsDatabaseLock>(new NoopMigrationsDatabaseLock(this));
    }
}
#endif
