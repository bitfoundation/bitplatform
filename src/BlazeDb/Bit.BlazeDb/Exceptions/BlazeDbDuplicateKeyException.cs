namespace Bit.BlazeDb;

/// <summary>Thrown when inserting a row whose primary key already exists.</summary>
public sealed class BlazeDbDuplicateKeyException : BlazeDbException
{
    public BlazeDbDuplicateKeyException(string tableName, object key)
        : base($"Table '{tableName}' already contains a row with key '{key}'.")
    {
        TableName = tableName;
        Key = key;
    }

    public string TableName { get; }

    public object Key { get; }
}
