namespace Bit.Butil;

/// <summary>
/// Mirrors a subset of <see href="https://developer.mozilla.org/en-US/docs/Web/API/MutationRecord">MutationRecord</see>.
/// DOM nodes can't cross interop, so they're flattened to lightweight summaries.
/// </summary>
public class MutationRecord
{
    /// <summary><c>"attributes"</c>, <c>"characterData"</c>, or <c>"childList"</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Tag name of the target node, or empty for non-element targets.</summary>
    public string TargetTagName { get; set; } = string.Empty;

    /// <summary>Id of the target node when present.</summary>
    public string? TargetId { get; set; }

    /// <summary>Attribute name (only for <c>"attributes"</c> mutations).</summary>
    public string? AttributeName { get; set; }

    /// <summary>Attribute namespace (only for <c>"attributes"</c> mutations).</summary>
    public string? AttributeNamespace { get; set; }

    /// <summary>Previous value when <c>AttributeOldValue</c> / <c>CharacterDataOldValue</c> were enabled.</summary>
    public string? OldValue { get; set; }

    /// <summary>Number of nodes added (only for <c>"childList"</c>).</summary>
    public int AddedCount { get; set; }

    /// <summary>Number of nodes removed (only for <c>"childList"</c>).</summary>
    public int RemovedCount { get; set; }
}
