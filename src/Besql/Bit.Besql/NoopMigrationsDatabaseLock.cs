#if NET9_0_OR_GREATER
using Microsoft.EntityFrameworkCore.Migrations;

namespace Bit.Besql;

public class NoopMigrationsDatabaseLock(IHistoryRepository historyRepository) : IMigrationsDatabaseLock
{
    IHistoryRepository IMigrationsDatabaseLock.HistoryRepository => historyRepository;

    public void Dispose()
    {
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
#endif
