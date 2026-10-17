namespace Bit.BlazeDb;

/// <summary>Thrown when a write would duplicate a value in a unique secondary index.</summary>
public sealed class BlazeDbUniqueConstraintViolationException : BlazeDbException
{
    public BlazeDbUniqueConstraintViolationException(string tableName, string indexName, object value)
        : base($"Unique index '{indexName}' on table '{tableName}' already contains value '{value}'.")
    {
        TableName = tableName;
        IndexName = indexName;
        Value = value;
    }

    public string TableName { get; }

    public string IndexName { get; }

    public object Value { get; }
}
