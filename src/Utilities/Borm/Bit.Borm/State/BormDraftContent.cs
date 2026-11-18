namespace Bit.Borm;

/// <summary>What a draft held, as read back.</summary>
internal sealed class BormDraftContent
{
    public string? Version { get; set; }
    public DateTimeOffset? SavedAt { get; set; }
    public int? Step { get; set; }
    public Dictionary<string, object?> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Rows { get; } = new(StringComparer.OrdinalIgnoreCase);
}
