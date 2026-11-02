namespace Bit.BlazeDb;

/// <summary>
/// A point inside an open <see cref="BlazeDbTransaction"/>, from <see cref="BlazeDbTransaction.CreateSavepoint"/>,
/// that <see cref="BlazeDbTransaction.RollbackTo(BlazeDbSavepoint)"/> can return the transaction to.
/// </summary>
public readonly struct BlazeDbSavepoint
{
    internal BlazeDbSavepoint(BlazeDbTransaction transaction, long id, int position)
    {
        Transaction = transaction;
        Id = id;
        Position = position;
    }

    internal BlazeDbTransaction? Transaction { get; }

    internal long Id { get; }

    /// <summary>How many writes the transaction had recorded when the savepoint was taken.</summary>
    public int Position { get; }
}
