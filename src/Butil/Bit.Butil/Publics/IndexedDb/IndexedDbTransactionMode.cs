namespace Bit.Butil;

/// <summary>
/// Access level a transaction asks for. See
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/IDBDatabase/transaction">IDBDatabase.transaction()</see>.
/// </summary>
public enum IndexedDbTransactionMode
{
    /// <summary>Reads and writes. Only one readwrite transaction per store runs at a time.</summary>
    ReadWrite,

    /// <summary>Reads only; these run concurrently with each other.</summary>
    ReadOnly
}
