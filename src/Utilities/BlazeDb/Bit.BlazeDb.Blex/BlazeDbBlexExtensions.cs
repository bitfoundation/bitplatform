namespace Bit.BlazeDb.Blex;

/// <summary>Registers the table Bit.Blex persists to on a BlazeDb database.</summary>
public static class BlazeDbBlexExtensions
{
    /// <summary>Registers the table Blex's state is kept in (<c>blex_state</c>) on the database being configured.</summary>
    public static BlazeDbDatabaseOptions AddBlexState(this BlazeDbDatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.AddTable(BlazeDbBlexEntry.Table);
    }
}
