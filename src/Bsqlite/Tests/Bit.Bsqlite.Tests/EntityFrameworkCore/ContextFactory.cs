using Bit.Bsqlite.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bit.Bsqlite.EntityFrameworkCore.Tests;

internal static class ContextFactory
{
    public static ProductContext Create(IBsqliteTransport transport, string databaseName = "ef.db")
        => new(new DbContextOptionsBuilder<ProductContext>()
            .UseBitBsqlite(new BsqliteConnection(transport, databaseName))
#if NET9_0_OR_GREATER
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
#endif
            .Options);

    public static ProductContext CreateStock()
        => new(new DbContextOptionsBuilder<ProductContext>()
            .UseSqlite("Data Source=:memory:")
#if NET9_0_OR_GREATER
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
#endif
            .Options);
}
