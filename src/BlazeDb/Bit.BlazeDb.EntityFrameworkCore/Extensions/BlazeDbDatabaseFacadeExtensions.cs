using Bit.BlazeDb.EntityFrameworkCore.Infrastructure;
using Bit.BlazeDb.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>Provider-specific members on <see cref="DatabaseFacade"/>, analogous to <c>GetSqliteConnection</c>.</summary>
public static class BlazeDbDatabaseFacadeExtensions
{
    /// <summary>
    /// The engine instance this context is running against. Needed only for BlazeDb-specific
    /// operations such as <see cref="Bit.BlazeDb.BlazeDbDatabase.FlushAsync"/>; ordinary CRUD goes through
    /// the context.
    /// </summary>
    public static Bit.BlazeDb.BlazeDbDatabase GetBlazeDb(this DatabaseFacade database)
    {
        ArgumentNullException.ThrowIfNull(database);
        return database.GetService<IBlazeDbTableCache>().Database;
    }
}
