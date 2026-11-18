namespace Bit.Bheadless;

/// <summary>A toast queued in a <see cref="BitBhToaster"/>.</summary>
public sealed class BitBhToastItem
{
    internal BitBhToastItem(string id, BitBhToastOptions options)
    {
        Id = id;
        Options = options;
    }

    /// <summary>Unique id.</summary>
    public string Id { get; }

    /// <summary>The options it was shown (or last updated) with.</summary>
    public BitBhToastOptions Options { get; internal set; }

    /// <summary>Incremented each time the toast is updated: a toast restarts its timer and announces itself again.</summary>
    public int Version { get; internal set; }

    /// <summary>The variant (<see cref="BitBhToastOptions.Variant"/>).</summary>
    public string? Variant => Options.Variant;

    /// <summary>The title.</summary>
    public string? Title => Options.Title;

    /// <summary>The description.</summary>
    public string? Description => Options.Description;

    /// <summary>Custom data.</summary>
    public object? Data => Options.Data;

    /// <summary>Whether it is closing (exit animation).</summary>
    public bool IsClosing { get; internal set; }
}
