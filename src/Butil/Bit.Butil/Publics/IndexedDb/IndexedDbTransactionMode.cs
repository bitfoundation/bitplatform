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

/// <summary>
/// How hard the browser should try to flush a transaction to disk before reporting it complete.
/// See <see href="https://developer.mozilla.org/en-US/docs/Web/API/IDBTransaction/durability">IDBTransaction.durability</see>.
/// </summary>
public enum IndexedDbDurability
{
    /// <summary>Let the browser decide (its own default, usually equivalent to <see cref="Relaxed"/>).</summary>
    Default,

    /// <summary>Complete as soon as the OS has the data; a crash may lose it. Faster.</summary>
    Relaxed,

    /// <summary>Don't complete until the data is genuinely flushed to storage. Slower, survives a crash.</summary>
    Strict
}
