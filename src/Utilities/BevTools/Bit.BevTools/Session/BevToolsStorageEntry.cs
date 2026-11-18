namespace Bit.BevTools;

/// <summary>One browser storage entry. The preview is redacted in the browser, so a secret never reaches BevTools.</summary>
public sealed record BevToolsStorageEntry(string Key, int Size, string Preview, bool Redacted = false);
