namespace Bit.Butil;

/// <summary>What <see cref="IndexedDb.Open"/> learned while opening the database.</summary>
internal sealed class IndexedDbOpenInfo
{
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public string[] StoreNames { get; set; } = [];
    public int OldVersion { get; set; }
    public int NewVersion { get; set; }
    public bool Upgraded { get; set; }
}
