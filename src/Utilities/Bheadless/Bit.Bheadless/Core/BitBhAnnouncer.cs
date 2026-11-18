namespace Bit.Bheadless;

/// <summary>Default <see cref="IBitBhAnnouncer"/> backed by a live region the JS runtime appends to &lt;body&gt;.</summary>
public sealed class BitBhAnnouncer : IBitBhAnnouncer
{
    private readonly BitBhInterop _interop;

    /// <summary>Creates the announcer.</summary>
    public BitBhAnnouncer(BitBhInterop interop)
    {
        _interop = interop;
    }

    /// <summary>How long (ms) a message stays in the region before it is cleared. Default: 7000.</summary>
    public int ClearAfterMilliseconds { get; set; } = 7000;

    /// <inheritdoc />
    public ValueTask AnnounceAsync(string message, BitBhPoliteness politeness = BitBhPoliteness.Polite)
        => string.IsNullOrWhiteSpace(message)
            ? ValueTask.CompletedTask
            : _interop.AnnounceAsync(message, politeness, ClearAfterMilliseconds);
}
