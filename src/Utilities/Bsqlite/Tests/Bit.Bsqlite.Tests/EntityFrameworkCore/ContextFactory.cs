using Bit.Bsqlite.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

internal static class ContextFactory
{
    public static ProductContext Create(IBsqliteTransport transport, string databaseName = "ef.db")
        => new(new DbContextOptionsBuilder<ProductContext>()
            .UseBitBsqlite(new BsqliteConnection(transport, databaseName))
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options);

    public static ProductContext CreateStock()
        => new(new DbContextOptionsBuilder<ProductContext>()
            .UseSqlite("Data Source=:memory:")
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options);
}
