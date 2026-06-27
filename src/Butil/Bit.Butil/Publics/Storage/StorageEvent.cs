namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/StorageEvent">StorageEvent</see>.
/// Fires only when the modification happens in another tab/window of the same origin.
/// </summary>
public class StorageEvent
{
    /// <summary>The key that was added/removed/changed. <c>null</c> when <c>storage.clear()</c> was called.</summary>
    public string? Key { get; set; }

    /// <summary>Previous value, or null when added or cleared.</summary>
    public string? OldValue { get; set; }

    /// <summary>New value, or null when removed or cleared.</summary>
    public string? NewValue { get; set; }

    /// <summary>URL of the document that triggered the event.</summary>
    public string? Url { get; set; }

    /// <summary><c>"localStorage"</c> or <c>"sessionStorage"</c>.</summary>
    public string StorageArea { get; set; } = string.Empty;
}
