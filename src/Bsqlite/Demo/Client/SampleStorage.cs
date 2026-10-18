namespace Bit.Bsqlite.Demo.Client;

internal static class SampleStorage
{
    public static string Title(string name) => name switch
    {
        "opfs" => "OPFS",
        "indexeddb" => "IndexedDB",
        "cache-storage" => "Cache Storage",
        "in-memory" => "In-memory",
        _ => name,
    };

    public static string Blurb(string name) => name switch
    {
        "opfs" => "Fastest persistent tier. Real files in the origin private file system. Concurrent connections, no COOP/COEP.",
        "indexeddb" => "Widest reach. Survives browsers that do not have OPFS. Relaxed durability available. Page size is pinned.",
        "cache-storage" => "The besql-compatible fallback. Persistence cost scales with database size.",
        "in-memory" => "Volatile. Gone on reload. Useful to try the engine without touching disk.",
        _ => "Registered storage provider.",
    };

    public static string PersistLabel(bool persistent) => persistent ? "survives reload" : "lost on reload";
}
