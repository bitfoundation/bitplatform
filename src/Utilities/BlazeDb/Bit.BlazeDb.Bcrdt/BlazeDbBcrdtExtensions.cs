namespace Bit.BlazeDb.Bcrdt;

/// <summary>Registers the table Bit.Bcrdt documents are kept in on a BlazeDb database.</summary>
public static class BlazeDbBcrdtExtensions
{
    /// <summary>Registers the table Bcrdt document updates are kept in (<c>bcrdt_records</c>) on the database being configured.</summary>
    public static BlazeDbDatabaseOptions AddBcrdtDocuments(this BlazeDbDatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.AddTable(BlazeDbBcrdtRecord.Table);
    }
}
