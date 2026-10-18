using Microsoft.EntityFrameworkCore.Metadata;

namespace Bit.BlazeDb.EntityFrameworkCore.Metadata;

/// <summary>Maps entity types onto the engine's tables.</summary>
internal interface IBlazeDbTableCache
{
    /// <summary>The engine instance the context was configured with.</summary>
    BlazeDbDatabase Database { get; }

    IBlazeDbTableBinding GetBinding(IEntityType entityType);

    /// <summary>
    /// Whether another context on the same engine holds a modified, unsaved entity of
    /// <paramref name="clrType"/> - the state in which the table's secondary indexes lag its rows
    /// and a query must read the rows instead. See <c>BlazeDbContextRegistry</c>.
    /// </summary>
    bool OtherContextsHaveModified(Type clrType);

    /// <summary>Drops the cached engine so the next access reopens after <c>EnsureDeleted</c>.</summary>
    void Reset();
}
