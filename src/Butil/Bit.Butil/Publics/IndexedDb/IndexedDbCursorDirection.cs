namespace Bit.Butil;

/// <summary>
/// Direction a cursor walks its records. See
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/IDBCursor/direction">IDBCursor.direction</see>.
/// </summary>
public enum IndexedDbCursorDirection
{
    /// <summary>Default. Ascending key order, every matching record.</summary>
    Next,

    /// <summary>Ascending key order, but only the first record per distinct key.</summary>
    NextUnique,

    /// <summary>Descending key order, every matching record.</summary>
    Previous,

    /// <summary>Descending key order, but only the first record per distinct key.</summary>
    PreviousUnique
}
